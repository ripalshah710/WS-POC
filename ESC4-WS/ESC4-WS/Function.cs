using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ESC4_WS
{
    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(ILambdaContext context)
        {
            //var sqsClient = new AmazonSQSClient();
            //await ShowQueuesAsync(sqsClient, context);
            return await GetDBOperationAsync(context);
        }

        private async Task<string> GetDBOperationAsync(ILambdaContext context)
        {
            string output = string.Empty;
            using (SqlCommand SQLCommand = new SqlCommand("[/sysmail/Notification/notifyTofaci]", new SqlConnection(this.ConnectionString)))
            {
                SQLCommand.CommandType = CommandType.StoredProcedure;
                var sqsClient = new AmazonSQSClient();
                try
                {
                    SQLCommand.Connection.Open();
                    SqlDataReader dr = SQLCommand.ExecuteReader(CommandBehavior.CloseConnection);
                    if (dr.HasRows)
                    {
                        context.Logger.LogLine($"Procedure return {dr.RecordsAffected} rows");
                    }
                    else
                    {
                        context.Logger.LogLine($"Procedure return no rows");
                    }
                    while (dr.Read())
                    {
                        await PushMessagetoSQS(sqsClient, Convert_DataRowToJson(dr),context);
                    }
                }
                catch (Exception ex)
                {
                    context.Logger.LogLine($"Error in executing command {ex.Message}");
                }

                finally
                {
                    if (SQLCommand.Connection.State != ConnectionState.Closed)
                        SQLCommand.Connection.Close();
                }
            }
            return output;
        }
        private async Task ShowQueuesAsync(IAmazonSQS sqsClient, ILambdaContext context)
        {
            ListQueuesResponse responseList = await sqsClient.ListQueuesAsync("");
            context.Logger.LogLine($"Total URL find {responseList.QueueUrls.Count}");
            foreach (string qUrl in responseList.QueueUrls)
            {
                context.Logger.LogLine($"Queue URL : {qUrl}");
                // Get and show all attributes. Could also get a subset.
                await ShowAllAttributes(sqsClient, qUrl);
            }
        }

        //
        // Method to show all attributes of a queue
        private static async Task ShowAllAttributes(IAmazonSQS sqsClient, string qUrl)
        {
            var attributes = new List<string> { };
            GetQueueAttributesResponse responseGetAtt =
              await sqsClient.GetQueueAttributesAsync(qUrl, attributes);
            Console.WriteLine($"Queue: {qUrl}");
            foreach (var att in responseGetAtt.Attributes)
                Console.WriteLine($"\t{att.Key}: {att.Value}");
        }

        private string Convert_DataRowToJson(SqlDataReader datarow)
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < datarow.FieldCount; i++)
            {
                dict.Add(datarow.GetName(i), datarow[i]);
            }
            
            return JsonConvert.SerializeObject(dict);
        }

        //
        // Method to put a message on a queue
        // Could be expanded to include message attributes, etc., in a SendMessageRequest
        private  async Task PushMessagetoSQS(
          IAmazonSQS sqsClient, string messageBody, ILambdaContext context)
        {
            context.Logger.LogLine($"Message {messageBody} added to queue\n  {this.SQSUrl}");
            SendMessageResponse responseSendMsg =
              await sqsClient.SendMessageAsync(this.SQSUrl, messageBody);
            context.Logger.LogLine($"HttpStatusCode: {responseSendMsg.HttpStatusCode}");
        }

        private string SQSUrl
        {
            get
            {
                return Environment.GetEnvironmentVariable("SQS_URL");
            }
        }

        private string ConnectionString {
            get {
                return Environment.GetEnvironmentVariable("DB_CONNECTION");
            }
        }
    }
}
