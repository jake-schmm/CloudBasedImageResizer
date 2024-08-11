using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.S3.Transfer;
using Amazon.S3;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyAsyncLambdaFunction
{

    public class AWSLambdaFunction
    {
        private static readonly AmazonSimpleNotificationServiceClient SnsClient = new AmazonSimpleNotificationServiceClient();
        private static readonly AmazonS3Client _s3Client = new AmazonS3Client();

        /// <summary>
        /// A Lambda function to process SQS messages asynchronously. This will function like a consumer for SQS messages that get published to the queue. 
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            // Get ALL messages from SQSEvent and process each of them
            // This allows you to handle multiple messages in a single invocation, which is more efficient and can help you scale your processing.
            foreach (var message in evnt.Records) 
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private static string GetSnsTopicArn()
        {
            var snsTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN"); // Stored in AWS: Lambda function > Environment variables 

            if (string.IsNullOrEmpty(snsTopicArn))
            {
                throw new Exception("Environment variable 'SNS_TOPIC_ARN' is not set.");
            }

            return snsTopicArn;
        }
        private static async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            try
            {
                // Convert JSON string (message.Body) into ImageMessageAmazonSQS object so you can get the FilePath attribute
                var imageMessage = JsonConvert.DeserializeObject<ImageMessageAmazonSQS>(message.Body); 
                if(imageMessage != null && imageMessage.S3KeyName != null)
                {
                    context.Logger.LogLine($"S3KeyName: {imageMessage.S3KeyName}, S3BucketName: {imageMessage.S3BucketName}");
                    //ImageResizer imageResizer = new ImageResizer();
                    //imageResizer.LambdaFunctionResizeImageInS3(imageMessage.S3BucketName, imageMessage.S3KeyName, 250, 200);
                    LambdaFunctionResizeImageInS3(imageMessage.S3BucketName, imageMessage.S3KeyName, 250, 200);

                    // Publish an SNS message
                    // NOTE: This is not required - you could just send a request directly to your WEB API at the end of this Lambda function to download the resized image
                    // But I've configured my Web API to listen for SNS notifications
                    // The pro of this is that it decouples the systems and allows me to potentially have multiple subscribers to the notifications in the future.
                    var snsMessage = JsonConvert.SerializeObject(new
                    {
                        BucketName = imageMessage.S3BucketName,
                        S3Key = imageMessage.S3KeyName
                    });

                    var publishRequest = new PublishRequest
                    {
                        TopicArn = GetSnsTopicArn(),
                        Message = snsMessage
                    };

                    var response = await SnsClient.PublishAsync(publishRequest);
                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        context.Logger.LogLine("Successfully sent message to SNS.");
                    }
                    else
                    {
                        context.Logger.LogLine($"Failed to send message to SNS. Status code: {response.HttpStatusCode}");
                    }
                }
                else
                {
                    context.Logger.LogLine($"Image Message or its S3 Key was null. Image was not resized.");
                }
                context.Logger.LogLine($"Successfully processed message with ID: {message.MessageId}");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message with ID: {message.MessageId}. Error: {ex.Message}");
                throw; // This will cause the message to be retried or sent to a dead-letter queue.
            }
        }

        public static void LambdaFunctionResizeImageInS3(string bucketName, string key, int width, int height)
        {
            string localPath = GetLambdaLocalPath(key);

            // Download the image from S3 to localPath
            DownloadFromS3(bucketName, key, localPath);

            // Resize the image
            using (FileStream inputStream = new FileStream(localPath, FileMode.Open, FileAccess.Read))
            {
                using (var image = Image.Load(inputStream))
                {
                    image.Mutate(x => x.Resize(width, height));
                    string extension = Path.GetExtension(key).ToLower();

                    using (FileStream outputStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                    {
                        switch (extension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 90 });
                                break;
                            case ".png":
                                image.SaveAsPng(outputStream);
                                break;
                            default:
                                throw new NotSupportedException("Unsupported image format: " + extension);
                        }
                    }
                }
            }

            // Upload the resized image back to S3
            UploadToS3(localPath, bucketName, key);

            // Delete the local temporary file from Lambda function's local environment
            DeleteLocalFile(localPath);
        }

        private static void DownloadFromS3(string bucketName, string key, string localPath)
        {
            try
            {
                var transferUtility = new TransferUtility(_s3Client);
                transferUtility.Download(localPath, bucketName, key);
                Console.WriteLine("File downloaded successfully from S3.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while downloading the file from S3: " + ex.Message);
            }
        }

        private static void UploadToS3(string localPath, string bucketName, string key)
        {
            try
            {
                var transferUtility = new TransferUtility(_s3Client);
                transferUtility.Upload(localPath, bucketName, key);
                Console.WriteLine("File uploaded successfully to S3.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while uploading the file to S3: " + ex.Message);
            }
        }

        private static string GetLambdaLocalPath(string key)
        {
            // tmp is typically the only accessible directory in a Lambda function's environment as it's running 
            return Path.Combine("/tmp", Path.GetFileName(key));
        }

        private static void DeleteLocalFile(string localPath)
        {
            try
            {
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                    Console.WriteLine("Temporary file deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while deleting the temporary file: " + ex.Message);
            }
        }


    }
}

