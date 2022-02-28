using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ESC4_SQStoLambda
{
    public class Function
    {
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {

        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            if(evnt.Records.Count > 0)
            {
                await ProcessMessageAsync(evnt.Records[0], context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processed message { message.Body}");
            try
            {
                var recepient = JsonConvert.DeserializeObject<RecepientDetails>(message.Body);
                context.Logger.LogLine($"Receipint Object found {recepient.name}, {recepient.attendee_pk}");
            }
            catch (Exception ex){
                context.Logger.LogLine($"There is error in casting object {ex.Message}");
            }

            // TODO: Do interesting work based on the new message
            await Task.CompletedTask;
        }


    }

    public class RecepientDetails
    { 
        public string attendee_pk { get; set; }
        public string title { get; set; }
        public string schedule { get; set; }
        public string emails { get; set; }
        public string location { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string home_phone { get; set; }
        public string work_phone { get; set; }
        public string Room { get; set; }
    }
}
