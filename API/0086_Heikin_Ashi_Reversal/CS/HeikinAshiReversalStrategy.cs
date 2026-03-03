using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi Reversal strategy.
/// Computes Heikin-Ashi candles from regular candles.
/// Enters long when HA switches from bearish to bullish.
/// Enters short when HA switches from bullish to bearish.
/// Uses SMA for exit confirmation.
/// </summary>
public class HeikinAshiReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _haOpen;
	private decimal _haClose;
	private bool? _prevBullish;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public HeikinAshiReversalStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

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
		_haOpen = default;
		_haClose = default;
		_prevBullish = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_haOpen = 0;
		_haClose = 0;
		_prevBullish = null;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

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

		// Compute Heikin-Ashi values
		var newHaClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;

		decimal newHaOpen;
		if (_haOpen == 0)
		{
			// First candle
			newHaOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
		}
		else
		{
			newHaOpen = (_haOpen + _haClose) / 2;
		}

		_haOpen = newHaOpen;
		_haClose = newHaClose;

		var isBullish = newHaClose > newHaOpen;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevBullish = isBullish;
			return;
		}

		if (_prevBullish == null)
		{
			_prevBullish = isBullish;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevBullish = isBullish;
			return;
		}

		// Reversal detection
		var bullishReversal = _prevBullish == false && isBullish;
		var bearishReversal = _prevBullish == true && !isBullish;

		if (Position == 0 && bullishReversal)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishReversal)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevBullish = isBullish;
	}
}
