using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on WMA crossover with KAMA confirmation.
/// </summary>
public class SnowiesoStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevKama;
	private bool _hasPrev;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int KamaLength { get => _kamaLength.Value; set => _kamaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SnowiesoStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast WMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow WMA period", "Indicators");

		_kamaLength = Param(nameof(KamaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "KAMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_prevKama = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new WeightedMovingAverage { Length = FastLength };
		var slow = new WeightedMovingAverage { Length = SlowLength };
		var kama = new KaufmanAdaptiveMovingAverage { Length = KamaLength };

		SubscribeCandles(CandleType).Bind(fast, slow, kama, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_prevKama = kamaValue;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fastValue > slowValue;
		var crossDown = _prevFast >= _prevSlow && fastValue < slowValue;
		var kamaRising = kamaValue > _prevKama;
		var kamaFalling = kamaValue < _prevKama;

		if (crossUp && kamaRising && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && kamaFalling && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevKama = kamaValue;
	}
}
