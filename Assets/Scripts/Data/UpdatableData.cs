using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event Action OnValuesUpdated;

    [SerializeField] private bool autoUpdate;

	protected virtual void OnValidate()
	{
		if (autoUpdate)
			NotifyOfUpdatedValues();
	}

	public void NotifyOfUpdatedValues() => OnValuesUpdated?.Invoke();
}
