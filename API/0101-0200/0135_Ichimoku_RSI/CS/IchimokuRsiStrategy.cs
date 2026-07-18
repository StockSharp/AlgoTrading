using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Ichimoku Tenkan/Kijun crossover and RSI indicators.
/// Enters on Tenkan/Kijun crossover with RSI confirmation.
/// Uses manual Tenkan(9)/Kijun(26) calculation to avoid Ichimoku composite indicator issues.
/// </summary>
public class IchimokuRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _rsiValue;
	private int _cooldown;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private const int TenkanPeriod = 9;
	private const int KijunPeriod = 26;

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
	/// Initializes a new instance of the <see cref="IchimokuRsiStrategy"/>.
	/// </summary>
	public IchimokuRsiStrategy()
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
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}

	private static decimal GetHighest(List<decimal> values, int period)
	{
		var start = Math.Max(0, values.Count - period);
		var max = decimal.MinValue;
		for (var i = start; i < values.Count; i++)
			if (values[i] > max) max = values[i];
		return max;
	}

	private static decimal GetLowest(List<decimal> values, int period)
	{
		var start = Math.Max(0, values.Count - period);
		var min = decimal.MaxValue;
		for (var i = start; i < values.Count; i++)
			if (values[i] < min) min = values[i];
		return min;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal)
	{
		_rsiValue = rsiVal;

		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Track highs and lows
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		// Keep buffer manageable
		if (_highs.Count > KijunPeriod * 2)
		{
			_highs.RemoveRange(0, _highs.Count - KijunPeriod * 2);
			_lows.RemoveRange(0, _lows.Count - KijunPeriod * 2);
		}

		// Need at least KijunPeriod bars for full calculation
		if (_highs.Count < KijunPeriod)
			return;

		// Tenkan-sen = (highest high over 9 periods + lowest low over 9 periods) / 2
		var tenkan = (GetHighest(_highs, TenkanPeriod) + GetLowest(_lows, TenkanPeriod)) / 2;
		// Kijun-sen = (highest high over 26 periods + lowest low over 26 periods) / 2
		var kijun = (GetHighest(_highs, KijunPeriod) + GetLowest(_lows, KijunPeriod)) / 2;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: tenkan > kijun (bullish) + RSI not overbought
		if (tenkan > kijun && _rsiValue < RsiOverbought && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: tenkan < kijun (bearish) + RSI not oversold
		else if (tenkan < kijun && _rsiValue > RsiOversold && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long if tenkan crosses below kijun
		if (Position > 0 && tenkan < kijun)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short if tenkan crosses above kijun
		else if (Position < 0 && tenkan > kijun)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
