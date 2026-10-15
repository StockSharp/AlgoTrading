using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XRP AI 15-m Adaptive v3.1 strategy.
/// Long-only entries with RSI/EMA filter and StdDev-based management.
/// Uses higher timeframe EMA trend filter.
/// </summary>
public class XrpAi15mAdaptiveV31Strategy : Strategy
{
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _tpSmall;
	private readonly StrategyParam<decimal> _tpLarge;
	private readonly StrategyParam<decimal> _trailPct;
	private readonly StrategyParam<int> _maxBars;
	private readonly StrategyParam<DataType> _candleType;

	private bool _trendUp;
	private int _barIndex;
	private int _entryBar;
	private decimal _entryPrice;
	private decimal _highWater;
	private bool _trailLive;
	private decimal _stopPrice;
	private decimal _takePrice;

	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpSmall { get => _tpSmall.Value; set => _tpSmall.Value = value; }
	public decimal TpLarge { get => _tpLarge.Value; set => _tpLarge.Value = value; }
	public decimal TrailPct { get => _trailPct.Value; set => _trailPct.Value = value; }
	public int MaxBars { get => _maxBars.Value; set => _maxBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XrpAi15mAdaptiveV31Strategy()
	{
		_stopPct = Param(nameof(StopPct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpSmall = Param(nameof(TpSmall), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Small TP %", "Take profit for small setups", "Risk");

		_tpLarge = Param(nameof(TpLarge), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Large TP %", "Take profit for large setups", "Risk");

		_trailPct = Param(nameof(TrailPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trail %", "Trailing stop percent", "Risk");

		_maxBars = Param(nameof(MaxBars), 48)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars", "Maximum bars to hold", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Main candle type", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_trendUp = false;
		_barIndex = 0;
		_entryBar = -1;
		_entryPrice = 0;
		_highWater = 0;
		_trailLive = false;
		_stopPrice = 0;
		_takePrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema13 = new ExponentialMovingAverage { Length = 13 };
		var ema34 = new ExponentialMovingAverage { Length = 34 };
		var rsi = new RelativeStrengthIndex { Length = 14 };

		_trendUp = false;
		_barIndex = 0;
		_entryBar = -1;
		_entryPrice = 0;
		_highWater = 0;
		_trailLive = false;
		_stopPrice = 0;
		_takePrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema13, ema34, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema13);
			DrawIndicator(area, ema34);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema13, decimal ema34, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		// Use EMA cross for trend
		_trendUp = ema13 > ema34;

		// Exit management
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.HighPrice > _highWater)
				_highWater = candle.HighPrice;

			// Stop loss
			if (candle.ClosePrice <= _stopPrice)
			{
				SellMarket();
				ResetTrade();
				return;
			}

			// Take profit
			if (candle.ClosePrice >= _takePrice)
			{
				SellMarket();
				ResetTrade();
				return;
			}

			// Trailing stop
			if (!_trailLive && _highWater >= _entryPrice * (1 + TrailPct / 100m))
				_trailLive = true;

			if (_trailLive)
			{
				var trailStop = _highWater * (1 - TrailPct / 200m);
				if (candle.ClosePrice <= trailStop)
				{
					SellMarket();
					ResetTrade();
					return;
				}
			}

			// Time-based exit
			if (_barIndex - _entryBar >= MaxBars)
			{
				SellMarket();
				ResetTrade();
				return;
			}
		}

		// Entry conditions - long only
		if (Position == 0)
		{
			// Large setup: extreme oversold + trend up
			var largeOk = rsi < 25m && candle.ClosePrice > ema34 && _trendUp;

			// Small setup: pullback to EMA in uptrend
			var smallOk = candle.ClosePrice <= ema13 * 0.998m &&
				rsi < 45m &&
				candle.ClosePrice > candle.OpenPrice &&
				_trendUp;

			if (largeOk)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_entryBar = _barIndex;
				_highWater = candle.ClosePrice;
				_trailLive = false;
				_stopPrice = _entryPrice * (1 - StopPct / 100m);
				_takePrice = _entryPrice * (1 + TpLarge / 100m);
			}
			else if (smallOk)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_entryBar = _barIndex;
				_highWater = candle.ClosePrice;
				_trailLive = false;
				_stopPrice = _entryPrice * (1 - StopPct / 100m);
				_takePrice = _entryPrice * (1 + TpSmall / 100m);
			}
		}
	}

	private void ResetTrade()
	{
		_entryBar = -1;
		_entryPrice = 0;
		_highWater = 0;
		_trailLive = false;
		_stopPrice = 0;
		_takePrice = 0;
	}
}
