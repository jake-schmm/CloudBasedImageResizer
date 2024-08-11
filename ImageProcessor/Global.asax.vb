Imports Amazon
Imports Amazon.Extensions.NETCore.Setup
Imports Amazon.S3
Imports Amazon.SQS
Imports MassTransit
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports System.Configuration
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web.Optimization

Public Class Global_asax
    Inherits HttpApplication

    Private Shared _busControl As IBusControl
    Private Shared _serviceProvider As IServiceProvider

    Sub Application_Start(sender As Object, e As EventArgs)
        ' Fires when the application is started
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)

        ' Configure services
        Dim serviceCollection As New ServiceCollection()
        ConfigureServices(serviceCollection)
        ConfigureMassTransit(serviceCollection) ' RabbitMQ configuration

        ' Build the service provider
        _serviceProvider = serviceCollection.BuildServiceProvider()

        ' Start hosted services
        StartHostedServicesAsync(_serviceProvider).GetAwaiter().GetResult()

        ' Start MassTransit
        StartBusControlAsync(_serviceProvider).GetAwaiter().GetResult()
    End Sub

    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        _busControl?.Stop()

        ' Stop all registered hosted services asynchronously during application shutdown.
        StopHostedServicesAsync(_serviceProvider).GetAwaiter().GetResult()
        If _serviceProvider IsNot Nothing Then
            DirectCast(_serviceProvider, IDisposable).Dispose()
        End If
    End Sub

    Private Sub ConfigureServices(ByVal services As IServiceCollection)
        ' Add logging
        services.AddLogging(Function(builder) builder.AddConsole().SetMinimumLevel(LogLevel.Debug))

        ' Read AWS region from Web.config
        Dim regionName As String = ConfigurationManager.AppSettings("AWSRegion")
        Dim region As Amazon.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(regionName)

        ' Configure AWS options
        Dim awsOptions = New AWSOptions() With {
            .Region = region
        }

        ' Add AWS services
        services.AddDefaultAWSOptions(awsOptions)
        services.AddSingleton(Of IAmazonSQS)(New AmazonSQSClient())
        services.AddSingleton(Of IAmazonS3)(New AmazonS3Client())

        services.AddSingleton(Of SqsPublisher)()
    End Sub

    Private Async Function StartBusControlAsync(serviceProvider As IServiceProvider) As Task
        _busControl = serviceProvider.GetRequiredService(Of IBusControl)()
        Try
            Await _busControl.StartAsync()
        Catch ex As Exception
            ' Log the error and handle it appropriately
            Dim logger = serviceProvider.GetRequiredService(Of ILogger(Of Global_asax))()
            logger.LogError(ex, "Error starting bus control")
            ' Consider shutting down the application or taking other appropriate actions
        End Try
    End Function

    Private Async Function StartHostedServicesAsync(serviceProvider As IServiceProvider) As Task
        ' Retrieve all hosted services and start them
        Dim hostedServices = serviceProvider.GetServices(Of IHostedService)()
        For Each hostedService In hostedServices
            Await hostedService.StartAsync(CancellationToken.None)
        Next
    End Function

    Private Async Function StopHostedServicesAsync(serviceProvider As IServiceProvider) As Task
        ' Retrieve all hosted services and stop them
        Dim hostedServices = serviceProvider.GetServices(Of IHostedService)()
        For Each hostedService In hostedServices
            Await hostedService.StopAsync(CancellationToken.None)
        Next
    End Function

    Private Sub ConfigureMassTransit(ByVal services As IServiceCollection)
        services.AddMassTransit(Function(x)
                                    x.UsingRabbitMq(AddressOf ConfigureBus)
                                    Return x
                                End Function)

        services.AddSingleton(Of IBusControl)(Function(provider)
                                                  Return Bus.Factory.CreateUsingRabbitMq(Function(cfg)
                                                                                             ConfigureBus(provider.GetRequiredService(Of IBusRegistrationContext)(), cfg)
                                                                                             Return cfg
                                                                                         End Function)
                                              End Function)

        services.AddSingleton(Of IBus)(Function(provider) provider.GetRequiredService(Of IBusControl)())
    End Sub

    Private Sub ConfigureBus(ByVal context As IBusRegistrationContext, ByVal cfg As IRabbitMqBusFactoryConfigurator)
        cfg.Host("localhost", "/", Sub(h)
                                       h.Username("guest")
                                       h.Password("guest")
                                   End Sub)
    End Sub

    Public Shared ReadOnly Property BusControl As IBusControl
        Get
            Return _busControl
        End Get
    End Property

    Public Shared ReadOnly Property ServiceProvider As IServiceProvider
        Get
            Return _serviceProvider
        End Get
    End Property
End Class
