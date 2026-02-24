using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Volume Weighted Moving Average (VWMA) slope reversals.
/// Opens or closes positions when the VWMA changes direction.
/// </summary>
public class VwapCloseStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapCloseStrategy()
	{
		_period = Param(nameof(Period), 2)
			.SetDisplay("Period", "VWMA calculation period", "Indicator")
			.SetOptimize(2, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = null;
		_prev2 = null;

		var vwma = new VolumeWeightedMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = vwmaValue;
			return;
		}

		var prev1 = _prev1.Value;
		var prev2 = _prev2.Value;

		// Open long on valley (VWMA turns up)
		if (prev1 < prev2 && vwmaValue > prev1 && Position <= 0)
			BuyMarket();
		// Open short on peak (VWMA turns down)
		else if (prev1 > prev2 && vwmaValue < prev1 && Position >= 0)
			SellMarket();

		_prev2 = prev1;
		_prev1 = vwmaValue;
	}
}
