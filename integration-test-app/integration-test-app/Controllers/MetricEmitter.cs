using System;
using System.Collections.Generic;
using OpenTelemetry;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Instrumentation;

namespace integration_test_app.Controllers
{
    public class MetricEmitter
    {
        const string DIMENSION_API_NAME = "apiName";
        const string DIMENSION_STATUS_CODE = "statusCode";
        

        static string API_COUNTER_METRIC = "apiBytesSent";
        static string API_LATENCY_METRIC = "latency";
        static string API_SUM_METRIC = "totalApiBytesSent";
        static string API_LAST_LATENCY_METRIC = "lastLatency";
        static string API_UP_DOWN_COUNTER_METRIC = "queueSizeChange";
        static string API_UP_DOWN_SUM_METRIC = "actualQueueSize";

        Histogram<double> apiLatencyRecorder;
        Counter<long> totalBytesSentObserver;

        long apiBytesSent;
        //long queueSizeChange;

        long totalBytesSent;
        long apiLastLatency;
        long actualQueueSize;

        // The below API name and status code dimensions are currently shared by all metrics observer in
        // this class.
        string apiNameValue = "";
        string statusCodeValue = "";
        
        public MetricEmitter()
        {
            Meter meter = new Meter("aws-otel", "1.0");
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("aws-otel")
                .AddOtlpExporter()
                .Build();
            
            string latencyMetricName = API_LATENCY_METRIC;
            string apiBytesSentMetricName = API_COUNTER_METRIC;
            string totalApiBytesSentMetricName = API_SUM_METRIC;
            string lastLatencyMetricName = API_LAST_LATENCY_METRIC;
            //string queueSizeChangeMetricName = API_UP_DOWN_COUNTER_METRIC;
            string actualQueueSizeMetricName = API_UP_DOWN_SUM_METRIC;

            string instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID");
            if (instanceId != null && !instanceId.Trim().Equals(""))
            {
                latencyMetricName = API_LATENCY_METRIC + "_" + instanceId;
                apiBytesSentMetricName = API_COUNTER_METRIC + "_" + instanceId;
                totalApiBytesSentMetricName = API_SUM_METRIC + "_" + instanceId;
                lastLatencyMetricName = API_LAST_LATENCY_METRIC + "_" + instanceId;
                //queueSizeChangeMetricName = API_UP_DOWN_COUNTER_METRIC + "_" + instanceId;
                actualQueueSizeMetricName = API_UP_DOWN_SUM_METRIC + "_" + instanceId;
            }
            apiLatencyRecorder = meter.CreateHistogram<double>(latencyMetricName, "ms", "API latency time");


            KeyValuePair<string,object> dimApiName = new KeyValuePair<string, object>(DIMENSION_API_NAME, apiNameValue);
            KeyValuePair<string, object> dimStatusCode =
                new KeyValuePair<string, object>(DIMENSION_STATUS_CODE, statusCodeValue);
            
            meter.CreateObservableCounter(apiBytesSentMetricName,() => apiBytesSent, 
                "one",
                "API Request load sent in bytes");

            meter.CreateObservableGauge(totalApiBytesSentMetricName, () => totalBytesSent, 
                "one",
                "Total API Request load sent in bytes");

            meter.CreateObservableGauge(lastLatencyMetricName, () => apiLastLatency, 
                "ms",
                "The last API latency observed at collection interval");

            meter.CreateObservableGauge(actualQueueSizeMetricName, () => actualQueueSize, 
                "one", "The actual queue size observed at collection interval");
            // Currently not supported
            //meter.CreateObservableUpDownCounter(queueSizeChangeMetricName, () => queueSizeChange, "one",
            //   "Queue size change");
            
        }
        
        public void emitReturnTimeMetric(long returnTime, String apiName, String statusCode) {
            apiLatencyRecorder.Record(
                returnTime,
                new KeyValuePair<string, object>(DIMENSION_API_NAME, apiName),
                new KeyValuePair<string, object>(DIMENSION_STATUS_CODE, statusCode));
        }
        public void emitBytesSentMetric(int bytes, String apiName, String statusCode) {
            Console.WriteLine("ebs: " + bytes);
            apiBytesSent += bytes;
            Console.WriteLine("apiBs: "+ apiBytesSent);
        } 
        /*
        public void emitQueueSizeChangeMetric(int queueSizeChange, String apiName, String statusCode) {
            queueSizeChange += queueSizeChange;
        }
        */
        
        public void updateTotalBytesSentMetric(int bytes, String apiName, String statusCode) {
            totalBytesSent += bytes;
            apiNameValue = apiName;
            statusCodeValue = statusCode;
        }
        
        public void updateLastLatencyMetric(long returnTime, String apiName, String statusCode) {
            apiLastLatency = returnTime;
            apiNameValue = apiName;
            statusCodeValue = statusCode;
        }
        
        public void updateActualQueueSizeMetric(int queueSizeChange, String apiName, String statusCode) {
            actualQueueSize += queueSizeChange;
            apiNameValue = apiName;
            statusCodeValue = statusCode;
        }
        

    }
}