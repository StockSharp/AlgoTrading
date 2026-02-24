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
/// Twisted SMA strategy.
/// Opens long when three SMAs align bullish and price is above main SMA while KAMA is moving.
/// Exits when SMAs align bearish.
/// </summary>
public class TwistedSma4hStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _midLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _mainSmaLength;
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevKama;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int MidLength { get => _midLength.Value; set => _midLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int MainSmaLength { get => _mainSmaLength.Value; set => _mainSmaLength.Value = value; }
	public int KamaLength { get => _kamaLength.Value; set => _kamaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TwistedSma4hStrategy()
	{
		_fastLength = Param(nameof(FastLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Length of the fastest SMA", "SMA");

		_midLength = Param(nameof(MidLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Middle SMA Length", "Length of the middle SMA", "SMA");

		_slowLength = Param(nameof(SlowLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Length of the slow SMA", "SMA");

		_mainSmaLength = Param(nameof(MainSmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Main SMA Length", "Length of the main SMA", "SMA");

		_kamaLength = Param(nameof(KamaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Length of KAMA", "KAMA");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevKama = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SimpleMovingAverage { Length = FastLength };
		var midSma = new SimpleMovingAverage { Length = MidLength };
		var slowSma = new SimpleMovingAverage { Length = SlowLength };
		var mainSma = new SimpleMovingAverage { Length = MainSmaLength };
		var kama = new KaufmanAdaptiveMovingAverage { Length = KamaLength };
		_prevKama = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, midSma, slowSma, mainSma, kama, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, midSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal mid, decimal slow, decimal main, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevKama == 0m)
		{
			_prevKama = kamaValue;
			return;
		}

		var isFlat = Math.Abs(kamaValue - _prevKama) / _prevKama < 0.0005m;

		var longCond = fast > mid && mid > slow && candle.ClosePrice > main && !isFlat;
		var shortCond = fast < mid && mid < slow;

		if (longCond && Position <= 0)
			BuyMarket();
		else if (shortCond && Position > 0)
			SellMarket();

		_prevKama = kamaValue;
	}
}
