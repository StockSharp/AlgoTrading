using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that buys breakouts when ADX is below a threshold.
/// Enters long if price closes above the previous highest close.
/// Limits trades per day and exits at session end.
/// </summary>
public class AdxRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _highestPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private Highest _highest;
	private decimal _prevHighest;
	private int _tradesToday;
	private DateTime _currentDay;

	private static readonly TimeSpan SessionStart = new(7, 30, 0);
	private static readonly TimeSpan SessionEnd = new(14, 30, 0);

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Lookback period for highest close.
	/// </summary>
	public int HighestPeriod
	{
		get => _highestPeriod.Value;
		set => _highestPeriod.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Maximum ADX value to allow entry.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Maximum trades allowed per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="AdxRangeBreakoutStrategy"/>.
	/// </summary>
	public AdxRangeBreakoutStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop-loss in price units", "Exits")
			.SetGreaterThanZero();

		_highestPeriod = Param(nameof(HighestPeriod), 34)
			.SetDisplay("Highest Lookback", "Bars for highest close", "Indicators")
			.SetGreaterThanZero();

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX", "Indicators")
			.SetGreaterThanZero();

		_adxThreshold = Param(nameof(AdxThreshold), 17.5m)
			.SetDisplay("ADX Threshold", "Upper ADX limit", "Indicators")
			.SetGreaterThanZero();

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 3)
			.SetDisplay("Max Trades Per Day", "Trade limit per session", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_prevHighest = default;
		_tradesToday = default;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_highest = new Highest { Length = HighestPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _highest, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawIndicator(area, _highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFinal || !highestValue.IsFinal)
			return;

		if (!_highest.IsFormed)
		{
			_prevHighest = highestValue.GetValue<decimal>();
			return;
		}

		if (candle.OpenTime.Date != _currentDay)
		{
			_currentDay = candle.OpenTime.Date;
			_tradesToday = 0;
		}

		if (!IsWithinSession(candle.OpenTime))
		{
			if (Position != 0)
			{
				ClosePosition();
				LogInfo("End of session exit.");
			}

			_prevHighest = highestValue.GetValue<decimal>();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHighest = highestValue.GetValue<decimal>();
			return;
		}

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var adx = adxTyped.MovingAverage;
		if (adx is not decimal adxMa)
		{
			_prevHighest = highestValue.GetValue<decimal>();
			return;
		}

		var prevHigh = _prevHighest;
		var highest = highestValue.GetValue<decimal>();

		if (Position == 0 && _tradesToday < MaxTradesPerDay && adxMa < AdxThreshold && candle.ClosePrice >= prevHigh)
		{
			BuyMarket(Volume);
			_tradesToday++;
			LogInfo($"Long entry at {candle.ClosePrice} ADX={adxMa}");
		}

		_prevHighest = highest;
	}

	private static bool IsWithinSession(DateTimeOffset time)
	{
		var t = time.TimeOfDay;
		return t >= SessionStart && t <= SessionEnd;
	}
}
