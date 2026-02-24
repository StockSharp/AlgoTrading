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
/// Correlation-based TSI with SuperTrend direction.
/// Calculates price-time correlation as trend strength, uses SuperTrend for direction.
/// </summary>
public class TsiSuperTrendDecisionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tsiLength;
	private readonly StrategyParam<int> _stLength;
	private readonly StrategyParam<decimal> _stMultiplier;
	private readonly StrategyParam<decimal> _threshold;

	private decimal[] _prices = Array.Empty<decimal>();
	private int _index;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int TsiLength { get => _tsiLength.Value; set => _tsiLength.Value = value; }
	public int StLength { get => _stLength.Value; set => _stLength.Value = value; }
	public decimal StMultiplier { get => _stMultiplier.Value; set => _stMultiplier.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }

	public TsiSuperTrendDecisionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_tsiLength = Param(nameof(TsiLength), 64)
			.SetDisplay("TSI Length", "Correlation period", "Indicators");
		_stLength = Param(nameof(StLength), 10)
			.SetDisplay("ST Length", "SuperTrend length", "Indicators");
		_stMultiplier = Param(nameof(StMultiplier), 3m)
			.SetDisplay("ST Mult", "SuperTrend factor", "Indicators");
		_threshold = Param(nameof(Threshold), 0.241m)
			.SetDisplay("TSI Threshold", "Entry threshold", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prices = Array.Empty<decimal>();
		_index = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prices = new decimal[TsiLength];
		_index = 0;

		var superTrend = new SuperTrend { Length = StLength, Multiplier = StMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(superTrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stValue is not SuperTrendIndicatorValue st)
			return;

		var isUp = st.IsUpTrend;

		_prices[_index % TsiLength] = candle.ClosePrice;
		_index++;

		if (_index < TsiLength)
			return;

		var tsi = CalculateCorrelation();

		// Entry: SuperTrend direction + correlation confirms trend
		if (isUp && tsi > -Threshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (!isUp && tsi < Threshold && Position >= 0)
		{
			SellMarket();
		}

		// Exit: trend reversal or correlation weakens
		if (Position > 0 && (!isUp || tsi < Threshold))
		{
			SellMarket();
		}
		else if (Position < 0 && (isUp || tsi > -Threshold))
		{
			BuyMarket();
		}
	}

	private decimal CalculateCorrelation()
	{
		var n = TsiLength;
		decimal sumX = 0m, sumY = 0m, sumX2 = 0m, sumY2 = 0m, sumXY = 0m;
		for (var i = 0; i < n; i++)
		{
			var x = _prices[(_index - n + i) % n];
			var y = (decimal)i;
			sumX += x;
			sumY += y;
			sumX2 += x * x;
			sumY2 += y * y;
			sumXY += x * y;
		}

		var num = (double)(n * sumXY - sumX * sumY);
		var den = Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
		if (den == 0.0)
			return 0m;
		return (decimal)(num / den);
	}
}
