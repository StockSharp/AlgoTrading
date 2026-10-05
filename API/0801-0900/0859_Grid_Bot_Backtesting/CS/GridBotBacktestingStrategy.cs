using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid bot backtesting strategy.
/// GridLines lines are spread evenly from the lower to the upper bound. With AutoBounds the bounds are the BoundLookback highest high
/// and lowest low ("Hi &amp; Low") or the BoundLookback SMA of the close ("Average"), widened by BoundDeviation; otherwise UpperBound and
/// LowerBound are used. A close crossing below a line that holds no order buys one Volume for that line, and a close crossing above the
/// next line up sells it again. Only longs are opened.
/// </summary>
public class GridBotBacktestingStrategy : Strategy
{
	private const string _hiLowSource = "Hi & Low";

	private readonly StrategyParam<bool> _autoBounds;
	private readonly StrategyParam<string> _boundSource;
	private readonly StrategyParam<int> _boundLookback;
	private readonly StrategyParam<decimal> _boundDeviation;
	private readonly StrategyParam<decimal> _upperBound;
	private readonly StrategyParam<decimal> _lowerBound;
	private readonly StrategyParam<int> _gridLines;
	private readonly StrategyParam<DataType> _candleType;

	private bool[] _filled = [];
	private decimal? _prevClose;

	/// <summary>
	/// Calculate the bounds from recent data.
	/// </summary>
	public bool AutoBounds
	{
		get => _autoBounds.Value;
		set => _autoBounds.Value = value;
	}

	/// <summary>
	/// Source of the automatic bounds: "Hi &amp; Low" or "Average".
	/// </summary>
	public string BoundSource
	{
		get => _boundSource.Value;
		set => _boundSource.Value = value;
	}

	/// <summary>
	/// Candles used for the automatic bounds.
	/// </summary>
	public int BoundLookback
	{
		get => _boundLookback.Value;
		set => _boundLookback.Value = value;
	}

	/// <summary>
	/// Fraction the automatic bounds are widened by.
	/// </summary>
	public decimal BoundDeviation
	{
		get => _boundDeviation.Value;
		set => _boundDeviation.Value = value;
	}

	/// <summary>
	/// Manual upper bound.
	/// </summary>
	public decimal UpperBound
	{
		get => _upperBound.Value;
		set => _upperBound.Value = value;
	}

	/// <summary>
	/// Manual lower bound.
	/// </summary>
	public decimal LowerBound
	{
		get => _lowerBound.Value;
		set => _lowerBound.Value = value;
	}

	/// <summary>
	/// Number of grid lines.
	/// </summary>
	public int GridLines
	{
		get => _gridLines.Value;
		set => _gridLines.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GridBotBacktestingStrategy()
	{
		_autoBounds = Param(nameof(AutoBounds), true)
			.SetDisplay("Auto Bounds", "Calculate the bounds from recent data", "Grid");

		_boundSource = Param(nameof(BoundSource), _hiLowSource)
			.SetDisplay("Bound Source", "Source of the automatic bounds: Hi & Low or Average", "Grid");

		_boundLookback = Param(nameof(BoundLookback), 250)
			.SetGreaterThanZero()
			.SetDisplay("Bound Lookback", "Candles used for the automatic bounds", "Grid");

		_boundDeviation = Param(nameof(BoundDeviation), 0.10m)
			.SetNotNegative()
			.SetDisplay("Bound Deviation", "Fraction the automatic bounds are widened by", "Grid");

		_upperBound = Param(nameof(UpperBound), 0.285m)
			.SetGreaterThanZero()
			.SetDisplay("Upper Bound", "Manual upper bound", "Grid");

		_lowerBound = Param(nameof(LowerBound), 0.225m)
			.SetGreaterThanZero()
			.SetDisplay("Lower Bound", "Manual lower bound", "Grid");

		_gridLines = Param(nameof(GridLines), 30)
			.SetRange(2, 1000)
			.SetDisplay("Grid Lines", "Number of grid lines", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_filled = [];
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_filled = new bool[GridLines];
		_prevClose = null;

		var highest = new Highest { Length = BoundLookback };
		var lowest = new Lowest { Length = BoundLookback };
		var average = new SimpleMovingAverage { Length = BoundLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, average, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal average)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading() || prevClose is not decimal pc)
			return;

		decimal upper, lower;

		if (AutoBounds)
		{
			var hiLow = BoundSource == _hiLowSource;
			upper = (hiLow ? highest : average) * (1 + BoundDeviation);
			lower = (hiLow ? lowest : average) * (1 - BoundDeviation);
		}
		else
		{
			upper = UpperBound;
			lower = LowerBound;
		}

		if (upper <= lower)
			return;

		var close = candle.ClosePrice;
		var count = _filled.Length;
		var step = (upper - lower) / (count - 1);

		for (var i = 0; i < count; i++)
		{
			var line = lower + step * i;

			if (!_filled[i] && pc > line && close <= line)
			{
				BuyMarket(Volume);
				_filled[i] = true;
			}
			else if (_filled[i] && i + 1 < count)
			{
				var next = line + step;

				if (pc < next && close >= next)
				{
					SellMarket(Volume);
					_filled[i] = false;
				}
			}
		}
	}
}
