<%@ Page Title="Upload Using RabbitMQ" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RabbitMQExample.aspx.vb" Inherits="ImageProcessor._RabbitMQExample" Async="true"  %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <main>
        <!DOCTYPE html>
        <html xmlns="http://www.w3.org/1999/xhtml">
        <body>
            <h2 id="title"><%: Title %></h2>
            <p>Upload a JPEG or PNG image, and it will be resized to 250 x 200 and uploaded to an "Uploads" directory on your C:\ drive.</p>
            <div>
                <asp:FileUpload ID="FileUpload1" runat="server" />
                <asp:Button ID="UploadButton" runat="server" Text="Upload" OnClick="UploadButton_Click" />
            </div>
            <div>
                <asp:Label ID="ResultLabel" runat="server" Text=""></asp:Label>
            </div>
        </body>
        </html>
    </main>

</asp:Content>
