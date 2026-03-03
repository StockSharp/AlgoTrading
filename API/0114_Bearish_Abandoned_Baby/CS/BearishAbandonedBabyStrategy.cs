using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bearish Abandoned Baby candlestick pattern.
/// Detects a bullish candle followed by a small-body candle near highs,
/// then a bearish confirmation candle. Uses SMA for trend filter.
/// Also detects the bullish mirror pattern for long entries.
/// </summary>
public class BearishAbandonedBabyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private decimal _prev2Open;
	private decimal _prev2Close;
	private decimal _prev2High;
	private decimal _prev2Low;
	private decimal _prev1Open;
	private decimal _prev1Close;
	private decimal _prev1High;
	private decimal _prev1Low;
	private decimal _prevMa;
	private int _candleCount;
	private int _cooldown;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public BearishAbandonedBabyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "SMA period for exit", "Indicators")
			.SetRange(10, 50);

		_cooldownBars = Param(nameof(CooldownBars), 400)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 3000);
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
		_ma = default;
		_prev2Open = 0;
		_prev2Close = 0;
		_prev2High = 0;
		_prev2Low = 0;
		_prev1Open = 0;
		_prev1Close = 0;
		_prev1High = 0;
		_prev1Low = 0;
		_prevMa = 0;
		_candleCount = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_candleCount++;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prev2Open = _prev1Open;
			_prev2Close = _prev1Close;
			_prev2High = _prev1High;
			_prev2Low = _prev1Low;
			_prev1Open = open;
			_prev1Close = close;
			_prev1High = high;
			_prev1Low = low;
			_prevMa = ma;
			return;
		}

		// Exit logic: MA cross
		if (Position > 0 && close < ma && _prevMa > 0 && _prev1Close >= _prevMa)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && close > ma && _prevMa > 0 && _prev1Close <= _prevMa)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		// Entry logic
		if (Position == 0 && _candleCount >= 3 && _prev2Close != 0)
		{
			var prev2Body = Math.Abs(_prev2Close - _prev2Open);
			var prev2Range = _prev2High - _prev2Low;

			// Small body (doji-like) for middle candle
			var isSmallBody = Math.Abs(_prev1Close - _prev1Open) < prev2Body * 0.4m && prev2Range > 0;

			// Bearish abandoned baby (relaxed):
			// 1. First candle is bullish
			// 2. Middle candle has small body near first candle high
			// 3. Current candle is bearish
			var firstBullish = _prev2Close > _prev2Open;
			var middleNearHigh = _prev1Close >= _prev2High - prev2Range * 0.3m;
			var currentBearish = close < open;

			// Bullish abandoned baby (relaxed, mirror):
			var firstBearish = _prev2Close < _prev2Open;
			var middleNearLow = _prev1Close <= _prev2Low + prev2Range * 0.3m;
			var currentBullish = close > open;

			if (isSmallBody && firstBullish && middleNearHigh && currentBearish && close < ma)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (isSmallBody && firstBearish && middleNearLow && currentBullish && close > ma)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		// Shift candle history
		_prev2Open = _prev1Open;
		_prev2Close = _prev1Close;
		_prev2High = _prev1High;
		_prev2Low = _prev1Low;
		_prev1Open = open;
		_prev1Close = close;
		_prev1High = high;
		_prev1Low = low;
		_prevMa = ma;
	}
}
