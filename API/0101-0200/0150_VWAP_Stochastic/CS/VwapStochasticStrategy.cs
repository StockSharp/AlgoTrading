using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining VWAP and manual Stochastic %K.
/// Buys when price is below VWAP and Stochastic is oversold.
/// Sells when price is above VWAP and Stochastic is overbought.
/// </summary>
public class VwapStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _volumes = new();
	private readonly List<decimal> _typicalPriceVol = new();
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
	/// Stochastic lookback period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for stochastic (0-100).
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold level for stochastic (0-100).
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
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
	/// Initialize strategy.
	/// </summary>
	public VwapStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("Stoch Period", "Lookback period for Stochastic %K", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought Level", "Level considered overbought", "Trading Levels");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold Level", "Level considered oversold", "Trading Levels");

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
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_volumes.Clear();
		_typicalPriceVol.Clear();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Use EMA as binding indicator
		var ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;
		var typicalPrice = (high + low + close) / 3m;

		_highs.Add(high);
		_lows.Add(low);
		_closes.Add(close);
		_volumes.Add(volume);
		_typicalPriceVol.Add(typicalPrice * volume);

		var period = StochPeriod;

		if (_closes.Count < period)
		{
			if (_cooldown > 0) _cooldown--;
			return;
		}

		// Manual VWAP (cumulative)
		decimal sumTpv = 0;
		decimal sumVol = 0;
		for (int i = 0; i < _typicalPriceVol.Count; i++)
		{
			sumTpv += _typicalPriceVol[i];
			sumVol += _volumes[i];
		}
		var vwapValue = sumVol > 0 ? sumTpv / sumVol : close;

		// Manual Stochastic %K
		decimal highestHigh = decimal.MinValue;
		decimal lowestLow = decimal.MaxValue;
		var count = _highs.Count;
		for (int i = count - period; i < count; i++)
		{
			if (_highs[i] > highestHigh) highestHigh = _highs[i];
			if (_lows[i] < lowestLow) lowestLow = _lows[i];
		}

		var range = highestHigh - lowestLow;
		var stochK = range > 0 ? 100m * (close - lowestLow) / range : 50m;

		// Keep stochastic lists manageable (but keep all data for VWAP)
		if (_highs.Count > period * 3)
		{
			// For VWAP we need all data, but for stochastic just recent
			// Keep all volumes/tpv for VWAP, trim only H/L/C for stochastic
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price below VWAP + Stochastic oversold
		if (close < vwapValue && stochK < OversoldLevel && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price above VWAP + Stochastic overbought
		else if (close > vwapValue && stochK > OverboughtLevel && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price above VWAP or stoch overbought
		if (Position > 0 && (close > vwapValue || stochK > OverboughtLevel))
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price below VWAP or stoch oversold
		else if (Position < 0 && (close < vwapValue || stochK < OversoldLevel))
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
