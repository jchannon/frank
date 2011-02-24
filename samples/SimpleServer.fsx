﻿// SimpleServer is an example of using Frack as a F# script.
#I @"..\lib\FSharp"
#I @"..\src\Frack\bin\Debug"
#I @"..\src\Frack.HttpListener\bin\Debug"
#r "System.Core.dll"
#r "System.Net.dll"
#r "System.Web.dll"
#r "System.Web.Abstractions.dll"
#r "frack.dll"
#r "Frack.Collections.dll"
#r "Frack.HttpListener.dll"

open System.Net
open System.Threading
open Frack
open Frack.Middleware
open Frack.Hosting.HttpListener

printfn "Creating server ..."

// Define the application function.
let app request = async { 
  let! body = request |> Request.readToEnd
  return "200 OK",
         dict [("Content_Type", "text/plain" )],
         seq { yield box "Howdy!"B
               yield box body } }

// Set up and start an HttpListener
let disposable = HttpListener.Start("http://localhost:9191/", app |> log)
printfn "Listening on http://localhost:9191/ ..."

Thread.Sleep(60 * 1000)
disposable.Dispose()
printfn "Stopped listening."