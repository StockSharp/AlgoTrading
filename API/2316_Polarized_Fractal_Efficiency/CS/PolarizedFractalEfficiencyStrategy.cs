using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Polarized Fractal Efficiency strategy.
/// Computes PFE manually from close prices.
/// Buys on PFE turning up from negative, sells on PFE turning down from positive.
/// </summary>
public class PolarizedFractalEfficiencyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _pfePeriod;

	private readonly List<decimal> _closes = new();
	private decimal _prevPfe;
	private decimal _prevPrevPfe;
	private int _formed;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int PfePeriod { get => _pfePeriod.Value; set => _pfePeriod.Value = value; }

	public PolarizedFractalEfficiencyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_pfePeriod = Param(nameof(PfePeriod), 9)
			.SetDisplay("PFE Period", "Indicator calculation period", "Indicators")
			.SetGreaterThanZero();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();
		_prevPfe = 0;
		_prevPrevPfe = 0;
		_formed = 0;

		var sma = new SimpleMovingAverage { Length = 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		var period = PfePeriod;
		if (_closes.Count < period + 1)
			return;

		// Keep only what we need
		while (_closes.Count > period + 2)
			_closes.RemoveAt(0);

		var n = _closes.Count;
		var closeNow = _closes[n - 1];
		var closePast = _closes[n - 1 - period];

		// Direct distance
		var diff = (double)(closeNow - closePast);
		var directDist = Math.Sqrt(diff * diff + (double)(period * period));

		// Sum of bar-to-bar distances
		var sumDist = 0.0;
		for (var i = n - period; i < n; i++)
		{
			var d = (double)(_closes[i] - _closes[i - 1]);
			sumDist += Math.Sqrt(d * d + 1.0);
		}

		if (sumDist == 0)
			return;

		var sign = closeNow >= closePast ? 1.0 : -1.0;
		var pfe = (decimal)(100.0 * sign * directDist / sumDist);

		_formed++;

		if (_formed < 3)
		{
			_prevPrevPfe = _prevPfe;
			_prevPfe = pfe;
			return;
		}

		if (!IsFormedAndOnline())
			return;

		// Trend reversal: PFE was falling and now rising => buy
		if (_prevPfe < _prevPrevPfe && pfe > _prevPfe && Position <= 0)
			BuyMarket();
		// PFE was rising and now falling => sell
		else if (_prevPfe > _prevPrevPfe && pfe < _prevPfe && Position >= 0)
			SellMarket();

		_prevPrevPfe = _prevPfe;
		_prevPfe = pfe;
	}
}
