using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tweezer Top strategy.
/// Enters short on Tweezer Top (bullish then bearish with matching highs).
/// Enters long on Tweezer Bottom (bearish then bullish with matching lows).
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class TweezerTopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tolerancePercent;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _prevCandle;
	private int _cooldown;

	/// <summary>
	/// Tolerance for matching highs/lows.
	/// </summary>
	public decimal TolerancePercent
	{
		get => _tolerancePercent.Value;
		set => _tolerancePercent.Value = value;
	}

	/// <summary>
	/// MA period for exit.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TweezerTopStrategy()
	{
		_tolerancePercent = Param(nameof(TolerancePercent), 0.1m)
			.SetRange(0.05m, 1m)
			.SetDisplay("Tolerance %", "Max diff between highs/lows", "Pattern");

		_maLength = Param(nameof(MaLength), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Length", "Period of SMA for exit", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevCandle = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevCandle = candle;
			return;
		}

		var highTolerance = _prevCandle.HighPrice * (TolerancePercent / 100m);
		var lowTolerance = _prevCandle.LowPrice * (TolerancePercent / 100m);

		// Tweezer Top: prev bullish, current bearish, matching highs
		var isTweezerTop =
			_prevCandle.ClosePrice > _prevCandle.OpenPrice &&
			candle.ClosePrice < candle.OpenPrice &&
			Math.Abs(_prevCandle.HighPrice - candle.HighPrice) <= highTolerance;

		// Tweezer Bottom: prev bearish, current bullish, matching lows
		var isTweezerBottom =
			_prevCandle.ClosePrice < _prevCandle.OpenPrice &&
			candle.ClosePrice > candle.OpenPrice &&
			Math.Abs(_prevCandle.LowPrice - candle.LowPrice) <= lowTolerance;

		if (Position == 0 && isTweezerTop)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && isTweezerBottom)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		_prevCandle = candle;
	}
}
