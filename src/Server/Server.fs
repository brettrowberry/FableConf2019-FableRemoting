open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open FSharp.Control.Tasks.V2
open Giraffe
open Shared

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"
let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let conversionApi : IConverterApi = {
    convert = fun c -> async { return  Length.convert c }
}

let docs = Docs.createFor<IConverterApi>()
let converterApiDocs =
    Remoting.documentation "Conversion API" [
        docs.route <@ fun api -> api.convert @>
        |> docs.alias "Convert"
        |> docs.description "Description"
        |> docs.example <@ fun api -> api.convert { Source = Kilometer; Target = Meter; Input = 1.0 } @>
        |> docs.example <@ fun api -> api.convert { Source = Meter; Target = USFoot; Input = 1.0 } @>
    ]

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue conversionApi
    |> Remoting.withDocs "/api/convert/docs" converterApiDocs
    |> Remoting.buildHttpHandler


let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    .Run()
