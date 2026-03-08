using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands candle color strategy.
/// </summary>
public class ColorBbCandlesStrategy : Strategy
{
	private enum BandStates
	{
		Neutral,
		Above,
		Below
	}

	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _breakoutPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private BandStates _previousState = BandStates.Neutral;
	private int _cooldownRemaining;

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal BreakoutPercent
	{
		get => _breakoutPercent.Value;
		set => _breakoutPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ColorBbCandlesStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Length of Bollinger Bands", "General")
			.SetOptimize(50, 200, 25);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Width of Bollinger Bands", "General")
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_breakoutPercent = Param(nameof(BreakoutPercent), 0.0005m)
			.SetDisplay("Breakout %", "Minimum breakout beyond the Bollinger band", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_previousState = BandStates.Neutral;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished || !bbValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upperBand || bb.LowBand is not decimal lowerBand)
			return;

		var state = BandStates.Neutral;
		if (candle.ClosePrice > upperBand * (1m + BreakoutPercent))
			state = BandStates.Above;
		else if (candle.ClosePrice < lowerBand * (1m - BreakoutPercent))
			state = BandStates.Below;

		if (_cooldownRemaining == 0)
		{
			if (state == BandStates.Above && _previousState != BandStates.Above)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (state == BandStates.Below && _previousState != BandStates.Below)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (state == BandStates.Neutral && _previousState != BandStates.Neutral)
			{
				if (Position > 0)
					SellMarket();
				else if (Position < 0)
					BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_previousState = state;
	}
}

