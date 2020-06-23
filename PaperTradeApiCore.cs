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

namespace PaperTradeApi
{
    public static class PaperTradeApiCore
    {
        private const int Version = 2;

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
                hrm.Content = new StringContent(json);
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


    }
}
