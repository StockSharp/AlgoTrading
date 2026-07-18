using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA crossover, Parabolic SAR, and Bulls/Bears Power indicators.
/// </summary>
public class EmaSarBullsBearsStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<int> _bearsBullsPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBears;
	private decimal _prevBulls;
	private bool _hasPrev;

	public int ShortEmaPeriod { get => _shortEmaPeriod.Value; set => _shortEmaPeriod.Value = value; }
	public int LongEmaPeriod { get => _longEmaPeriod.Value; set => _longEmaPeriod.Value = value; }
	public int BearsBullsPeriod { get => _bearsBullsPeriod.Value; set => _bearsBullsPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaSarBullsBearsStrategy()
	{
		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");

		_bearsBullsPeriod = Param(nameof(BearsBullsPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Bulls/Bears Period", "Period for Bulls and Bears Power", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevBears = 0;
		_prevBulls = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var shortEma = new ExponentialMovingAverage { Length = ShortEmaPeriod };
		var longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };
		var sar = new ParabolicSar();
		var bearsPower = new BearPower { Length = BearsBullsPeriod };
		var bullsPower = new BullPower { Length = BearsBullsPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, longEma, sar, bearsPower, bullsPower, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal longEma, decimal sarValue, decimal bearsPower, decimal bullsPower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevBears = bearsPower;
			_prevBulls = bullsPower;
			_hasPrev = true;
			return;
		}

		var shortSignal = shortEma < longEma && sarValue > candle.HighPrice && bearsPower < 0m &&
			bearsPower > _prevBears;

		var longSignal = shortEma > longEma && sarValue < candle.LowPrice && bullsPower > 0m &&
			bullsPower < _prevBulls;

		if (shortSignal && Position >= 0)
			SellMarket();
		else if (longSignal && Position <= 0)
			BuyMarket();

		_prevBears = bearsPower;
		_prevBulls = bullsPower;
	}
}
