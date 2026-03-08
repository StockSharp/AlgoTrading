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
/// RVI histogram reversal strategy.
/// Opens long when RVI Average crosses above Signal.
/// Opens short when RVI Average crosses below Signal.
/// </summary>
public class RviHistogramReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAvg;
	private decimal? _prevSig;

	public int RviPeriod { get => _rviPeriod.Value; set => _rviPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RviHistogramReversalStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Period of RVI indicator", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAvg = null;
		_prevSig = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi = new RelativeVigorIndex();
		rvi.Average.Length = RviPeriod;
		rvi.Signal.Length = RviPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rvi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rvi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviVal = value as IRelativeVigorIndexValue;
		if (rviVal?.Average is not decimal avg || rviVal?.Signal is not decimal sig)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevAvg = avg;
			_prevSig = sig;
			return;
		}

		if (_prevAvg is decimal pa && _prevSig is decimal ps)
		{
			// Average crosses above Signal - buy
			if (pa <= ps && avg > sig && Position <= 0)
				BuyMarket();
			// Average crosses below Signal - sell
			else if (pa >= ps && avg < sig && Position >= 0)
				SellMarket();
		}

		_prevAvg = avg;
		_prevSig = sig;
	}
}
