module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Fable.Core.JsInterop

open Shared

open Microsoft.FSharp.Reflection

type Model = {
    Input: float option
    Source: Length
    Target: Length
    Result: float
    Error: exn option }

type Msg =
    | UpdateInput of string
    | UpdateSource of Length
    | UpdateTarget of Length
    | Convert of Conversion
    | ConversionOk of float
    | ConversionErr of exn

module Server =

    open Shared
    open Fable.Remoting.Client

    let api : IConverterApi =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<IConverterApi>

let init () : Model * Cmd<Msg> =
    let initialModel = {
        Input = None
        Source = Meter
        Target = Meter
        Result = 0.0
        Error = None }
    initialModel, Cmd.none

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match msg with
    | UpdateInput i ->
        let success, f = System.Double.TryParse i
        let input = if success then Some f else None
        { model with Input = input }, Cmd.none
    | UpdateSource s -> { model with Source = s }, Cmd.none
    | UpdateTarget t -> { model with Target = t }, Cmd.none
    | Convert c ->
        model, Cmd.OfAsync.either Server.api.convert c ConversionOk ConversionErr
    | ConversionOk r -> { model with Result = r }, Cmd.none
    | ConversionErr e -> { model with Error = Some e }, Cmd.none

let button isDisabled txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.Disabled isDisabled
          Button.Color IsPrimary
          Button.OnClick onClick ]
        [ str txt ]

let unitDropdown dispatch message selected =
    let construct (caseInfo: UnionCaseInfo) = FSharpValue.MakeUnion(caseInfo, [||]) :?> Length
    let lengths = FSharpType.GetUnionCases(typeof<Length>) |> Array.map construct |> Array.toList

    let option length = option [ Value length ] [ str (string length) ]
    let options = lengths |> List.map option

    Column.column [] [
        select [
            Value selected
            OnChange (fun ev -> message ev.target?value |> dispatch ) ]
            options ]

let inputView model dispatch =
    seq {
        yield input [
            Placeholder "Enter length"
            OnChange (fun ev -> UpdateInput !!ev.target?value |> dispatch) ]
        if model.Input.IsNone then yield Text.p [] [ str "Must be a number"]
    } |> Seq.toList

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "Unit Conversion" ] ] ]

          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ Heading.h3 [] [ str (string model.Result) ] ]
                Columns.columns []
                    [
                      Column.column [] (inputView model dispatch)
                      unitDropdown dispatch UpdateSource model.Source
                      unitDropdown dispatch UpdateTarget model.Target
                      Column.column []
                        [ button
                            model.Input.IsNone
                            "Convert"
                            (fun _ ->
                                Convert { Source = model.Source; Target = model.Target; Input = model.Input.Value }
                                |> dispatch ) ]
                                ] ] ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
