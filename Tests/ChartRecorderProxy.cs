namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using StockSharp.Charting;

/// <summary>
/// Implements any chart interface for <see cref="ChartRecorder"/>. Properties keep what was assigned,
/// factories return further proxies, and the calls that build or submit a picture are reported to the
/// recorder.
/// </summary>
public class ChartRecorderProxy : DispatchProxy
{
	private readonly Lock _sync = new();
	private readonly Dictionary<string, object> _properties = [];
	private readonly List<ChartRecorder.DrawnValue> _pending = [];
	private ChartRecorder _recorder;
	private ChartRecorderProxy _data;
	private DateTime _time;

	internal static T Create<T>(ChartRecorder recorder)
		=> (T)CreateProxy(typeof(T), recorder);

	/// <inheritdoc />
	protected override object Invoke(MethodInfo method, object[] args)
	{
		args ??= [];

		switch (method.Name)
		{
			case nameof(IChartBuilder.CreateArea):
			{
				var area = (ChartRecorderProxy)CreateProxy(method.ReturnType, _recorder);
				area.SetProperty(nameof(IChartArea.Chart), this);
				return area;
			}

			case nameof(IChart.AddArea) when args is [IChartArea area]:
				_recorder.AddArea(area);
				return null;

			case nameof(IChart.AddElement) when args is [IChartArea area, IChartElement element, ..]:
				_recorder.AddElement(area, element);
				return null;

			case nameof(IChartDrawData.Group) when args is [DateTime time]:
			{
				var item = (ChartRecorderProxy)CreateProxy(method.ReturnType, _recorder);
				item._data = this;
				item._time = time;
				return item;
			}

			// Only a group item has an owning draw data; its values count once that data is drawn.
			case nameof(IChartDrawData.IChartDrawDataItem.Add) when _data is not null && args is [IChartElement element, .. var values]:
				_data.Enqueue(new(_time, element, values));
				return this;

			case nameof(IThemeableChart.Draw) when args is [ChartRecorderProxy data]:
				_recorder.Draw(data.TakePending());
				return null;
		}

		if (method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal) && args.Length == 1)
		{
			SetProperty(method.Name[4..], args[0]);
			return null;
		}

		if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal) && args.Length == 0)
			return GetProperty(method.Name[4..], method.ReturnType);

		if (method.ReturnType.IsInterface && method.ReturnType.IsInstanceOfType(this))
			return this;

		return CreateDefault(method.ReturnType);
	}

	private static object CreateProxy(Type interfaceType, ChartRecorder recorder)
	{
		var proxy = (ChartRecorderProxy)Create(interfaceType, typeof(ChartRecorderProxy));
		proxy._recorder = recorder;
		return proxy;
	}

	private object CreateDefault(Type type)
	{
		if (type == typeof(void) || type.ContainsGenericParameters)
			return null;

		if (type.IsValueType)
			return Activator.CreateInstance(type);

		return type.IsInterface ? CreateProxy(type, _recorder) : null;
	}

	private void SetProperty(string name, object value)
	{
		using (_sync.EnterScope())
			_properties[name] = value;
	}

	private object GetProperty(string name, Type type)
	{
		using (_sync.EnterScope())
		{
			if (!_properties.TryGetValue(name, out var value))
				_properties[name] = value = CreateDefault(type);

			return value;
		}
	}

	private void Enqueue(ChartRecorder.DrawnValue value)
	{
		using (_sync.EnterScope())
			_pending.Add(value);
	}

	private ChartRecorder.DrawnValue[] TakePending()
	{
		using (_sync.EnterScope())
		{
			ChartRecorder.DrawnValue[] values = [.. _pending];
			_pending.Clear();
			return values;
		}
	}
}
