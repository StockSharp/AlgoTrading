namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using StockSharp.Charting;

/// <summary>
/// A chart that draws nothing and remembers what a strategy asked it to show: the areas in the order
/// they were created, the elements placed on each area and the values each element received in draw
/// data that was submitted to the chart.
/// </summary>
sealed class ChartRecorder
{
	private readonly Lock _sync = new();
	private readonly List<IChartArea> _areas = [];
	private readonly List<(IChartArea Area, IChartElement Element)> _elements = [];
	private readonly List<DrawnValue> _drawn = [];

	public ChartRecorder()
	{
		Chart = ChartRecorderProxy.Create<IChart>(this);
	}

	/// <summary>
	/// The chart to hand to the strategy before it starts.
	/// </summary>
	public IChart Chart { get; }

	/// <summary>
	/// Areas in the order the strategy created them.
	/// </summary>
	public IChartArea[] Areas
	{
		get
		{
			using (_sync.EnterScope())
				return [.. _areas];
		}
	}

	/// <summary>
	/// Elements the strategy placed on the area.
	/// </summary>
	public IChartElement[] ElementsOf(IChartArea area)
	{
		using (_sync.EnterScope())
			return [.. _elements.Where(e => ReferenceEquals(e.Area, area)).Select(e => e.Element)];
	}

	/// <summary>
	/// Values the element received in draw data that reached the chart.
	/// </summary>
	public DrawnValue[] DrawnOn(IChartElement element)
	{
		using (_sync.EnterScope())
			return [.. _drawn.Where(v => ReferenceEquals(v.Element, element))];
	}

	internal void AddArea(IChartArea area)
	{
		using (_sync.EnterScope())
			_areas.Add(area);
	}

	internal void AddElement(IChartArea area, IChartElement element)
	{
		using (_sync.EnterScope())
			_elements.Add((area, element));
	}

	internal void Draw(IEnumerable<DrawnValue> values)
	{
		using (_sync.EnterScope())
			_drawn.AddRange(values);
	}

	/// <summary>
	/// One value put on an element: the time of its group and the arguments that followed the element.
	/// </summary>
	public readonly record struct DrawnValue(DateTime Time, IChartElement Element, object[] Values);
}
