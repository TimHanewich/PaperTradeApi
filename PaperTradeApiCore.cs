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

namespace PaperTradeApi
{
    public static class PaperTradeApiCore
    {
        [FunctionName("StockSummaryData")]
        public async static Task<string> GetStockSummaryData([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, ILogger log)
        {
            log.LogInformation("New request received");
            string symbol = req.Query["symbol"];
            if (symbol == null || symbol == "")
            {
                log.LogInformation("Symbol not present as part of request. Ending.");
                return "Fatal failure. Parameter 'symbol' not provided. You must provide a stock symbol to return data for.";
            }



            try
            {
                log.LogInformation("Downloading data for " + symbol);
                Equity e = Equity.Create(symbol);
                await e.DownloadSummaryAsync();
                log.LogInformation("Serializing");
                string json = JsonConvert.SerializeObject(e.Summary);
                log.LogInformation("Returning");
                return json;
            }
            catch (Exception e)
            {
                string err_msg = "Fatal failure while accessing summary data for stock '" + symbol.ToUpper() + "'. Internal error message: " + e.Message;
                log.LogInformation(err_msg);
                return err_msg;
            }
        }


    }
}
