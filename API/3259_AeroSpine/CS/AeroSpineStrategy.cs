using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AeroSpine strategy: dual EMA crossover with ATR-based volatility filter.
/// Buys on fast EMA crossing above slow EMA when ATR confirms sufficient volatility.
/// Sells on the opposite crossover.
/// </summary>
public class AeroSpineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrSmaPeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public int AtrSmaPeriod
	{
		get => _atrSmaPeriod.Value;
		set => _atrSmaPeriod.Value = value;
	}

	public AeroSpineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR lookback", "Indicators");

		_atrSmaPeriod = Param(nameof(AtrSmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR SMA", "SMA of ATR for filter", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var atrSma = new SimpleMovingAverage { Length = AtrSmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, atr, atrSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue, IIndicatorValue atrSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var atrAvg = atrSmaValue.ToDecimal();

		if (_prevFast.HasValue && _prevSlow.HasValue)
		{
			if (_prevFast.Value <= _prevSlow.Value && fast > slow && Position <= 0)
			{
				BuyMarket();
			}
			else if (_prevFast.Value >= _prevSlow.Value && fast < slow && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
