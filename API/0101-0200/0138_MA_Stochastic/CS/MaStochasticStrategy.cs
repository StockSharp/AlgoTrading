using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Moving Average and manual Stochastic %K calculation.
/// Enters when price is above MA and Stochastic oversold (longs)
/// or below MA and Stochastic overbought (shorts).
/// </summary>
public class MaStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving Average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic period for %K calculation.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
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
	public MaStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Period", "Period of the Moving Average", "Indicators");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("Stochastic Period", "Period for %K calculation", "Indicators");

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
		_cooldown = 0;
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Track highs, lows, closes for manual stochastic
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_closes.Add(candle.ClosePrice);

		// Keep buffers manageable
		var maxBuf = StochPeriod * 2;
		if (_highs.Count > maxBuf)
		{
			_highs.RemoveRange(0, _highs.Count - maxBuf);
			_lows.RemoveRange(0, _lows.Count - maxBuf);
			_closes.RemoveRange(0, _closes.Count - maxBuf);
		}

		if (_highs.Count < StochPeriod)
			return;

		// Calculate %K manually
		var start = _highs.Count - StochPeriod;
		var highestHigh = decimal.MinValue;
		var lowestLow = decimal.MaxValue;
		for (var i = start; i < _highs.Count; i++)
		{
			if (_highs[i] > highestHigh) highestHigh = _highs[i];
			if (_lows[i] < lowestLow) lowestLow = _lows[i];
		}

		var diff = highestHigh - lowestLow;
		if (diff == 0)
			return;

		var stochK = 100m * (candle.ClosePrice - lowestLow) / diff;
		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long: price above MA + Stochastic oversold
		if (close > maValue && stochK < StochOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: price below MA + Stochastic overbought
		else if (close < maValue && stochK > StochOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price below MA
		if (Position > 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price above MA
		else if (Position < 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
