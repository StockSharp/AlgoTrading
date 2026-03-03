using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses VWAP as a reference point and RSI for oversold/overbought conditions.
/// Enters when price is below VWAP and RSI oversold (longs) or above VWAP and RSI overbought (shorts).
/// </summary>
public class VwapRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _vwapValue;
	private int _cooldown;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public VwapRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_vwapValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var vwap = new VolumeWeightedMovingAverage();
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		// Bind VWAP to capture value (candle-input indicator)
		subscription.BindEx(vwap, OnVwap);

		// Bind RSI for main logic
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}

	private void OnVwap(ICandleMessage candle, IIndicatorValue vwapValue)
	{
		if (vwapValue.IsFormed)
			_vwapValue = vwapValue.ToDecimal();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_vwapValue == 0)
			return;

		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long: price below VWAP + RSI oversold
		if (close < _vwapValue && rsiValue < RsiOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: price above VWAP + RSI overbought
		else if (close > _vwapValue && rsiValue > RsiOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price above VWAP
		if (Position > 0 && close > _vwapValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price below VWAP
		else if (Position < 0 && close < _vwapValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
