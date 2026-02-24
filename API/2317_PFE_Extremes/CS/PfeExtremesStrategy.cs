using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Polarized Fractal Efficiency breakout strategy.
/// Computes PFE manually. Buys when PFE crosses above upper level,
/// sells when PFE crosses below lower level.
/// </summary>
public class PfeExtremesStrategy : Strategy
{
	private readonly StrategyParam<int> _pfePeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private decimal? _prevPfe;

	public int PfePeriod { get => _pfePeriod.Value; set => _pfePeriod.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PfeExtremesStrategy()
	{
		_pfePeriod = Param(nameof(PfePeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("PFE Period", "Number of bars for PFE calculation", "Indicator");

		_upLevel = Param(nameof(UpLevel), 20m)
			.SetDisplay("Upper Level", "PFE value to trigger long entries", "Signal");

		_downLevel = Param(nameof(DownLevel), -20m)
			.SetDisplay("Lower Level", "PFE value to trigger short entries", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();
		_prevPfe = null;

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

		while (_closes.Count > period + 2)
			_closes.RemoveAt(0);

		var n = _closes.Count;
		var closeNow = _closes[n - 1];
		var closePast = _closes[n - 1 - period];

		var diff = (double)(closeNow - closePast);
		var directDist = Math.Sqrt(diff * diff + (double)(period * period));

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

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPfe = pfe;
			return;
		}

		if (_prevPfe is decimal prev)
		{
			// Upward crossover triggers long
			if (prev <= UpLevel && pfe > UpLevel && Position <= 0)
				BuyMarket();
			// Downward crossover triggers short
			else if (prev >= DownLevel && pfe < DownLevel && Position >= 0)
				SellMarket();
		}

		_prevPfe = pfe;
	}
}
