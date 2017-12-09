﻿using UnityEngine;

namespace NormandErwan.MasterThesis.Experiment.Inputs
{
  public interface ICursor
  {
    CursorType Type { get; }
    Transform Transform { get; }
  }
}