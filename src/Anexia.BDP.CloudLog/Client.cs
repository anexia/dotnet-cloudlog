using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Diagnostics;

namespace Anexia.BDP.CloudLog
{
    /// <summary>
    ///     Implements a CloudLog client
    /// </summary>
    public class Client
    {
        private string brokers = "kafka0401.bdp.anexia-it.com:8443";
        private string api = "https://api0401.bdp.anexia-it.com";

        private string index;
        private string token;
        private string caFile;
        private string certFile;
        private string keyFile;
        private string keyPassword;
        private string clientType;
        private bool isHttp;
        private long queue = 0;

        private Producer<string, string> producer;
        private HttpClient httpClient;

        /// <summary>
        ///     Creates a new client instance (kafka)
        /// </summary>
        /// <param name="index">
        ///     Index name
        /// </param>
        /// <param name="caFile">
        ///     Path to ca file
        /// </param>
        /// <param name="certFile">
        ///     Path to certificate file
        /// </param>
        /// <param name="keyFile">
        ///     Path to key file
        /// </param>
        public Client(string index, string caFile, string certFile, string keyFile, string keyPassword = "")
        {
            this.index = index;
            this.caFile = caFile;
            this.certFile = certFile;
            this.keyFile = keyFile;
            this.keyPassword = keyPassword;
            this.isHttp = false;
            this.clientType = "dotnet-client-kafka";
            CheckKafkaConfiguration();
            CreateProducer();

        }

        /// <summary>
        ///     Creates a new client instance (http)
        /// </summary>
        /// <param name="index">
        ///     Index name
        /// </param>
        /// <param name="token">
        ///     Authentification token
        /// </param>
        public Client(string index, string token)
        {
            this.index = index;
            this.token = token;
            this.isHttp = true;
            this.clientType = "dotnet-client-http";
            CheckHttpConfiguration();

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        }

        /// <summary>
        ///     Set custom client type name
        /// </summary>
        /// <param name="clientType">
        ///     Client type
        /// </param>
        public void SetClientType(string clientType)
        {
            this.clientType = clientType;
        }

        /// <summary>
        ///     Push events to CloudLog
        /// </summary>
        /// <param name="events">
        ///     Events
        /// </param>
        public void PushEvents(string[] events)
        {
            long timestamp = (long)DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds;

            if (isHttp)
            {
                JArray array = new JArray();
                foreach (string evt in events)
                {
                    var finalEvt = AddMetadata(evt, timestamp);
                    array.Add(finalEvt);
                }
                SendOverHttp(array.ToString());
            }
            else
            {
                foreach (string evt in events)
                {
                    var finalEvt = AddMetadata(evt, timestamp);
                    var deliveryReport = producer.ProduceAsync(index, null, finalEvt);
                    queue++;
                    deliveryReport.ContinueWith(task =>
                    {
                        queue--;
                        if (task.Result.Error.HasError)
                        {
                            Console.Error.WriteLine(task.Result.Error.Reason);
                        }
                    });
                }
            }

        }

        /// <summary>
        ///     Flush events
        /// </summary>
        public void Flush()
        {
            if (!isHttp && queue > 0)
            {
                producer.Flush(5000);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (queue > 0)
            {

                if (sw.ElapsedMilliseconds > 5000)
                {
                    queue = 0;
                }
            }

        }

        /// <summary>
        ///     Send events to http api
        /// </summary>
        /// <param name="events">
        ///     Events
        /// </param>
        private void SendOverHttp(string events)
        {
            var content = new StringContent("{ \"records\" : " + events + "}", Encoding.UTF8, "application/json");
            queue++;
            httpClient.PostAsync(api + "/v1/index/" + index + "/data", content).ContinueWith(task =>
            {
                queue--;
                if (task.Result.StatusCode != System.Net.HttpStatusCode.Created && task.Result.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Console.Error.WriteLine("Failed send message with status code: " + task.Result.StatusCode);
                }
            });
        }

        /// <summary>
        ///     Parse and add meta data to event
        /// </summary>
        /// <param name="evt">
        ///     Event
        /// </param>
        /// <param name="timestamp">
        ///     Timestamp
        /// </param>
        private string AddMetadata(string evt, long timestamp)
        {
            JObject data = null;
            try
            {
                data = JObject.Parse(evt);
            }
            catch (Exception) { }
            if (data == null)
            {
                data = new JObject();
                data["message"] = evt;
            }
            if (data["timestamp"] == null)
            {
                data["timestamp"] = timestamp;
            }
            data["cloudlog_client_type"] = clientType;
            data["cloudlog_source_host"] = Environment.MachineName;

            return data.ToString();
        }

        /// <summary>
        ///     Sends an event to CloudLog
        /// </summary>
        /// <param name="evt">
        ///     Event
        /// </param>
        public void PushEvent(string evt)
        {
            string[] events = { evt };
            PushEvents(events);
        }

        /// <summary>
        ///     Check http configuration
        /// </summary>
        private void CheckHttpConfiguration()
        {
            if (String.IsNullOrEmpty(index))
            {
                throw new ArgumentException("mssing index");
            }
            if (String.IsNullOrEmpty(token))
            {
                throw new ArgumentException("mssing token");
            }
        }

        /// <summary>
        ///     Check kafka configuration
        /// </summary>
        private void CheckKafkaConfiguration()
        {
            if (!File.Exists(caFile))
            {
                throw new FileNotFoundException("ca file not found at: " + caFile);
            }
            if (!File.Exists(certFile))
            {
                throw new FileNotFoundException("certificate file not found at: " + certFile);
            }
            if (!File.Exists(keyFile))
            {
                throw new FileNotFoundException("key file not found at: " + keyFile);
            }
            if (String.IsNullOrEmpty(index))
            {
                throw new ArgumentException("mssing index");
            }
        }

        /// <summary>
        ///     Create kafka producer
        /// </summary>
        private void CreateProducer()
        {
            var config = new Dictionary<string, object> {
                { "bootstrap.servers", brokers },
                { "security.protocol", "ssl" },
                { "ssl.ca.location", caFile },
                { "ssl.certificate.location", certFile },
                { "ssl.key.location", keyFile },
                { "request.required.acks", "all" },
                { "compression.codec", "gzip" },
                { "retries", 10 },

            };
            if (!string.IsNullOrWhiteSpace(keyPassword))
            {
                config.Add("ssl.key.password", keyPassword);
            }

            producer = new Producer<string, string>(config, new StringSerializer(Encoding.UTF8), new StringSerializer(Encoding.UTF8));
        }


    }
}