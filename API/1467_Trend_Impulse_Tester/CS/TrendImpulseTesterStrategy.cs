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
/// Trend impulse tester based on EMA trend and RSI triggers.
/// Enters in direction of trend when RSI crosses threshold.
/// </summary>
public class TrendImpulseTesterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiUp;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiUp { get => _rsiUp.Value; set => _rsiUp.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendImpulseTesterStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "General");

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");

		_rsiUp = Param(nameof(RsiUp), 55)
			.SetGreaterThanZero()
			.SetDisplay("RSI Up", "RSI bullish threshold", "General");

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
		_prevRsi = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(emaFast, emaSlow, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0)
		{
			_prevRsi = rsiValue;
			return;
		}

		var rsiDown = 100 - RsiUp;
		var uptrend = emaFast > emaSlow;
		var downtrend = emaFast < emaSlow;

		if (uptrend && _prevRsi <= RsiUp && rsiValue > RsiUp && Position <= 0)
			BuyMarket();
		else if (downtrend && _prevRsi >= rsiDown && rsiValue < rsiDown && Position >= 0)
			SellMarket();

		_prevRsi = rsiValue;
	}
}
