using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining RSI and manual Stochastic %K for double confirmation
/// of oversold/overbought conditions.
/// </summary>
public class RsiStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _cooldownBars;

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
	public RsiStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "Stochastic oversold level", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "Stochastic overbought level", "Indicators");

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

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Track highs/lows for manual stochastic
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

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long: both RSI and Stochastic oversold
		if (rsiValue < RsiOversold && stochK < StochOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: both RSI and Stochastic overbought
		else if (rsiValue > RsiOverbought && stochK > StochOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: RSI leaves oversold zone
		if (Position > 0 && rsiValue > 50)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: RSI leaves overbought zone
		else if (Position < 0 && rsiValue < 50)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
