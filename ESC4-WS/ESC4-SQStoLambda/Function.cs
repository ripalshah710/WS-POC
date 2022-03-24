using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ESC4_SQStoLambda
{
    public class Function
    {
        public IAmazonS3 S3Client;
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the 
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            RegionEndpoint bucketRegion = RegionEndpoint.USEast1;
            S3Client = new AmazonS3Client(bucketRegion);
        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt">SQS Events</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            if (evnt.Records.Count > 0)
            {
                var message = await ProcessMessageAsync(evnt.Records[0], context);
                return JsonConvert.SerializeObject(message);
            }
            return null;
        }

        private async Task<object> ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processed message { message.Body}");
            try
            {
                var recepient = JsonConvert.DeserializeObject<RecepientDetails>(message.Body);
                context.Logger.LogLine($"Receipint Object found {recepient.name}, {recepient.attendee_pk}");
                return await CreateMailBody(recepient, context);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"There is error in casting object {ex.Message}");
            }

            // TODO: Do interesting work based on the new message
            return null;
        }

        private async Task<object> CreateMailBody(RecepientDetails recepient, ILambdaContext context)
        {
            XmlDocument DocElement = await GetMailTemplatesFromS3(context);
            if (DocElement != null && recepient != null)
            {
                Message message = new Message();
                message.Subject = "New enrollee information";
                message.IsBodyHtml = true;

                message.Body = string.Format(DocElement.DocumentElement.ChildNodes[7].InnerText, recepient.title, recepient.schedule, recepient.location, 
                    recepient.name , recepient.email, recepient.Address1, recepient.Address2, recepient.home_phone, recepient.work_phone, recepient.Room);

                string[] sessiondelete_recipient = recepient.emails.Split(';');
                StringBuilder ss = new StringBuilder();
                //foreach (string eachrecipient in sessiondelete_recipient)
                //{
                //    //checking for format of email
                //    if (new System.Text.RegularExpressions.Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*").IsMatch(eachrecipient))
                //    {
                //        message.To.Add(new MailAddress(eachrecipient));
                //    }
                //}
                message.FromAddress= "hardik.panchal@bacancy.com";
                message.ToAddress = "hardik.panchal@esc4.net";
                
                return message;
            }
            return null;
        }

        private async Task<XmlDocument> GetMailTemplatesFromS3(ILambdaContext context)
        {
            XmlDocument DocElement = new XmlDocument();
            var bucketName = S3BucketName;//"esc-04-mail-template";//;
            var fileName = S3FileName;// "tx_esc_04NotificationServices.xml";//;
            context.Logger.LogLine($"Bucket Name : {bucketName}");
            context.Logger.LogLine($"File Name : {fileName}");
            try
            {
                GetObjectResponse response = new GetObjectResponse();
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };
                response = await S3Client.GetObjectAsync(request);

                DocElement.Load(response.ResponseStream);
                return DocElement;

            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"There is error in fetching S3 object {ex.Message}");
            }
            return null;
        }

        private string S3BucketName
        {
            get
            {
                return Environment.GetEnvironmentVariable("S3_Bucket_Name") ?? "esc-04-mail-template";
            }
        }

        private string S3FileName
        {
            get
            {
                return Environment.GetEnvironmentVariable("S3_File_Name") ?? "tx_esc_04NotificationServices.xml";
            }
        }
    }

    public class Message
    {
        public string attendee_pk { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }
        public string ToAddress { get; set; }
        public string FromAddress { get; set; }
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
