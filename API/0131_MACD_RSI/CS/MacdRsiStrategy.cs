using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining MACD and RSI indicators.
/// Uses MACD for trend direction and RSI for entry timing at extreme levels.
/// </summary>
public class MacdRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _rsiValue;
	private decimal _prevMacdLine;
	private decimal _prevSignalLine;
	private int _cooldown;

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for RSI calculation.
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
	/// Initializes a new instance of the <see cref="MacdRsiStrategy"/>.
	/// </summary>
	public MacdRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI Settings");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI Settings");

		_cooldownBars = Param(nameof(CooldownBars), 150)
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
		_prevMacdLine = 0;
		_prevSignalLine = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal();
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		// Bind RSI separately to capture its value
		subscription.Bind(rsi, OnRsi);

		// Bind MACD with BindEx to get signal+macd
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, macd);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}

	private void OnRsi(ICandleMessage candle, decimal rsi)
	{
		_rsiValue = rsi;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMacdLine = macdLine;
			_prevSignalLine = signalLine;
			return;
		}

		var isUptrend = macdLine > signalLine;

		// Entry: uptrend + oversold RSI = buy
		if (isUptrend && _rsiValue < RsiOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Entry: downtrend + overbought RSI = sell
		else if (!isUptrend && _rsiValue > RsiOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit: trend reversal
		if (Position > 0 && !isUptrend)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && isUptrend)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevMacdLine = macdLine;
		_prevSignalLine = signalLine;
	}
}
