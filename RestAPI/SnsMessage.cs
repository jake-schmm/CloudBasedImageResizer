namespace RestAPI
{
    public class SnsMessage
    {
        /// <summary>
        /// Note: the names of these fields must match the fields in the serialized object in the Lambda function 
        /// because the Message attribute of the SNS message was set to that serialized object
        /// TODO: instead of using a generic object in the Lambda function, maybe use a class 
        /// </summary>
        public string BucketName { get; set; }
        public string S3Key { get; set; }
    }
}
