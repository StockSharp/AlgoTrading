using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that buys breakouts when ADX is below a threshold.
/// Enters long if price closes above the previous highest close.
/// </summary>
public class AdxRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _highestPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevHighest;
	private int _cooldownRemaining;

	public int HighestPeriod { get => _highestPeriod.Value; set => _highestPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdxRangeBreakoutStrategy()
	{
		_highestPeriod = Param(nameof(HighestPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Highest Lookback", "Bars for highest close", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "Upper ADX limit for range", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHighest = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var highest = new Highest { Length = HighestPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, highest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var curHighest = highestValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHighest = curHighest;
			return;
		}

		if (_prevHighest == 0)
		{
			_prevHighest = curHighest;
			return;
		}

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adxMa)
		{
			_prevHighest = curHighest;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevHighest = curHighest;
			return;
		}

		// Buy breakout when ADX is low (range-bound market breaking out)
		if (Position == 0 && adxMa < AdxThreshold && candle.ClosePrice > _prevHighest)
		{
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long when ADX rises (trend established, take profit)
		else if (Position > 0 && (adxMa >= AdxThreshold * 1.5m || candle.ClosePrice < _prevHighest * 0.98m))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevHighest = curHighest;
	}
}
