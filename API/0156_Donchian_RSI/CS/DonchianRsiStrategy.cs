using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining manual Donchian Channels with RSI.
/// Buys on upper band breakout when RSI is not overbought.
/// Sells on lower band breakout when RSI is not oversold.
/// </summary>
public class DonchianRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverboughtLevel;
	private readonly StrategyParam<decimal> _rsiOversoldLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
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
	/// Donchian channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
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
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverboughtLevel
	{
		get => _rsiOverboughtLevel.Value;
		set => _rsiOverboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversoldLevel
	{
		get => _rsiOversoldLevel.Value;
		set => _rsiOversoldLevel.Value = value;
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
	public DonchianRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Donchian Period", "Period for Donchian Channels", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("RSI Period", "Period for RSI", "Indicators");

		_rsiOverboughtLevel = Param(nameof(RsiOverboughtLevel), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Trading Levels");

		_rsiOversoldLevel = Param(nameof(RsiOversoldLevel), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Trading Levels");

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
		_cooldown = 0;
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

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		_highs.Add(high);
		_lows.Add(low);

		var period = DonchianPeriod;

		if (_highs.Count < period + 1)
		{
			if (_cooldown > 0) _cooldown--;
			return;
		}

		// Previous Donchian channel (excluding current bar)
		decimal prevUpper = decimal.MinValue;
		decimal prevLower = decimal.MaxValue;
		var count = _highs.Count;
		for (int i = count - period - 1; i < count - 1; i++)
		{
			if (_highs[i] > prevUpper) prevUpper = _highs[i];
			if (_lows[i] < prevLower) prevLower = _lows[i];
		}
		var middleBand = (prevUpper + prevLower) / 2m;

		// Trim lists
		if (_highs.Count > period * 3)
		{
			var trim = _highs.Count - period * 2;
			_highs.RemoveRange(0, trim);
			_lows.RemoveRange(0, trim);
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: upper breakout + RSI not overbought
		if (close > prevUpper && rsiValue < RsiOverboughtLevel && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: lower breakout + RSI not oversold
		else if (close < prevLower && rsiValue > RsiOversoldLevel && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price below middle
		if (Position > 0 && close < middleBand)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price above middle
		else if (Position < 0 && close > middleBand)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
