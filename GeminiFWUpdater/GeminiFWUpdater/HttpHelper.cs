using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GeminiFWUpdater
{
    internal class HttpHelper
    {
        #region Class Var
        public int Port { get; }
        public string IP { get; }
        public string Endpoint { get; }
        public int TimeoutInSeconds { get; } // Timeout in seconds

        public HttpHelper(string ip, int port, string endPoint, int timeoutInSeconds = 3000)
        {
            IP = ip;
            Port = port;
            Endpoint = endPoint;
            TimeoutInSeconds = timeoutInSeconds;
        }
        #endregion

        #region Class Methods
        // Method to make the HTTP POST request with a timeout
        public async Task<string> PostJsonAsync(object jsonObject)
        {
            string url = $"http://{IP}:{Port}/{Endpoint}";
            string jsonContent = JsonConvert.SerializeObject(jsonObject);

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(TimeoutInSeconds); // Set the timeout

                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"HTTP POST failed with status code {response.StatusCode}");
                    }

                    return await response.Content.ReadAsStringAsync();
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    // Handle timeout
                    Console.WriteLine("Error: HTTP request timed out.");
                    return null;
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"Error: {ex.Message}");
                    return null;
                }
            }
        }

        // Download file with a timeout and handle errors
        public async Task DownloadFileAsync(string fileUrl, string destinationPath)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(TimeoutInSeconds); // Set the timeout

                try
                {
                    HttpResponseMessage response = await client.GetAsync(fileUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"HTTP GET failed with status code {response.StatusCode}");
                    }

                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Use the synchronous method for .NET Framework to write the file
                    File.WriteAllBytes(destinationPath, fileBytes);
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    // Handle timeout
                    Console.WriteLine("Error: File download timed out.");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        #endregion

        #region JSON De-Serializaion
        // JSON utility: Serialize an object to a JSON string
        public string SerializeToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        // JSON utility: Deserialize a JSON string to a C# object
        public T DeserializeFromJson<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
        #endregion
    }

    #region ResponseModel
    // VersionManagerResponseModel class
    public class VersionManagerResponseModel
    {
        public int statusCode { get; set; }
        public string serverTime { get; set; }
        public bool isUpdateRequired { get; set; }
        public string url { get; set; }
        public string mD5 { get; set; }

        public void DisplayResponse()
        {
            Console.WriteLine("{");
            Console.WriteLine("statusCode:\t" + statusCode + ",");
            Console.WriteLine("serverTime:\t" + serverTime + ",");
            Console.WriteLine("isUpdateRequired:\t" + isUpdateRequired + ",");
            Console.WriteLine("url:\t" + url + ",");
            Console.WriteLine("mD5:\t" + mD5 + ",");
            Console.WriteLine("}");
        }
    }
    #endregion
}
