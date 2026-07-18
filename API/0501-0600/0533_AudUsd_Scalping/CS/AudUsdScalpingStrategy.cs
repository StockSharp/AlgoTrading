using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using EMA crossover with RSI filter.
/// Buys when fast EMA crosses above slow EMA and RSI exits oversold.
/// Sells when fast EMA crosses below slow EMA and RSI exits overbought.
/// </summary>
public class AudUsdScalpingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaShort;
	private readonly StrategyParam<int> _emaLong;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaShort
	{
		get => _emaShort.Value;
		set => _emaShort.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaLong
	{
		get => _emaLong.Value;
		set => _emaLong.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
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
	public AudUsdScalpingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_emaShort = Param(nameof(EmaShort), 13)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Fast EMA period", "Indicators");

		_emaLong = Param(nameof(EmaLong), 26)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Slow EMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");
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
		_prevFastEma = 0;
		_prevSlowEma = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaShort };
		var emaSlow = new ExponentialMovingAverage { Length = EmaLong };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// EMA crossover with RSI filter
		var crossUp = _prevFastEma > 0 && _prevFastEma <= _prevSlowEma && fastValue > slowValue;
		var crossDown = _prevFastEma > 0 && _prevFastEma >= _prevSlowEma && fastValue < slowValue;

		if (crossUp && rsiValue < 60 && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (crossDown && rsiValue > 40 && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevFastEma = fastValue;
		_prevSlowEma = slowValue;
	}
}
