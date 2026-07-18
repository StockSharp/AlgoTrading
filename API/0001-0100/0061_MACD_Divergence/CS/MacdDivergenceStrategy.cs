using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Divergence strategy.
/// Detects divergences between price and MACD for reversal signals.
/// Bullish: price falling but MACD rising.
/// Bearish: price rising but MACD falling.
/// </summary>
public class MacdDivergenceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevPrice;
	private decimal _prevMacd;
	private int _cooldown;

	/// <summary>
	/// Candle type.
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
	/// Constructor.
	/// </summary>
	public MacdDivergenceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, _candleType.Value)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPrice = default;
		_prevMacd = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPrice = 0;
		_prevMacd = 0;
		_cooldown = 0;

		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(_candleType.Value);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signal)
			return;

		if (_prevPrice == 0)
		{
			_prevPrice = candle.ClosePrice;
			_prevMacd = macdLine;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrice = candle.ClosePrice;
			_prevMacd = macdLine;
			return;
		}

		// Bullish divergence: price down but MACD up
		var bullishDiv = candle.ClosePrice < _prevPrice && macdLine > _prevMacd;
		// Bearish divergence: price up but MACD down
		var bearishDiv = candle.ClosePrice > _prevPrice && macdLine < _prevMacd;

		if (Position == 0 && bullishDiv && macdLine > signal)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishDiv && macdLine < signal)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && macdLine < signal)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && macdLine > signal)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevPrice = candle.ClosePrice;
		_prevMacd = macdLine;
	}
}
