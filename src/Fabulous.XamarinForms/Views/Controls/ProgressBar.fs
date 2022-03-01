namespace Fabulous.XamarinForms

open Fabulous.XamarinForms
open Xamarin.Forms
open System.Runtime.CompilerServices
open Fabulous

type IProgressBar =
    inherit IView

module ProgressBar =

    let WidgetKey = Widgets.register<ProgressBar> ()

    let ProgressColor =
        Attributes.defineAppThemeBindable<Color> ProgressBar.ProgressColorProperty

    let Progress =
        Attributes.defineBindable<float> ProgressBar.ProgressProperty

    let ProgressTo =
        Attributes.define<struct (float * uint32 * Easing)>
            "ProgressBar_ProgressTo"
            (fun newValueOpt node ->
                let view = node.Target :?> ProgressBar

                match newValueOpt with
                | ValueNone ->
                    view.ProgressTo(0., uint32 0, Easing.Linear)
                    |> Async.AwaitTask
                    |> ignore
                | ValueSome (progress, duration, easing) ->
                    view.ProgressTo(progress, duration, easing)
                    |> Async.AwaitTask
                    |> ignore)

[<AutoOpen>]
module ProgressBarBuilders =
    type Fabulous.XamarinForms.View with
        static member inline ProgressBar<'msg>(progress: float) =
            WidgetBuilder<'msg, IProgressBar>(ProgressBar.WidgetKey, ProgressBar.Progress.WithValue(progress))

[<Extension>]
type ProgressBarModifiers =
    /// <summary>Set the color of the progress bar</summary>
    /// <param name="light">The color of the progress bar in the light theme.</param>
    /// <param name="dark">The color of the progress bar in the dark theme.</param>
    [<Extension>]
    static member inline progressColor(this: WidgetBuilder<'msg, #IProgressBar>, light: Color, ?dark: Color) =
        this.AddScalar(ProgressBar.ProgressColor.WithValue(AppTheme.create light dark))

    [<Extension>]
    static member inline progressTo
        (
            this: WidgetBuilder<'msg, #IProgressBar>,
            value: float,
            duration: int,
            easing: Easing
        ) =
        this.AddScalar(ProgressBar.ProgressTo.WithValue(value, uint32 duration, easing))

    /// <summary>Link a ViewRef to access the direct ProgressBar control instance</summary>
    [<Extension>]
    static member inline reference(this: WidgetBuilder<'msg, IProgressBar>, value: ViewRef<ProgressBar>) =
        this.AddScalar(ViewRefAttributes.ViewRef.WithValue(value.Unbox))
