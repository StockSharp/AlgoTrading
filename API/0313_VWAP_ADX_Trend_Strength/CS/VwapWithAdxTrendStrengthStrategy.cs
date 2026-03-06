using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that trades only when ADX confirms strong directional pressure around VWAP.
/// </summary>
public class VwapWithAdxTrendStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private VolumeWeightedMovingAverage _vwap;
	private int _cooldown;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX required to trade.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public VwapWithAdxTrendStrengthStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 23m)
			.SetRange(1m, 100m)
			.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 72)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_adx = null;
		_vwap = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_vwap = new VolumeWeightedMovingAverage();
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typedAdx = (AverageDirectionalIndexValue)adxValue;

		if (typedAdx.MovingAverage is not decimal adx ||
			typedAdx.Dx.Plus is not decimal diPlus ||
			typedAdx.Dx.Minus is not decimal diMinus)
			return;

		var vwap = _vwap.Process(candle).ToDecimal();

		if (!_adx.IsFormed || !_vwap.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var price = candle.ClosePrice;
		var isStrongTrend = adx >= AdxThreshold;
		var bullishTrend = diPlus > diMinus;
		var bearishTrend = diMinus > diPlus;
		var aboveVwap = price > vwap;
		var belowVwap = price < vwap;

		if (Position == 0)
		{
			if (isStrongTrend && bullishTrend && aboveVwap)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isStrongTrend && bearishTrend && belowVwap)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position > 0 && (!aboveVwap || adx < AdxThreshold * 0.8m || bearishTrend))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (!belowVwap || adx < AdxThreshold * 0.8m || bullishTrend))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
