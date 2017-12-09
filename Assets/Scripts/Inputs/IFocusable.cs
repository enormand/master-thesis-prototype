﻿using System;

namespace NormandErwan.MasterThesis.Experiment.Inputs
{
  public interface IFocusable : IInteractable
  {
    // Properties

    bool IsFocused { get; }

    // Events

    event Action<IFocusable> Focused;
    
    // Methods

    void SetFocused(bool value);
  }
}