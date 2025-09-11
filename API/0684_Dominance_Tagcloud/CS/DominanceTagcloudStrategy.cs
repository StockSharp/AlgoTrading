using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dominance Tagcloud Strategy (684).
/// Generates random rectangles sized by dominance values.
/// </summary>
public class DominanceTagcloudStrategy : Strategy
{
	private readonly StrategyParam<bool> _moveBoxes;
	private readonly StrategyParam<bool> _labelAtSide;

	private readonly Random _random = new();
	private readonly List<string> _tickers = new();
	private readonly List<decimal> _dominance = new();

	private const int Min = 100;
	private const int Max = 500;

	/// <summary>
	/// Reposition boxes every bar.
	/// </summary>
	public bool MoveBoxes
	{
		get => _moveBoxes.Value;
		set => _moveBoxes.Value = value;
	}

	/// <summary>
	/// Show labels at chart side.
	/// </summary>
	public bool LabelAtSide
	{
		get => _labelAtSide.Value;
		set => _labelAtSide.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DominanceTagcloudStrategy"/>.
	/// </summary>
	public DominanceTagcloudStrategy()
	{
		_moveBoxes = Param(nameof(MoveBoxes), true)
			.SetDisplay("Moving boxes?", "Randomly move boxes", "Visualization")
			.SetCanOptimize(false);

		_labelAtSide = Param(nameof(LabelAtSide), true)
			.SetDisplay("Label at the side", "Place labels at chart side", "Visualization")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		FillDominance();

		var rectangles = new List<Rect>();

		for (var i = 0; i < _tickers.Count; i++)
		{
			Rect rect;
			var attempts = 0;

			do
			{
				rect = MakeRect(i);
				attempts++;
			}
			while (HasOverlap(rectangles, rect) && attempts < 100);

			rectangles.Add(rect);

			this.Log().Info(rect.Text);
		}
	}

	private void FillDominance()
	{
		AddDominance("BTC");
		AddDominance("ETH");
		AddDominance("BNB");
		AddDominance("DOT");
		AddDominance("ADA");
		AddDominance("XRP");
		AddDominance("LTC");
		AddDominance("BCH");
		AddDominance("XLM");
		AddDominance("EOS");
		AddDominance("XMR");
		AddDominance("TRX");
		AddDominance("BSV");
		AddDominance("MIOTA");
		AddDominance("OTHERS");
		AddDominance("USDT");
	}

	private void AddDominance(string name)
	{
		_tickers.Add(name);
		var value = (decimal)_random.Next(1, 50);
		_dominance.Add(value);
	}

	private Rect MakeRect(int index)
	{
		var dom = _dominance[index];

		var randX = _random.Next(Min, Max);
		var randY = _random.Next(Min, Max);

		var x1 = randX;
		var x2 = randX + (int)Math.Round(5m * dom);
		var y1 = randY;
		var y2 = y1 + 5m * dom;

		var xLabel = (x1 + x2) / 2;
		var yLabel = (y1 + y2) / 2;
		var text = $"{_tickers[index]} {Math.Round(dom, 2)}";

		return new Rect
		{
			X1 = x1,
			Y1 = y1,
			X2 = x2,
			Y2 = y2,
			XLabel = xLabel,
			YLabel = yLabel,
			Text = text
		};
	}

	private static bool HasOverlap(List<Rect> rects, Rect rect)
	{
		foreach (var r in rects)
		{
			if (Overlap(r, rect))
				return true;
		}

		return false;
	}

	private static bool Overlap(Rect a, Rect b)
	{
		var overlapX =
			(a.X2 >= b.X2 && a.X2 <= b.X1) || (a.X2 <= b.X2 && a.X1 >= b.X2) ||
			(b.X2 >= a.X2 && b.X2 <= a.X1) || (b.X2 <= a.X2 && b.X1 >= a.X2);

		var overlapY =
			(a.Y1 >= b.Y1 && a.Y1 <= b.Y2) || (a.Y1 <= b.Y1 && a.Y2 >= b.Y1) ||
			(b.Y1 >= a.Y1 && b.Y1 <= a.Y2) || (b.Y1 <= a.Y1 && b.Y2 >= a.Y1);

		return overlapX && overlapY;
	}

	private class Rect
	{
		public int X1;
		public decimal Y1;
		public int X2;
		public decimal Y2;
		public int XLabel;
		public decimal YLabel;
		public string Text;
	}
}