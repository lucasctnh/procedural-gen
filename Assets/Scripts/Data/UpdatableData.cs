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
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
	}

	public void NotifyOfUpdatedValues()
	{
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		OnValuesUpdated?.Invoke();
	}
}
