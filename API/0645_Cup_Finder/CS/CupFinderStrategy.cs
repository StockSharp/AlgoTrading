using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cup pattern strategy: detects rounded bottoms or tops and trades breakouts.
/// </summary>
public class CupFinderStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _widthPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal? _leftPeak;
	private decimal? _cupLow;
	private bool _cupFormed;

	private decimal? _leftBottom;
	private decimal? _cupHigh;
	private bool _invertedCupFormed;

	/// <summary>
	/// Lookback period for peak and bottom detection.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Allowed width of the cup as percentage of peak/bottom price.
	/// </summary>
	public decimal WidthPercent
	{
		get => _widthPercent.Value;
		set => _widthPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CupFinderStrategy"/>.
	/// </summary>
	public CupFinderStrategy()
	{
		_lookback = Param(nameof(Lookback), 150)
			.SetRange(30, 250)
			.SetDisplay("Lookback", "Number of bars to search", "Pattern Parameters")
			.SetCanOptimize(true);

		_widthPercent = Param(nameof(WidthPercent), 5m)
			.SetRange(1m, 20m)
			.SetDisplay("Width %", "Maximum cup width in percent", "Pattern Parameters")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Percentage for stop-loss", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_leftPeak = null;
		_cupLow = null;
		_cupFormed = false;
		_leftBottom = null;
		_cupHigh = null;
		_invertedCupFormed = false;
		_highest = null;
		_lowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(0, UnitTypes.Absolute),
			new Unit(StopLossPercent, UnitTypes.Percent),
			false);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highest = _highest.Process(candle).ToDecimal();
		var lowest = _lowest.Process(candle).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Bullish cup detection
		if (_leftPeak == null || candle.HighPrice >= highest)
		{
			_leftPeak = candle.HighPrice;
			_cupLow = null;
			_cupFormed = false;
		}
		else
		{
			if (_cupLow == null || candle.LowPrice < _cupLow)
				_cupLow = candle.LowPrice;

			var width = _leftPeak.Value * WidthPercent / 100m;

			if (!_cupFormed && _cupLow.HasValue && candle.ClosePrice >= _leftPeak - width)
				_cupFormed = true;

			if (_cupFormed && candle.ClosePrice > _leftPeak)
			{
				if (Position <= 0)
					BuyMarket();

				_leftPeak = null;
				_cupLow = null;
				_cupFormed = false;
			}
		}

		// Bearish cup detection (inverted)
		if (_leftBottom == null || candle.LowPrice <= lowest)
		{
			_leftBottom = candle.LowPrice;
			_cupHigh = null;
			_invertedCupFormed = false;
		}
		else
		{
			if (_cupHigh == null || candle.HighPrice > _cupHigh)
				_cupHigh = candle.HighPrice;

			var width = _leftBottom.Value * WidthPercent / 100m;

			if (!_invertedCupFormed && _cupHigh.HasValue && candle.ClosePrice <= _leftBottom + width)
				_invertedCupFormed = true;

			if (_invertedCupFormed && candle.ClosePrice < _leftBottom)
			{
				if (Position >= 0)
					SellMarket();

				_leftBottom = null;
				_cupHigh = null;
				_invertedCupFormed = false;
			}
		}
	}
}
