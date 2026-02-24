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
/// Strategy that accumulates positions when RSI drops below its mean minus standard deviation,
/// and exits when RSI rises above its mean plus standard deviation.
/// </summary>
public class TtpIntelligentAccumulatorStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _rsiHistory = new();
	private decimal _entryPrice;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TtpIntelligentAccumulatorStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

		_lookback = Param(nameof(Lookback), 14)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Period for RSI mean and std dev", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiHistory.Clear();
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsiHistory.Add(rsiVal);
		if (_rsiHistory.Count > Lookback)
			_rsiHistory.RemoveAt(0);

		if (_rsiHistory.Count < Lookback)
			return;

		// Calculate mean and std of RSI
		var sum = 0m;
		for (var i = 0; i < _rsiHistory.Count; i++)
			sum += _rsiHistory[i];
		var mean = sum / _rsiHistory.Count;

		var sumSq = 0m;
		for (var i = 0; i < _rsiHistory.Count; i++)
		{
			var diff = _rsiHistory[i] - mean;
			sumSq += diff * diff;
		}
		var std = (decimal)Math.Sqrt((double)(sumSq / _rsiHistory.Count));

		if (std <= 0)
			return;

		var entrySignal = rsiVal < mean - std;
		var exitSignal = rsiVal > mean + std;

		// Accumulate long when RSI is oversold relative to its own distribution
		if (entrySignal && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		// Exit when RSI is overbought
		else if (exitSignal && Position > 0)
		{
			SellMarket();
			_entryPrice = 0;
		}
		// Also allow short on extreme overbought
		else if (exitSignal && Position == 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (entrySignal && Position < 0)
		{
			BuyMarket();
			_entryPrice = 0;
		}
	}
}
