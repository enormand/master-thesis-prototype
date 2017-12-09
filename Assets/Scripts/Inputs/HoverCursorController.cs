﻿using Hover.Core.Cursors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NormandErwan.MasterThesis.Experiment.Inputs
{
  [RequireComponent(typeof(HoverCursorData))]
  [RequireComponent(typeof(Collider))]
  public class HoverCursorController : MonoBehaviour, ICursor
  {
    // Constants

    public static readonly float longPressTimeout = 0.5f; // in seconds
    public static readonly float tapTimeout = 0.3f; // in seconds

    // ICursor properties

    public CursorType Type { get; protected set; }
    public Transform Transform { get { return transform; } }
    
    // Properties

    public HoverCursorData HoverCursorData { get; protected set; }

    public bool IsFinger { get { return Type != CursorType.Look; } }
    public bool IsIndex { get { return Type == CursorType.LeftIndex || Type == CursorType.RightIndex; } }
    public bool IsThumb { get { return Type == CursorType.LeftThumb || Type == CursorType.RightThumb; } }
    public bool IsMiddle { get { return Type == CursorType.LeftMiddle || Type == CursorType.RightMiddle; } }

    // Variables

    protected static Dictionary<ITransformable, Dictionary<HoverCursorController, Vector3>> latestCursorPositions 
      = new Dictionary<ITransformable, Dictionary<HoverCursorController, Vector3>>();

    protected Dictionary<ILongPressable, float> longPressTimers = new Dictionary<ILongPressable, float>();
    protected Dictionary<ITappable, float> tapTimers = new Dictionary<ITappable, float>();

    // Methods

    protected void Awake()
    {
      HoverCursorData = GetComponent<HoverCursorData>();
      switch (HoverCursorData.Type)
      {
        case Hover.Core.Cursors.CursorType.LeftThumb:   Type = CursorType.LeftThumb;    break;
        case Hover.Core.Cursors.CursorType.LeftIndex:   Type = CursorType.LeftIndex;    break;
        case Hover.Core.Cursors.CursorType.LeftMiddle:  Type = CursorType.LeftMiddle;   break;
        case Hover.Core.Cursors.CursorType.LeftRing:    Type = CursorType.LeftRing;     break;
        case Hover.Core.Cursors.CursorType.LeftPinky:   Type = CursorType.LeftPinky;    break;
        case Hover.Core.Cursors.CursorType.RightThumb:  Type = CursorType.RightThumb;   break;
        case Hover.Core.Cursors.CursorType.RightIndex:  Type = CursorType.RightIndex;   break;
        case Hover.Core.Cursors.CursorType.RightMiddle: Type = CursorType.RightMiddle;  break;
        case Hover.Core.Cursors.CursorType.RightRing:   Type = CursorType.RightRing;    break;
        case Hover.Core.Cursors.CursorType.RightPinky:  Type = CursorType.RightPinky;   break;
        case Hover.Core.Cursors.CursorType.Look:        Type = CursorType.Look;         break;
      }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
      GetInteractable<IInteractable>(other, (interactable) =>
      {
        if (!interactable.IsInteractable)
        {
          return;
        }
      });

      GetInteractable<IFocusable>(other, (focusable) =>
      {
        focusable.SetFocused(true);
      });

      if (IsFinger)
      {
        GetInteractable<ITransformable>(other, (transformable) =>
        {
          if (!latestCursorPositions.ContainsKey(transformable))
          {
            latestCursorPositions.Add(transformable, new Dictionary<HoverCursorController, Vector3>());
          }
          latestCursorPositions[transformable].Add(this, transform.position);
        });

        GetInteractable<IDraggable>(other, (draggable) =>
        {
          if (latestCursorPositions[draggable].Count > 1 && draggable.IsDragging)
          {
            draggable.SetDragging(false); // Only one finger can drag, cancel if more than one finger
          }
        });

        GetInteractable<IZoomable>(other, (zoomable) =>
        {
          if (latestCursorPositions[zoomable].Count == 2 && !zoomable.IsZooming)
          {
            zoomable.SetZooming(true);
          }
          else if (latestCursorPositions[zoomable].Count > 2 && zoomable.IsZooming)
          {
            zoomable.SetZooming(false);
          }
        });

        GetInteractable<ILongPressable>(other, (longPressable) =>
        {
          if (longPressable.IsSelectable)
          {
            longPressTimers.Add(longPressable, 0);
          }
        });

        GetInteractable<ITappable>(other, (tappable) =>
        {
          if (tappable.IsSelectable)
          {
            tapTimers.Add(tappable, 0);
          }
        });
      }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
      if (IsFinger)
      {
        GetInteractable<IDraggable>(other, (draggable) =>
        {
          if (draggable.IsInteractable)
          {
            var translation = transform.position - latestCursorPositions[draggable][this];
            if (draggable.IsDragging)
            {
              latestCursorPositions[draggable][this] = transform.position;
              draggable.Drag(translation);
            }
            else if (translation.magnitude > draggable.DistanceToStartDragging)
            {
              latestCursorPositions[draggable][this] = transform.position;
              draggable.SetDragging(true);
            }
          }
        });

        GetInteractable<IZoomable>(other, (zoomable) =>
        {
          if (zoomable.IsInteractable && zoomable.IsZooming)
          {
            var cursors = new List<HoverCursorController>(latestCursorPositions[zoomable].Keys);
            if (cursors[0] == this) // Update only once per frame
            {
              var translation = cursors[0].transform.position;
              var previousTranslation = latestCursorPositions[zoomable][cursors[0]];
              var distance = cursors[0].transform.position - cursors[1].transform.position;
              var previousDistance = latestCursorPositions[zoomable][cursors[0]] - latestCursorPositions[zoomable][cursors[1]];

              foreach (var cursor in cursors)
              {
                latestCursorPositions[zoomable][cursor] = cursor.transform.position;
              }

              zoomable.Zoom(distance, previousDistance, translation, previousTranslation);
            }
          }
        });

        GetInteractable<ILongPressable>(other, (longPressable) =>
        {
          if (longPressTimers.ContainsKey(longPressable))
          {
            if (longPressTimers[longPressable] < longPressTimeout)
            {
              longPressTimers[longPressable] += Time.deltaTime;
            }
            else
            {
              if (longPressable.IsInteractable && longPressable.IsSelectable)
              {
                longPressable.SetSelected(true);
              }
              longPressTimers.Remove(longPressable);
            }
          }
        });

        GetInteractable<ITappable>(other, (tappable) =>
        {
          if (tapTimers.ContainsKey(tappable))
          {
            if (tapTimers[tappable] < tapTimeout)
            {
              tapTimers[tappable] += Time.deltaTime;
            }
          }
        });
      }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
      GetInteractable<IFocusable>(other, (focusable) =>
      {
        focusable.SetFocused(false);
      });

      if (IsFinger)
      {
        GetInteractable<ITransformable>(other, (transformable) =>
        {
          latestCursorPositions[transformable].Remove(this);
        });

        GetInteractable<IDraggable>(other, (draggable) =>
        {
          if (latestCursorPositions[draggable].Count == 0 && draggable.IsDragging)
          {
            draggable.SetDragging(false);
          }
        });

        GetInteractable<IZoomable>(other, (zoomable) =>
        {
          if (latestCursorPositions[zoomable].Count < 2 && zoomable.IsZooming)
          {
            zoomable.SetZooming(false);
          }
        });

        GetInteractable<ILongPressable>(other, (longPressable) =>
        {
          if (longPressTimers.ContainsKey(longPressable))
          {
            longPressTimers.Remove(longPressable);
          }
        });

        GetInteractable<ITappable>(other, (tappable) =>
        {
          if (tapTimers.ContainsKey(tappable))
          {
            if (tappable.IsInteractable && tappable.IsSelectable && tapTimers[tappable] < tapTimeout)
            {
              tappable.SetSelected(true);
            }
            tapTimers.Remove(tappable);
          }
        });
      }
    }

    protected virtual void GetInteractable<T>(Component component, Action<T> actionOnInteractable)
    {
      var interactable = component.GetComponent<T>();
      if (interactable != null)
      {
        actionOnInteractable(interactable);
      }
    }
  }
}