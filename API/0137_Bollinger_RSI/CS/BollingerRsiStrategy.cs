using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined strategy that uses Bollinger Bands and RSI indicators
/// for mean reversion trading.
/// </summary>
public class BollingerRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _rsiValue;
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
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	public BollingerRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Bollinger Period", "Period of the Bollinger Bands indicator", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators");

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
		_rsiValue = 50;
		_cooldown = 0;
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

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		// Bind RSI to capture value
		subscription.Bind(rsi, OnRsi);

		// Bind Bollinger for main logic
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}

	private void OnRsi(ICandleMessage candle, decimal rsi)
	{
		_rsiValue = rsi;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bollingerTyped = (BollingerBandsValue)bollingerValue;

		if (bollingerTyped.UpBand is not decimal upperBand ||
			bollingerTyped.LowBand is not decimal lowerBand ||
			bollingerTyped.MovingAverage is not decimal middleBand)
			return;

		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long entry: price below lower band + RSI oversold
		if (close < lowerBand && _rsiValue < RsiOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short entry: price above upper band + RSI overbought
		else if (close > upperBand && _rsiValue > RsiOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price returns to middle band
		if (Position > 0 && close > middleBand)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price returns below middle band
		else if (Position < 0 && close < middleBand)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
