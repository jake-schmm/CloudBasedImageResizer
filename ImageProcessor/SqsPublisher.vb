Imports System.Threading.Tasks
Imports Amazon.Runtime
Imports Amazon.SQS
Imports Amazon.SQS.Model
Imports Microsoft.Extensions.Logging
Imports Newtonsoft.Json

Public Class SqsPublisher
    Private ReadOnly _sqsClient As IAmazonSQS
    Private ReadOnly _logger As ILogger(Of SqsPublisher)
    Private ReadOnly _queueUrl As String = ConfigurationManager.AppSettings("AWS_SQS_QUEUE_URL")  ' Configure queue url in Web.config

    Public Sub New(sqsClient As IAmazonSQS, logger As ILogger(Of SqsPublisher))
        _sqsClient = DirectCast(Global_asax.ServiceProvider.GetService(GetType(IAmazonSQS)), IAmazonSQS)
        _logger = logger
    End Sub

    Public Async Function PublishMessageAsync(message As ImageMessageAmazonSQS) As Task
        Try
            Dim messageBody As String = JsonConvert.SerializeObject(message)
            Dim sendMessageRequest = New SendMessageRequest With {
                .QueueUrl = _queueUrl,
                .MessageBody = messageBody
            }
            Dim sendMessageResponse = Await _sqsClient.SendMessageAsync(sendMessageRequest)
            _logger.LogInformation($"Message sent to queue with MessageId: {sendMessageResponse.MessageId}")
        Catch ex As Exception
            _logger.LogError(ex, "An error occurred while sending the message to SQS.")
        End Try
    End Function
End Class
