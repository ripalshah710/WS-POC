using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ESC4_SES_Service
{
    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool FunctionHandler(EMailMessage input, ILambdaContext context)
        {
            return SendEmail(input,context);
        }

        private bool SendEmail(EMailMessage input, ILambdaContext context)
        {
            try
            {
                using (var client = new AmazonSimpleEmailServiceClient(RegionEndpoint.USEast1))
                {
                    var sendRequest = new SendEmailRequest
                    {
                        Source = input.FromAddress,
                        Destination = new Destination
                        {
                            ToAddresses =
                            new List<string> { input.ToAddress }
                        },
                        Message = new Message
                        {
                            Subject = new Content(input.Subject),
                            Body = new Body
                            {
                                Html = new Content
                                {
                                    Data = input.Body
                                },
                                Text = new Content
                                {
                                    Charset = "UTF-8",
                                    Data = input.Body
                                }
                            }
                        },
                        // If you are not using a configuration set, comment
                        // or remove the following line 
                        //ConfigurationSetName = configSet
                    };
                    try
                    {
                        context.Logger.LogLine("Sending email using Amazon SES...");
                        var response = client.SendEmailAsync(sendRequest);
                        context.Logger.LogLine("The email was sent successfully.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogLine("The email was not sent.");
                        context.Logger.LogLine("Error message: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"There is error in sending SES {ex.Message}");
            }
            return false;
        }
    }

    public class EMailMessage
    {
        public string attendee_pk { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }
        public string ToAddress { get; set; }
        public string FromAddress { get; set; }
    }
}
