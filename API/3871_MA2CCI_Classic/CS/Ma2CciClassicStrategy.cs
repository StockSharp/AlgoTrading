using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA2CCI Classic strategy - dual SMA crossover with CCI zero-line filter.
/// Buys when fast SMA crosses above slow SMA and CCI above zero.
/// Sells when fast SMA crosses below slow SMA and CCI below zero.
/// </summary>
public class Ma2CciClassicStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Ma2CciClassicStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI lookback", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;

		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal cci)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!_hasPrev) { _prevFast = fast; _prevSlow = slow; _hasPrev = true; return; }

		if (_prevFast <= _prevSlow && fast > slow && cci > 0 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevFast >= _prevSlow && fast < slow && cci < 0 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		_prevFast = fast; _prevSlow = slow;
	}
}
