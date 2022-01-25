﻿namespace Fabulous

open System.Collections.Generic
open Fabulous

/// Define the logic to apply diffs and store event handlers of its target control
[<Sealed>]
type ViewNode(parentNode: IViewNode voption, treeContext: ViewTreeContext, targetRef: System.WeakReference) =

    let _handlers = Dictionary<IEventAttributeDefinition, obj>()
    let mutable _mapMsg: (obj -> obj) voption = ValueNone

    member inline private this.ApplyScalarDiffs(diffs: ScalarChanges inref) =
        for diff in diffs do
            match diff with
            | ScalarChange.Added added ->
                added.Definition.UpdateNode (ValueSome added.Value) this

            | ScalarChange.Removed removed ->
                removed.Definition.UpdateNode ValueNone this

            | ScalarChange.Updated newAttr ->
                newAttr.Definition.UpdateNode (ValueSome newAttr.Value) this

    member inline private this.ApplyEventDiffs(diffs: EventChanges inref) =
        for diff in diffs do
            match diff with
            | EventChange.Added added ->
                added.Definition.AddHandler (ValueSome added.Value) this

            | EventChange.Removed removed ->
                removed.Definition.RemoveHandler this

            | EventChange.Updated newAttr ->
                newAttr.Definition.RemoveHandler this
                newAttr.Definition.AddHandler (ValueSome newAttr.Value) this

    member inline private this.ApplyWidgetDiffs(diffs: WidgetChanges inref) =
        for diff in diffs do
            match diff with
            | WidgetChange.Added newWidget
            | WidgetChange.ReplacedBy newWidget ->
                newWidget.Definition.CreateNode newWidget.Value (this :> IViewNode)

            | WidgetChange.Removed removed ->
                removed.Definition.RemoveNode (this :> IViewNode)

            | WidgetChange.Updated struct (newAttr, diffs) ->
                let childNode = newAttr.Definition.GetChildNode (this :> IViewNode) :?> IViewNodeWithDiff
                childNode.ApplyDiff(&diffs)

    member inline private this.ApplyWidgetCollectionDiffs(diffs: WidgetCollectionChanges inref) =
        for diff in diffs do
            match diff with
            | WidgetCollectionChange.Added added ->
                added.Definition.UpdateNode (ValueSome added.Value) (this :> IViewNode)

            | WidgetCollectionChange.Removed removed ->
                removed.Definition.UpdateNode ValueNone (this :> IViewNode)

            | WidgetCollectionChange.Updated struct (newAttr, updatedDiffs) ->
                for updatedDiff in updatedDiffs do
                    match updatedDiff with
                    | WidgetCollectionItemChange.Remove index -> newAttr.Definition.Remove (this :> IViewNode) index
                    | _ -> ()

                for updatedDiff in updatedDiffs do
                    match updatedDiff with
                    | WidgetCollectionItemChange.Insert (index, widget) ->
                        let view = Helpers.createViewForWidget this widget
                        newAttr.Definition.Insert (this :> IViewNode) index view

                    | WidgetCollectionItemChange.Update (index, widgetDiff) ->
                        let itemNode = newAttr.Definition.GetItemNode (this :> IViewNode) index :?> IViewNodeWithDiff
                        itemNode.ApplyDiff(&widgetDiff)

                    | WidgetCollectionItemChange.Replace (index, widget) ->
                        let view = Helpers.createViewForWidget this widget
                        newAttr.Definition.Replace (this :> IViewNode) index view

                    | _ -> ()

    interface IViewNode with
        member _.Target = targetRef.Target
        member _.Parent = parentNode

    interface IViewNodeWithContext with
        member _.TreeContext = treeContext
    
    interface IViewNodeWithDiff with
        member x.ApplyDiff(diff) =
            if not targetRef.IsAlive then
                ()
            else
                x.ApplyScalarDiffs(&diff.ScalarChanges)
                x.ApplyEventDiffs(&diff.EventChanges)
                x.ApplyWidgetDiffs(&diff.WidgetChanges)
                x.ApplyWidgetCollectionDiffs(&diff.WidgetCollectionChanges)

    interface IViewNodeWithEvents with        
        member _.TryGetHandler<'T>(definition: IEventAttributeDefinition) =
            match _handlers.TryGetValue(definition) with
            | false, _ -> ValueNone
            | true, v -> ValueSome(unbox<'T> v)

        member _.SetHandler<'T>(definition: IEventAttributeDefinition, handlerOpt: 'T voption) =
            match handlerOpt with
            | ValueNone -> _handlers.Remove(definition) |> ignore
            | ValueSome handler -> _handlers.[definition] <- handler
            
        member _.Dispatch(msg: obj) =
            let mutable parentOpt = parentNode
            
            let mutable mapMsg =
                match _mapMsg with
                | ValueNone -> id
                | ValueSome fn -> fn

            while parentOpt.IsSome do
                let parent = parentOpt.Value :?> IMappedViewNode
                parentOpt <- parent.Parent

                mapMsg <-
                    match parent.MapMsg with
                    | ValueNone -> mapMsg
                    | ValueSome fn -> mapMsg >> fn

            let newMsg = mapMsg msg
            treeContext.Dispatch(newMsg)
            
    interface IMappedViewNode with
        member _.MapMsg
            with get () = _mapMsg
            and set value = _mapMsg <- value
        
    interface ILazyViewNode with
        member val MemoizedWidget: Widget option = None with get, set
        
