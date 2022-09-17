using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
	public static ThreadedDataRequester Instance;

	private struct ThreadInfo
	{
		public readonly Action<object> Callback;
		public readonly object Parameter;

		public ThreadInfo(Action<object> callback, object parameter)
		{
			Callback = callback;
			Parameter = parameter;
		}
	}

	private Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(this);
			return;
		}

		Instance = this;
	}

	private void Update()
	{
		if (dataQueue.Count > 0)
		{
			for (int i = 0; i < dataQueue.Count; i++)
			{
				ThreadInfo threadInfo = dataQueue.Dequeue();
				threadInfo.Callback(threadInfo.Parameter);
			}
		}
	}

	public static void RequestData(Func<object> generateData, Action<object> callback)
	{
		ThreadStart threadStart = delegate
		{
			Instance.DataThread(generateData, callback);
		};

		new Thread(threadStart).Start();
	}

	private void DataThread(Func<object> generateData, Action<object> callback)
	{
		object data = generateData();
		lock (dataQueue)
		{
			dataQueue.Enqueue(new ThreadInfo(callback, data));
		}
	}
}
