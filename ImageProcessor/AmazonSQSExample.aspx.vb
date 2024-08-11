Imports System.IO
Imports Microsoft.Extensions.DependencyInjection
Imports Amazon.S3
Imports Amazon.S3.Transfer

Partial Class _AmazonSQSExample
    Inherits System.Web.UI.Page

    Private Shared ReadOnly bucketName As String = ConfigurationManager.AppSettings("AWS_BUCKET_NAME")
    Private s3Client As IAmazonS3

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Resolve the Amazon3lient from the service providers
        s3Client = DirectCast(Global_asax.ServiceProvider.GetService(GetType(IAmazonS3)), IAmazonS3)
    End Sub
    Protected Async Sub UploadButton_Click(sender As Object, e As EventArgs)
        If FileUpload1.HasFile Then
            Dim fileExtension As String = Path.GetExtension(FileUpload1.FileName).ToLower()
            If fileExtension = ".jpg" OrElse fileExtension = ".jpeg" OrElse fileExtension = ".png" Then
                'Dim uploadDir As String = Server.MapPath("~/Uploads/")
                Dim uploadDir As String = "C:/Uploads/"
                ' Check if the directory exists, if not, create it
                If Not Directory.Exists(uploadDir) Then
                    Directory.CreateDirectory(uploadDir)
                End If

                ' Save the uploaded file to the directory
                ' The following SaveAs is necessary because you need a file path to upload from to upload to S3, and the server of your application can't access what file path you originally uploaded from for security reasons.
                ' 'So save it to C:/Uploads so that you at least have a place you can upload to S3 bucket from
                Dim filePath As String = Path.Combine(uploadDir, Path.GetFileName(FileUpload1.FileName))
                FileUpload1.SaveAs(filePath)

                Dim keyName As String = Path.GetFileName(filePath)

                ' Upload to S3
                Dim fileTransferUtility = New TransferUtility(s3Client)
                Await fileTransferUtility.UploadAsync(filePath, bucketName)

                ' AWS SQS
                Dim message As New ImageMessageAmazonSQS With {
                    .S3KeyName = keyName,
                    .S3BucketName = bucketName
                }
                Dim sqsPublisher = Global_asax.ServiceProvider.GetRequiredService(Of SqsPublisher)() 'In Global.asax: services.AddSingleton(Of SqsPublisher)()

                ' Param of PublishMessageAsync (method that I defined): message object, which contains S3KeyName string
                ' PublishMessageAsync serializes the message object into a JSON string before sending to Amazon SQS (because AmazonSQS message body only accepts strings - xml, json, or plain text)
                ' The publisher class will be responsible for using sqsClient
                Await sqsPublisher.PublishMessageAsync(message) ' Note: whole method must be Async to use Await

                ResultLabel.Text = "File uploaded!"
            Else
                ResultLabel.Text = "Please upload either a JPEG or a PNG file."
            End If

        Else
            ' Provide feedback to the user
            ResultLabel.Text = "No file selected."
        End If
    End Sub
End Class