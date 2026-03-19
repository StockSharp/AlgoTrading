using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining manual Supertrend with RSI.
/// Buys when price above Supertrend and RSI oversold.
/// Sells when price below Supertrend and RSI overbought.
/// </summary>
public class SupertrendRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private decimal _prevSupertrend;
	private bool _prevUpTrend;
	private bool _stInitialized;
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
	/// ATR period for Supertrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initialize strategy.
	/// </summary>
	public SupertrendRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetRange(5, 30)
			.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend");

		_multiplier = Param(nameof(Multiplier), 3.0m)
			.SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Supertrend");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("RSI Period", "Period for RSI", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

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
		_prevSupertrend = 0;
		_prevUpTrend = true;
		_stInitialized = false;
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

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

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


		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		_highs.Add(high);
		_lows.Add(low);
		_closes.Add(close);

		var period = AtrPeriod;

		if (_closes.Count < period + 1)
		{
			if (_cooldown > 0) _cooldown--;
			return;
		}

		// Manual ATR calculation
		decimal sumTr = 0;
		var count = _highs.Count;
		for (int i = count - period; i < count; i++)
		{
			var h = _highs[i];
			var l = _lows[i];
			var prevC = _closes[i - 1];
			var tr = Math.Max(h - l, Math.Max(Math.Abs(h - prevC), Math.Abs(l - prevC)));
			sumTr += tr;
		}
		var atr = sumTr / period;

		// Manual Supertrend
		var midPrice = (high + low) / 2m;
		var upperBand = midPrice + Multiplier * atr;
		var lowerBand = midPrice - Multiplier * atr;

		bool upTrend;
		decimal supertrend;

		if (!_stInitialized)
		{
			upTrend = close > midPrice;
			supertrend = upTrend ? lowerBand : upperBand;
			_stInitialized = true;
		}
		else
		{
			if (_prevUpTrend)
			{
				// In uptrend: lower band can only increase
				if (lowerBand < _prevSupertrend)
					lowerBand = _prevSupertrend;

				upTrend = close >= lowerBand;
				supertrend = upTrend ? lowerBand : upperBand;
			}
			else
			{
				// In downtrend: upper band can only decrease
				if (upperBand > _prevSupertrend)
					upperBand = _prevSupertrend;

				upTrend = close > upperBand;
				supertrend = upTrend ? lowerBand : upperBand;
			}
		}

		_prevSupertrend = supertrend;
		_prevUpTrend = upTrend;

		// Trim lists
		if (_highs.Count > period * 3)
		{
			var trim = _highs.Count - period * 2;
			_highs.RemoveRange(0, trim);
			_lows.RemoveRange(0, trim);
			_closes.RemoveRange(0, trim);
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (Position != 0)
			return;

		// Buy: uptrend + RSI below midpoint (momentum not exhausted)
		if (upTrend && rsiValue < 50m)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: downtrend + RSI above midpoint
		else if (!upTrend && rsiValue > 50m)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
	}
}
