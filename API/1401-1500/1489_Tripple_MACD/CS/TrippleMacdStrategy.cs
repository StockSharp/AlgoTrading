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
/// Combines multiple EMA difference lines (simulating MACD histogram) with RSI filter.
/// Buys when averaged MACD histogram turns positive with RSI below threshold.
/// </summary>
public class TrippleMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _fast1;
	private readonly StrategyParam<int> _slow1;
	private readonly StrategyParam<int> _fast2;
	private readonly StrategyParam<int> _slow2;

	private decimal _prevHist;
	private decimal _prevRsi;
	private int _cooldown;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public int Fast1 { get => _fast1.Value; set => _fast1.Value = value; }
	public int Slow1 { get => _slow1.Value; set => _slow1.Value = value; }
	public int Fast2 { get => _fast2.Value; set => _fast2.Value = value; }
	public int Slow2 { get => _slow2.Value; set => _slow2.Value = value; }

	public TrippleMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
		_fast1 = Param(nameof(Fast1), 8).SetDisplay("Fast1", "Fast period 1", "Indicators");
		_slow1 = Param(nameof(Slow1), 21).SetDisplay("Slow1", "Slow period 1", "Indicators");
		_fast2 = Param(nameof(Fast2), 13).SetDisplay("Fast2", "Fast period 2", "Indicators");
		_slow2 = Param(nameof(Slow2), 34).SetDisplay("Slow2", "Slow period 2", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHist = 0;
		_prevRsi = 0;
		_cooldown = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast1 = new ExponentialMovingAverage { Length = Fast1 };
		var emaSlow1 = new ExponentialMovingAverage { Length = Slow1 };
		var emaFast2 = new ExponentialMovingAverage { Length = Fast2 };
		var emaSlow2 = new ExponentialMovingAverage { Length = Slow2 };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(emaFast1, emaSlow1, emaFast2, emaSlow2, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast1);
			DrawIndicator(area, emaSlow1);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal f1, decimal s1, decimal f2, decimal s2, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Simulated MACD histograms
		var macd1 = f1 - s1;
		var macd2 = f2 - s2;
		var hist = (macd1 + macd2) / 2m;

		if (_prevHist == 0 || _prevRsi == 0)
		{
			_prevHist = hist;
			_prevRsi = rsiVal;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevHist = hist;
			_prevRsi = rsiVal;
			return;
		}

		// histogram trend direction
		var histUp = hist > 0m;
		var histDown = hist < 0m;

		// RSI cross 50
		var rsiUp = _prevRsi <= 50m && rsiVal > 50m;
		var rsiDown = _prevRsi >= 50m && rsiVal < 50m;

		// Exit on opposite RSI cross
		if (Position > 0 && rsiDown)
		{
			SellMarket();
			_cooldown = 80;
		}
		else if (Position < 0 && rsiUp)
		{
			BuyMarket();
			_cooldown = 80;
		}

		// Entry: RSI cross 50 + histogram confirms
		if (Position == 0)
		{
			if (rsiUp && histUp)
			{
				BuyMarket();
				_cooldown = 80;
			}
			else if (rsiDown && histDown)
			{
				SellMarket();
				_cooldown = 80;
			}
		}

		_prevRsi = rsiVal;

		_prevHist = hist;
	}
}
