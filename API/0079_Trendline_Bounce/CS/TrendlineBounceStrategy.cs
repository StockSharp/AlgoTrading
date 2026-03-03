using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trendline Bounce strategy.
/// Calculates linear regression of recent lows (support) and highs (resistance).
/// Buys on bounce off support trendline, sells on bounce off resistance.
/// Uses SMA for exit signals.
/// </summary>
public class TrendlineBounceStrategy : Strategy
{
	private readonly StrategyParam<int> _trendlinePeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private int _cooldown;

	/// <summary>
	/// Trendline period.
	/// </summary>
	public int TrendlinePeriod
	{
		get => _trendlinePeriod.Value;
		set => _trendlinePeriod.Value = value;
	}

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
	public TrendlineBounceStrategy()
	{
		_trendlinePeriod = Param(nameof(TrendlinePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trendline Period", "Lookback for trendline", "Indicators");

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
		_highs.Clear();
		_lows.Clear();
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
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

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > TrendlinePeriod)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highs.Count < TrendlinePeriod)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Calculate linear regression for support (lows) and resistance (highs)
		var supportLevel = GetLinRegValue(_lows);
		var resistanceLevel = GetLinRegValue(_highs);
		var buffer = (resistanceLevel - supportLevel) * 0.05m;

		if (buffer <= 0)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		// Bounce off support (buy)
		if (Position == 0 && candle.LowPrice <= supportLevel + buffer && isBullish)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Bounce off resistance (sell)
		else if (Position == 0 && candle.HighPrice >= resistanceLevel - buffer && isBearish)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit using SMA
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
	}

	private static decimal GetLinRegValue(List<decimal> values)
	{
		var n = values.Count;
		if (n == 0) return 0;

		decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
		for (int i = 0; i < n; i++)
		{
			sumX += i;
			sumY += values[i];
			sumXY += i * values[i];
			sumX2 += i * i;
		}

		var denom = n * sumX2 - sumX * sumX;
		if (denom == 0) return sumY / n;

		var slope = (n * sumXY - sumX * sumY) / denom;
		var intercept = (sumY - slope * sumX) / n;

		return slope * (n - 1) + intercept;
	}
}
