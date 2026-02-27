using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp Iin MA Signal MMRec strategy. Uses SMA crossover.
/// </summary>
public class ExpIinMaSignalMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public ExpIinMaSignalMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 8).SetGreaterThanZero().SetDisplay("Fast SMA", "Fast SMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21).SetGreaterThanZero().SetDisplay("Slow SMA", "Slow SMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = null; _prevSlow = null;
		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal f, decimal s)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevFast == null || _prevSlow == null) { _prevFast = f; _prevSlow = s; return; }
		var pa = _prevFast.Value > _prevSlow.Value; var ca = f > s;
		_prevFast = f; _prevSlow = s;
		if (!pa && ca && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (pa && !ca && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
	}
}
