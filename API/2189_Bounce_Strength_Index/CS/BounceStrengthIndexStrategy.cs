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
/// Bounce Strength Index strategy.
/// Uses close price position within recent range to generate momentum signals.
/// </summary>
public class BounceStrengthIndexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _smaPeriod;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal? _prevBsi;
	private bool? _prevRising;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RangePeriod { get => _rangePeriod.Value; set => _rangePeriod.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	public BounceStrengthIndexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_rangePeriod = Param(nameof(RangePeriod), 10)
			.SetDisplay("Range Period", "Period for highest and lowest search", "Indicator");
		_smaPeriod = Param(nameof(SmaPeriod), 10)
			.SetDisplay("SMA Period", "SMA period for trend filter", "Indicator");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_prevBsi = null;
		_prevRising = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new ExponentialMovingAverage { Length = SmaPeriod };

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
		if (_highs.Count > RangePeriod) _highs.RemoveAt(0);
		if (_lows.Count > RangePeriod) _lows.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highs.Count < 3)
			return;

		var high = _highs.Max();
		var low = _lows.Min();
		var range = high - low;

		if (range <= 0)
			return;

		var bsi = (candle.ClosePrice - low) / range * 100m;

		if (_prevBsi is decimal prev)
		{
			var rising = bsi > prev;

			if (rising && _prevRising != true && Position <= 0)
				BuyMarket();
			else if (!rising && _prevRising != false && Position >= 0)
				SellMarket();

			_prevRising = rising;
		}

		_prevBsi = bsi;
	}
}
