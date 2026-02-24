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
/// Triple EMA with QQE-inspired RSI filter and trailing stop.
/// Uses fast/slow EMA crossover confirmed by RSI momentum direction.
/// </summary>
public class TripleEmaQqeTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<decimal> _trailPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevRsi;
	private decimal _stopLoss;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public decimal TrailPct { get => _trailPct.Value; set => _trailPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TripleEmaQqeTrendFollowingStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_trailPct = Param(nameof(TrailPct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail %", "Trailing stop percent", "Risk");

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
		_prevFast = 0;
		_prevSlow = 0;
		_prevRsi = 0;
		_stopLoss = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0 || _prevRsi == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_prevRsi = rsiVal;
			return;
		}

		var price = candle.ClosePrice;

		// QQE-inspired: RSI crossing above 50 = momentum long, below 50 = momentum short
		var rsiLong = _prevRsi <= 50m && rsiVal > 50m;
		var rsiShort = _prevRsi >= 50m && rsiVal < 50m;

		// TEMA-inspired: fast EMA trending above slow
		var trendUp = price > fastVal && fastVal > slowVal && slowVal > _prevSlow;
		var trendDown = price < fastVal && fastVal < slowVal && slowVal < _prevSlow;

		// Trailing stop management
		if (Position > 0)
		{
			var newStop = price * (1m - TrailPct / 100m);
			_stopLoss = Math.Max(_stopLoss, newStop);
			if (price < _stopLoss)
			{
				SellMarket();
				_stopLoss = 0;
			}
		}
		else if (Position < 0)
		{
			var newStop = price * (1m + TrailPct / 100m);
			if (_stopLoss == 0) _stopLoss = newStop;
			_stopLoss = Math.Min(_stopLoss, newStop);
			if (price > _stopLoss)
			{
				BuyMarket();
				_stopLoss = 0;
			}
		}

		// Entry signals
		if (trendUp && (rsiLong || rsiVal > 55m) && Position <= 0)
		{
			BuyMarket();
			_stopLoss = price * (1m - TrailPct / 100m);
		}
		else if (trendDown && (rsiShort || rsiVal < 45m) && Position >= 0)
		{
			SellMarket();
			_stopLoss = price * (1m + TrailPct / 100m);
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
		_prevRsi = rsiVal;
	}
}
