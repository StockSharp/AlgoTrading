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
/// Strategy based on ADX directional cross and Hull moving averages.
/// Buys when +DI crosses above -DI and sells when -DI crosses above +DI.
/// </summary>
public class ExpAdxCrossHullStyleStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _fastHullLength;
	private readonly StrategyParam<int> _slowHullLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevPlusDi;
	private decimal? _prevMinusDi;

	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public int FastHullLength { get => _fastHullLength.Value; set => _fastHullLength.Value = value; }
	public int SlowHullLength { get => _slowHullLength.Value; set => _slowHullLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpAdxCrossHullStyleStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");
		_fastHullLength = Param(nameof(FastHullLength), 20)
			.SetDisplay("Fast Hull Length", "Period of the fast Hull MA", "Indicators");
		_slowHullLength = Param(nameof(SlowHullLength), 50)
			.SetDisplay("Slow Hull Length", "Period of the slow Hull MA", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPlusDi = _prevMinusDi = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var fastHull = new HullMovingAverage { Length = FastHullLength };
		var slowHull = new HullMovingAverage { Length = SlowHullLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, fastHull, slowHull, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastHull);
			DrawIndicator(area, slowHull);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue fastHullValue, IIndicatorValue slowHullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxVal = adxValue as IAverageDirectionalIndexValue;
		if (adxVal?.Dx is not IDirectionalIndexValue dx)
			return;

		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
			return;

		if (!fastHullValue.IsFormed || !slowHullValue.IsFormed)
			return;

		var fastHull = fastHullValue.GetValue<decimal>();
		var slowHull = slowHullValue.GetValue<decimal>();

		if (_prevPlusDi is decimal prevPlus && _prevMinusDi is decimal prevMinus)
		{
			// Entry: +DI crosses above -DI
			if (prevPlus <= prevMinus && plusDi > minusDi && Position <= 0)
				BuyMarket();
			// Entry: -DI crosses above +DI
			else if (prevPlus >= prevMinus && plusDi < minusDi && Position >= 0)
				SellMarket();

			// Exit on Hull MA cross
			if (Position > 0 && fastHull < slowHull)
				SellMarket();
			else if (Position < 0 && fastHull > slowHull)
				BuyMarket();
		}

		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}
}
