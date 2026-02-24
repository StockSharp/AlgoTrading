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
/// Triple timeframe strategy simplified to single timeframe.
/// Uses long SMA for trend, medium SMA for entries, short SMA for exits.
/// </summary>
public class TimeshifterTripleTimeframeStrategy : Strategy
{
	private readonly StrategyParam<int> _higherMaLength;
	private readonly StrategyParam<int> _mediumMaLength;
	private readonly StrategyParam<int> _lowerMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMediumMa;
	private decimal _prevShortMa;

	public int HigherMaLength { get => _higherMaLength.Value; set => _higherMaLength.Value = value; }
	public int MediumMaLength { get => _mediumMaLength.Value; set => _mediumMaLength.Value = value; }
	public int LowerMaLength { get => _lowerMaLength.Value; set => _lowerMaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeshifterTripleTimeframeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Chart timeframe", "General");

		_higherMaLength = Param(nameof(HigherMaLength), 50)
			.SetDisplay("Trend MA Length", "Long SMA for trend direction", "Indicators");

		_mediumMaLength = Param(nameof(MediumMaLength), 20)
			.SetDisplay("Entry MA Length", "Medium SMA for entries", "Indicators");

		_lowerMaLength = Param(nameof(LowerMaLength), 10)
			.SetDisplay("Exit MA Length", "Short SMA for exits", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevMediumMa = 0;
		_prevShortMa = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var longMa = new SimpleMovingAverage { Length = HigherMaLength };
		var medMa = new SimpleMovingAverage { Length = MediumMaLength };
		var shortMa = new SimpleMovingAverage { Length = LowerMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(longMa, medMa, shortMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, longMa);
			DrawIndicator(area, medMa);
			DrawIndicator(area, shortMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal longMaVal, decimal medMaVal, decimal shortMaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_prevClose == 0 || _prevMediumMa == 0)
		{
			_prevClose = close;
			_prevMediumMa = medMaVal;
			_prevShortMa = shortMaVal;
			return;
		}

		// Trend direction from long MA
		var uptrend = close > longMaVal;
		var downtrend = close < longMaVal;

		// Entry: medium MA crossover
		var entryLong = _prevClose <= _prevMediumMa && close > medMaVal;
		var entryShort = _prevClose >= _prevMediumMa && close < medMaVal;

		// Exit: short MA crossover
		var exitLong = _prevClose >= _prevShortMa && close < shortMaVal;
		var exitShort = _prevClose <= _prevShortMa && close > shortMaVal;

		// Exits first
		if (Position > 0 && exitLong)
			SellMarket();
		else if (Position < 0 && exitShort)
			BuyMarket();

		// Entries when flat
		if (Position == 0)
		{
			if (uptrend && entryLong)
				BuyMarket();
			else if (downtrend && entryShort)
				SellMarket();
		}

		_prevClose = close;
		_prevMediumMa = medMaVal;
		_prevShortMa = shortMaVal;
	}
}
