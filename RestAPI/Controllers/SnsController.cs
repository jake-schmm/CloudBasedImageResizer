using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace RestAPI.Controllers
{
    [Route("api/SNS")]
    [ApiController]
    public class SNSController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;

        public SNSController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var messageType = Request.Headers["x-amz-sns-message-type"].FirstOrDefault();
            var contentType = Request.Headers["Content-Type"].FirstOrDefault();

            Console.WriteLine($"Message Type: {messageType}");
            Console.WriteLine($"Content-Type: {contentType}");

            using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();
                Console.WriteLine($"Received Body: {body}");

                if (messageType == "SubscriptionConfirmation")
                {
                    // The body is plain text, not JSON
                    // Handle the SubscriptionConfirmation here, you need to confirm the subscription by sending a GET request to the SubscribeURL
                    // When you subscribe an endpoint (like an HTTP/HTTPS URL, email address, or SQS queue) to an SNS topic, SNS sends a subscription confirmation message to the endpoint.
                    // The recipient (or the application) must visit the SubscribeURL to confirm the subscription. This is typically done by sending a GET request to the URL provided in the confirmation message.
                    var snsMessage = JsonConvert.DeserializeObject<SubscriptionConfirmation>(body);
                    var subscribeUrl = snsMessage?.SubscribeURL;

                    if (!string.IsNullOrEmpty(subscribeUrl))
                    {
                        await ConfirmSubscription(subscribeUrl);
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("SubscribeURL is missing in the subscription confirmation message.");
                    }
                }

                if (messageType == "Notification")
                {
                    try
                    {
                        dynamic snsDynamicMessage = JsonConvert.DeserializeObject<dynamic>(body) ?? throw new InvalidOperationException("Deserialized message is null");
                        var snsMessageBody = snsDynamicMessage.Message?.ToString() ?? throw new InvalidOperationException("Message body is null");

                        var snsMessage = JsonConvert.DeserializeObject<SnsMessage>(snsMessageBody) ?? throw new InvalidOperationException("Failed to deserialize SnsMessage");
                        await ProcessMessageAsync(snsMessage);
                        return Ok();
                    }
                    catch (JsonSerializationException ex)
                    {
                        Console.WriteLine($"Error deserializing SNS message: {ex.Message}");
                        return BadRequest("Invalid JSON format in the SNS message.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Error processing SNS message: {ex.Message}");
                        return BadRequest(ex.Message);
                    }
                }
            }

            return BadRequest();
        }

        private async Task ConfirmSubscription(string subscribeUrl)
        {
            using (var httpClient = new HttpClient())
            {
                await httpClient.GetAsync(subscribeUrl);
            }
        }

        private async Task ProcessMessageAsync(SnsMessage message)
        {
            var bucketName = message.BucketName;
            var key = message.S3Key;
            var localFilePath = Path.Combine("C:/Uploads/", Path.GetFileName(key));

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.DownloadAsync(localFilePath, bucketName, key);

            Console.WriteLine($"Downloaded resized image to {localFilePath}");
        }
    }

}
public class SubscriptionConfirmation
{
    public string SubscribeURL { get; set; }
}