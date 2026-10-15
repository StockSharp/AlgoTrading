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
/// Strategy based on "Stalin" indicator.
/// Uses fast and slow EMAs with optional RSI filter.
/// </summary>
public class StalinStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private RelativeStrengthIndex _rsi;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StalinStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
			.SetOptimize(5, 30, 1);

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
			.SetOptimize(20, 50, 1);

		_rsiLength = Param(nameof(RsiLength), 17)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicator")
			.SetOptimize(10, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_fastMa = null;
		_slowMa = null;
		_rsi = null;
		_prevFast = 0;
		_prevSlow = 0;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var buySignal = _prevFast <= _prevSlow && fast > slow && rsiValue > 50m;
		var sellSignal = _prevFast >= _prevSlow && fast < slow && rsiValue < 50m;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
