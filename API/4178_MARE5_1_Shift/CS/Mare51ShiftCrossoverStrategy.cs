using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MARE5.1 Shift Crossover: SMA crossover with ATR stops.
/// </summary>
public class Mare51ShiftCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public Mare51ShiftCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");
		_fastSmaLength = Param(nameof(FastSmaLength), 13)
			.SetDisplay("Fast SMA", "Fast SMA period.", "Indicators");
		_slowSmaLength = Param(nameof(SlowSmaLength), 55)
			.SetDisplay("Slow SMA", "Slow SMA period.", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastSmaLength { get => _fastSmaLength.Value; set => _fastSmaLength.Value = value; }
	public int SlowSmaLength { get => _slowSmaLength.Value; set => _slowSmaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = 0; _prevSlow = 0; _entryPrice = 0;
		var fastEma = new ExponentialMovingAverage { Length = FastSmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowSmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fastEma); DrawIndicator(area, slowEma); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevFast == 0 || _prevSlow == 0 || atrVal <= 0) { _prevFast = fastVal; _prevSlow = slowVal; return; }
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if ((fastVal < slowVal && _prevFast >= _prevSlow) || close <= _entryPrice - atrVal * 2m) { SellMarket(); _entryPrice = 0; }
		}
		else if (Position < 0)
		{
			if ((fastVal > slowVal && _prevFast <= _prevSlow) || close >= _entryPrice + atrVal * 2m) { BuyMarket(); _entryPrice = 0; }
		}

		if (Position == 0)
		{
			if (fastVal > slowVal && _prevFast <= _prevSlow) { _entryPrice = close; BuyMarket(); }
			else if (fastVal < slowVal && _prevFast >= _prevSlow) { _entryPrice = close; SellMarket(); }
		}
		_prevFast = fastVal; _prevSlow = slowVal;
	}
}
