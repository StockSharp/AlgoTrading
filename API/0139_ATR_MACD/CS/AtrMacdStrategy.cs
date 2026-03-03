using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses ATR for volatility detection and MACD for trend direction.
/// Enters when MACD confirms trend direction.
/// </summary>
public class AtrMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _atrValue;
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
	public AtrMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

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
		_atrValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = 14 };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);

		// Bind ATR to capture value
		subscription.BindEx(atr, OnAtr);

		// Bind MACD for main logic
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var atrArea = CreateChartArea();
			if (atrArea != null)
				DrawIndicator(atrArea, atr);

			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, macd);
		}
	}

	private void OnAtr(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (atrValue.IsFormed)
			_atrValue = atrValue.ToDecimal();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Entry: MACD bullish crossover
		if (macdLine > signalLine && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Entry: MACD bearish crossover
		else if (macdLine < signalLine && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit on MACD crossover against position
		if (Position > 0 && macdLine < signalLine)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && macdLine > signalLine)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
