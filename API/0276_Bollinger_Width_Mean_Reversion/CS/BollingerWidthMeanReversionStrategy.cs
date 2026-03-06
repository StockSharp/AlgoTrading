using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger width mean reversion strategy.
/// Trades contractions and expansions of normalized Bollinger Bands width around its recent average.
/// </summary>
public class BollingerWidthMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _widthLookbackPeriod;
	private readonly StrategyParam<decimal> _widthDeviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bollinger;
	private decimal[] _widthHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Lookback period for width statistics.
	/// </summary>
	public int WidthLookbackPeriod
	{
		get => _widthLookbackPeriod.Value;
		set => _widthLookbackPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for width standard deviation thresholds.
	/// </summary>
	public decimal WidthDeviationMultiplier
	{
		get => _widthDeviationMultiplier.Value;
		set => _widthDeviationMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Cooldown bars between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BollingerWidthMeanReversionStrategy"/>.
	/// </summary>
	public BollingerWidthMeanReversionStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period for Bollinger Bands calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
			.SetOptimize(1m, 3m, 0.5m);

		_widthLookbackPeriod = Param(nameof(WidthLookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Width Lookback", "Lookback for width mean", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Width Dev Mult", "Multiplier for width standard deviation threshold", "Strategy Parameters")
			.SetOptimize(0.5m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_bollinger = null;
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_widthHistory = new decimal[WidthLookbackPeriod];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation,
		};

		_widthHistory = new decimal[WidthLookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		if (middleBand <= 0)
			return;

		var lastWidth = (upperBand - lowerBand) / middleBand;

		_widthHistory[_currentIndex] = lastWidth;
		_currentIndex = (_currentIndex + 1) % WidthLookbackPeriod;

		if (_filledCount < WidthLookbackPeriod)
			_filledCount++;

		if (_filledCount < WidthLookbackPeriod)
			return;

		var avgWidth = 0m;
		var sumSq = 0m;

		for (var i = 0; i < WidthLookbackPeriod; i++)
			avgWidth += _widthHistory[i];

		avgWidth /= WidthLookbackPeriod;

		if (avgWidth <= 0)
			return;

		for (var i = 0; i < WidthLookbackPeriod; i++)
		{
			var diff = _widthHistory[i] - avgWidth;
			sumSq += diff * diff;
		}

		var stdWidth = (decimal)Math.Sqrt((double)(sumSq / WidthLookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = avgWidth - WidthDeviationMultiplier * stdWidth;
		var upperThreshold = avgWidth + WidthDeviationMultiplier * stdWidth;

		if (Position == 0)
		{
			if (lastWidth < lowerThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (lastWidth > upperThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && lastWidth >= avgWidth)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && lastWidth <= avgWidth)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
