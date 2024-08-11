Imports System.Web.Routing
Imports Microsoft.AspNet.FriendlyUrls

Public Module RouteConfig
    Sub RegisterRoutes(ByVal routes As RouteCollection)
        Dim settings As FriendlyUrlSettings = New FriendlyUrlSettings() With {
            .AutoRedirectMode = RedirectMode.Permanent
        }
        routes.EnableFriendlyUrls(settings)
        routes.MapPageRoute("DefaultRoute", "", "~/AmazonSQSExample.aspx") ' Make AmazonSQSExample the default home page
    End Sub
End Module
