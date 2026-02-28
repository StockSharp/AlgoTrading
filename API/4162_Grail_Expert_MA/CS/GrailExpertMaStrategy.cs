using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grail Expert MA: SMA crossover with ATR stops.
/// </summary>
public class GrailExpertMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public GrailExpertMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastSmaLength = Param(nameof(FastSmaLength), 10)
			.SetDisplay("Fast SMA", "Fast SMA period.", "Indicators");

		_slowSmaLength = Param(nameof(SlowSmaLength), 40)
			.SetDisplay("Slow SMA", "Slow SMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastSmaLength
	{
		get => _fastSmaLength.Value;
		set => _fastSmaLength.Value = value;
	}

	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;

		var fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0 || atrVal <= 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (fastVal < slowVal && _prevFast >= _prevSlow)
			{
				SellMarket();
				_entryPrice = 0;
			}
			else if (close <= _entryPrice - atrVal * 2m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (fastVal > slowVal && _prevFast <= _prevSlow)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			else if (close >= _entryPrice + atrVal * 2m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (fastVal > slowVal && _prevFast <= _prevSlow)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (fastVal < slowVal && _prevFast >= _prevSlow)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
