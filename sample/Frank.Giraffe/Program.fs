module Frank.Giraffe.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Logging
open Giraffe
open Frank.Builder
open Frank.Giraffe.Extensions

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open GiraffeViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "Frank.Giraffe" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "Frank.Giraffe" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view

let helloWorld =
    resource "/" {
        name "Hello World"
        get (indexHandler "world")
    }

let helloName =
    resource "/hello/{name}" {
        name "Hello Name"
        get (fun next ctx ->
            let name = ctx.GetRouteValue("name") |> string
            indexHandler name next ctx)
    }

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder:CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureLogging (builder:ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug()

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")

    webHost args {
        configure (fun bldr ->
            bldr.UseKestrel()
                .UseContentRoot(contentRoot)
                .UseIISIntegration()
                .UseWebRoot(webRoot))
        logging configureLogging
        useCors configureCors
        // enable Giraffe content negotiation
        service (fun services -> services.AddGiraffe())

        plugWhen isDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage
        plugWhenNot isDevelopment (fun app -> app.UseGiraffeErrorHandler(errorHandler))

        plug HttpsPolicyBuilderExtensions.UseHttpsRedirection
        plug StaticFileExtensions.UseStaticFiles

        resource helloWorld
        resource helloName
    }

    0