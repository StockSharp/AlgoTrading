using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Keltner Channels (EMA + ATR) and manual Stochastic %K.
/// Enters when price reaches Keltner bands and Stochastic confirms oversold/overbought.
/// </summary>
public class KeltnerStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _atrValue;
	private int _cooldown;
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private const int StochPeriod = 14;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period for Keltner Channel.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Keltner Channel multiplier.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
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
	public KeltnerStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetRange(10, 30)
			.SetDisplay("EMA Period", "Period of the EMA for Keltner Channel", "Indicators");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2.0m)
			.SetDisplay("Keltner Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators");

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
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		// Bind ATR to capture value
		subscription.BindEx(atr, OnAtr);

		// Bind EMA for main logic
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void OnAtr(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (atrValue.IsFormed)
			_atrValue = atrValue.ToDecimal();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_atrValue <= 0)
			return;

		// Track highs/lows for stochastic
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxBuf = StochPeriod * 2;
		if (_highs.Count > maxBuf)
		{
			_highs.RemoveRange(0, _highs.Count - maxBuf);
			_lows.RemoveRange(0, _lows.Count - maxBuf);
		}

		if (_highs.Count < StochPeriod)
			return;

		// Manual Stochastic %K
		var start = _highs.Count - StochPeriod;
		var highestHigh = decimal.MinValue;
		var lowestLow = decimal.MaxValue;
		for (var i = start; i < _highs.Count; i++)
		{
			if (_highs[i] > highestHigh) highestHigh = _highs[i];
			if (_lows[i] < lowestLow) lowestLow = _lows[i];
		}
		var diff = highestHigh - lowestLow;
		if (diff == 0) return;
		var stochK = 100m * (candle.ClosePrice - lowestLow) / diff;

		// Keltner Channel
		var upperBand = emaValue + (KeltnerMultiplier * _atrValue);
		var lowerBand = emaValue - (KeltnerMultiplier * _atrValue);
		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long: price below lower Keltner + Stochastic oversold
		if (close < lowerBand && stochK < StochOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: price above upper Keltner + Stochastic overbought
		else if (close > upperBand && stochK > StochOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price above EMA
		if (Position > 0 && close > emaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price below EMA
		else if (Position < 0 && close < emaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
