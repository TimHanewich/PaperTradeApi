using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Yahoo.Finance;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Collections.Generic;


namespace PaperTradeApi
{
    public static class PaperTradeApiCore
    {
        private const int Version = 5;

        [FunctionName("StockSummaryData")]
        public async static Task<HttpResponseMessage> GetStockSummaryData([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            log.LogInformation("New request received");
            string symbol = req.Query["symbol"];
            if (symbol == null || symbol == "")
            {
                log.LogInformation("Symbol not present as part of request. Ending.");
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent("Fatal failure. Parameter 'symbol' not provided. You must provide a stock symbol to return data for.");
                return hrm;
            }



            try
            {
                log.LogInformation("Downloading data for " + symbol);
                Equity e = Equity.Create(symbol);
                await e.DownloadSummaryAsync();
                log.LogInformation("Serializing");
                string json = JsonConvert.SerializeObject(e.Summary);
                log.LogInformation("Returning");

                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.OK);
                hrm.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return hrm;
            }
            catch (Exception e)
            {
                string err_msg = "Fatal failure while accessing summary data for stock '" + symbol.ToUpper() + "'. Internal error message: " + e.Message;
                log.LogInformation(err_msg);
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent(err_msg);
                return hrm;
            }
        }

        //Depends on the "StockSummaryData" function (the "StockSummaryData" funtion must already be uploaded to the Azu Function).
        [FunctionName("MultipleStockSummaryData")]
        public async static Task<HttpResponseMessage> GetMultipleStockSummaryData([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, ILogger log)
        {
            StreamReader sr = new StreamReader(req.Body);
            string content = await sr.ReadToEndAsync();

            //Check for blank
            if (content == null || content == "")
            {
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent("Your request body was blank. Please include a body of an array of strings (stock symbols), encoded in JSON format.");
                return hrm;
            }

            //Deserialize the body into an array of strings
            string[] symbols = null;
            try
            {
                symbols = JsonConvert.DeserializeObject<string[]>(content);
            }
            catch
            {
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent("Fatal error while parsing request body to JSON string array.");
                return hrm;
            }
            

            //Set them up
            HttpClient hc = new HttpClient();
            List<Task<HttpResponseMessage>> Requests = new List<Task<HttpResponseMessage>>();
            foreach (string s in symbols)
            {
                string url = "http://papertradesim.azurewebsites.net/api/StockSummaryData?symbol=" + s.Trim().ToLower();
                Requests.Add(hc.GetAsync(url));
            }

            //Wait for all responses
            HttpResponseMessage[] responses = await Task.WhenAll(Requests);


            //Parse into all
            List<EquitySummaryData> datas = new List<EquitySummaryData>();
            foreach (HttpResponseMessage hrm in responses)
            {
                if (hrm.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        string cont = await hrm.Content.ReadAsStringAsync();
                        EquitySummaryData esd = JsonConvert.DeserializeObject<EquitySummaryData>(cont);
                        datas.Add(esd);
                    }
                    catch
                    {
                        
                    }
                }
            }



            //return as Json
            string as_json = JsonConvert.SerializeObject(datas.ToArray());
            HttpResponseMessage rhrm = new HttpResponseMessage(HttpStatusCode.OK);
            rhrm.Content = new StringContent(as_json, Encoding.UTF8, "application/json");
            return rhrm;
        }

        [FunctionName("StockStatisticalData")]
        public async static Task<HttpResponseMessage> GetStockStatisticalData([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            log.LogInformation("New request received");
            string symbol = req.Query["symbol"];
            if (symbol == null || symbol == "")
            {
                log.LogInformation("Parameter 'symbol' not present as part of request. Ending.");
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent("Fatal failure. Parameter 'symbol' not provided. You must provide a stock symbol to return data for.");
                return hrm;
            }
            symbol = symbol.Trim().ToUpper();
            log.LogInformation("Symbol requested: '" + symbol + "'");

            //Download data
            EquityStatisticalData ToReturn = null;
            try
            {
                log.LogInformation("Attemping to download statistical data for '" + symbol + "'.");
                ToReturn = await EquityStatisticalData.CreateAsync(symbol.Trim().ToUpper());
            }
            catch
            {
                log.LogInformation("Error while downloading statistical data for stock '" + symbol + "'.");
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                hrm.Content = new StringContent("Fatal failure. Unable to download statistical data for stock with symbol '" + symbol + "'.");
                return hrm;
            }

            //Return it
            string json = JsonConvert.SerializeObject(ToReturn);
            HttpResponseMessage fhrm = new HttpResponseMessage(HttpStatusCode.OK);
            fhrm.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return fhrm;


        }

        //Depends on the "StockStatisticalData" function (the "StockStatisticalData" funtion must already be uploaded to the Azu Function).
        [FunctionName("MultipleStockStatisticalData")]
        public async static Task<HttpResponseMessage> GetMultipleStockStatisticalData([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, ILogger log)
        {
            StreamReader sr = new StreamReader(req.Body);
            string content = await sr.ReadToEndAsync();

            //Check for blank
            if (content == null || content == "")
            {
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent("Your request body was blank. Please include a body of an array of strings (stock symbols), encoded in JSON format.");
                return hrm;
            }

            //Deserialize the body into an array of strings
            string[] symbols = null;
            try
            {
                symbols = JsonConvert.DeserializeObject<string[]>(content);
            }
            catch
            {
                HttpResponseMessage hrm = new HttpResponseMessage(HttpStatusCode.BadRequest);
                hrm.Content = new StringContent("Fatal error while parsing request body to JSON string array.");
                return hrm;
            }
            

            //Set them up
            HttpClient hc = new HttpClient();
            List<Task<HttpResponseMessage>> Requests = new List<Task<HttpResponseMessage>>();
            foreach (string s in symbols)
            {
                string url = "http://papertradesim.azurewebsites.net/api/StockStatisticalData?symbol=" + s.Trim().ToLower();
                Requests.Add(hc.GetAsync(url));
            }

            //Wait for all responses
            HttpResponseMessage[] responses = await Task.WhenAll(Requests);


            //Parse into all
            List<EquityStatisticalData> datas = new List<EquityStatisticalData>();
            foreach (HttpResponseMessage hrm in responses)
            {
                if (hrm.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        string cont = await hrm.Content.ReadAsStringAsync();
                        EquityStatisticalData esd = JsonConvert.DeserializeObject<EquityStatisticalData>(cont);
                        datas.Add(esd);
                    }
                    catch
                    {
                        
                    }
                }
            }



            //return as Json
            string as_json = JsonConvert.SerializeObject(datas.ToArray());
            HttpResponseMessage rhrm = new HttpResponseMessage(HttpStatusCode.OK);
            rhrm.Content = new StringContent(as_json, Encoding.UTF8, "application/json");
            return rhrm;
        }


    }
}
