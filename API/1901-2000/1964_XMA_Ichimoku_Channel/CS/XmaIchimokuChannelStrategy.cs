using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the XMA Ichimoku channel concept.
/// </summary>
public class XmaIchimokuChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _upPeriod;
	private readonly StrategyParam<int> _downPeriod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _upPercent;
	private readonly StrategyParam<decimal> _downPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private readonly SimpleMovingAverage _sma = new();
	private bool _isInitialized;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;

	/// <summary>
	/// Period for calculating the highest value.
	/// </summary>
	public int UpPeriod
	{
		get => _upPeriod.Value;
		set => _upPeriod.Value = value;
	}

	/// <summary>
	/// Period for calculating the lowest value.
	/// </summary>
	public int DownPeriod
	{
		get => _downPeriod.Value;
		set => _downPeriod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Percentage above the average for the upper band.
	/// </summary>
	public decimal UpPercent
	{
		get => _upPercent.Value;
		set => _upPercent.Value = value;
	}

	/// <summary>
	/// Percentage below the average for the lower band.
	/// </summary>
	public decimal DownPercent
	{
		get => _downPercent.Value;
		set => _downPercent.Value = value;
	}

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="XmaIchimokuChannelStrategy"/>.
	/// </summary>
	public XmaIchimokuChannelStrategy()
	{
		_upPeriod = Param(nameof(UpPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Up Period", "Lookback for high prices", "Channel");

		_downPeriod = Param(nameof(DownPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Down Period", "Lookback for low prices", "Channel");

		_maLength = Param(nameof(MaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Smoothing length", "Channel");

		_upPercent = Param(nameof(UpPercent), 1m)
			.SetDisplay("Up Percent", "Upper band offset in %", "Channel");

		_downPercent = Param(nameof(DownPercent), 1m)
			.SetDisplay("Down Percent", "Lower band offset in %", "Channel");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_highs.Clear();
		_lows.Clear();
		_sma.Length = MaLength;
		_sma.Reset();
		_isInitialized = false;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma.Length = MaLength;
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_highs.Enqueue(candle.HighPrice);
		if (_highs.Count > UpPeriod)
			_highs.Dequeue();

		_lows.Enqueue(candle.LowPrice);
		if (_lows.Count > DownPeriod)
			_lows.Dequeue();

		if (_highs.Count < UpPeriod || _lows.Count < DownPeriod)
			return;

		var highestValue = GetMax(_highs);
		var lowestValue = GetMin(_lows);
		var midValue = (highestValue + lowestValue) / 2m;
		var middle = _sma.Process(new DecimalIndicatorValue(_sma, midValue, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_sma.IsFormed)
			return;

		var upper = middle * (1m + UpPercent / 100m);
		var lower = middle * (1m - DownPercent / 100m);

		if (!_isInitialized)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevClose = candle.ClosePrice;
			_isInitialized = true;
			return;
		}

		if (_prevClose > _prevUpper && candle.ClosePrice <= upper && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_prevClose < _prevLower && candle.ClosePrice >= lower && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}

	private static decimal GetMax(IEnumerable<decimal> values)
	{
		var result = decimal.MinValue;

		foreach (var value in values)
		{
			if (value > result)
				result = value;
		}

		return result;
	}

	private static decimal GetMin(IEnumerable<decimal> values)
	{
		var result = decimal.MaxValue;

		foreach (var value in values)
		{
			if (value < result)
				result = value;
		}

		return result;
	}
}
