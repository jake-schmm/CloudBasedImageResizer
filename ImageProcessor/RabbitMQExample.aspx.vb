Imports System.IO


Partial Class _RabbitMQExample
    Inherits System.Web.UI.Page
    Protected Async Sub UploadButton_Click(sender As Object, e As EventArgs)
        If FileUpload1.HasFile Then
            Dim fileExtension As String = Path.GetExtension(FileUpload1.FileName).ToLower()
            If fileExtension = ".jpg" OrElse fileExtension = ".jpeg" OrElse fileExtension = ".png" Then
                Dim uploadDir As String = "C:/Uploads/"
                ' Check if the directory exists, if not, create it
                If Not Directory.Exists(uploadDir) Then
                    Directory.CreateDirectory(uploadDir)
                End If

                ' Save the uploaded file to the directory
                Dim filePath As String = Path.Combine(uploadDir, Path.GetFileName(FileUpload1.FileName))
                FileUpload1.SaveAs(filePath)

                ' Publish message using publisher - use asynchronous publihsing (await this) - note: whole method must be async to use await
                Await Global_asax.BusControl.Publish(New ModelLibrary.ImageMessageRabbitMQ With {.FilePath = filePath})

                ResultLabel.Text = "File uploaded successfully!"
            Else
                ResultLabel.Text = "Please upload either a JPEG or a PNG file."
            End If
        Else
                ' Provide feedback to the user
                ResultLabel.Text = "No file selected."
        End If
    End Sub
End Class