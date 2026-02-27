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
/// Strategy that trades using Relative Vigor Index on three timeframes.
/// </summary>
public class TripleRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;

	private int _trend1;
	private int _trend2;

	private decimal? _prevAvg3;
	private decimal? _prevSig3;

	public int RviPeriod { get => _rviPeriod.Value; set => _rviPeriod.Value = value; }
	public DataType CandleType1 { get => _candleType1.Value; set => _candleType1.Value = value; }
	public DataType CandleType2 { get => _candleType2.Value; set => _candleType2.Value = value; }
	public DataType CandleType3 { get => _candleType3.Value; set => _candleType3.Value = value; }

	public TripleRviStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Period of RVI", "General");

		_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Timeframe 1", "Higher timeframe", "General");

		_candleType2 = Param(nameof(CandleType2), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Timeframe 2", "Middle timeframe", "General");

		_candleType3 = Param(nameof(CandleType3), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Timeframe 3", "Trading timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi1 = new RelativeVigorIndex();
		rvi1.Average.Length = RviPeriod;
		var rvi2 = new RelativeVigorIndex();
		rvi2.Average.Length = RviPeriod;
		var rvi3 = new RelativeVigorIndex();
		rvi3.Average.Length = RviPeriod;

		var sub1 = SubscribeCandles(CandleType1);
		sub1.BindEx(rvi1, (candle, val) =>
		{
			if (candle.State != CandleStates.Finished) return;
			var rv = (RelativeVigorIndexValue)val;
			if (rv.Average is not decimal avg || rv.Signal is not decimal sig) return;
			_trend1 = avg > sig ? 1 : avg < sig ? -1 : 0;
		}).Start();

		var sub2 = SubscribeCandles(CandleType2);
		sub2.BindEx(rvi2, (candle, val) =>
		{
			if (candle.State != CandleStates.Finished) return;
			var rv = (RelativeVigorIndexValue)val;
			if (rv.Average is not decimal avg || rv.Signal is not decimal sig) return;
			_trend2 = avg > sig ? 1 : avg < sig ? -1 : 0;
		}).Start();

		var sub3 = SubscribeCandles(CandleType3);
		sub3.BindEx(rvi3, ProcessCandle3).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub3);
			DrawIndicator(area, rvi3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle3(ICandleMessage candle, IIndicatorValue val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rv = (RelativeVigorIndexValue)val;
		if (rv.Average is not decimal avg || rv.Signal is not decimal sig)
			return;

		if (_prevAvg3 is decimal prevAvg && _prevSig3 is decimal prevSig)
		{
			var crossUp = prevAvg < prevSig && avg >= sig;
			var crossDown = prevAvg > prevSig && avg <= sig;

			if (crossUp && _trend1 > 0 && _trend2 > 0 && Position <= 0)
				BuyMarket();
			else if (crossDown && _trend1 < 0 && _trend2 < 0 && Position >= 0)
				SellMarket();

			// Exit on trend reversal
			if (Position > 0 && (_trend1 < 0 || _trend2 < 0))
				SellMarket();
			else if (Position < 0 && (_trend1 > 0 || _trend2 > 0))
				BuyMarket();
		}

		_prevAvg3 = avg;
		_prevSig3 = sig;
	}
}
