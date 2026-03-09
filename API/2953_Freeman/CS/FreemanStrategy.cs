using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Freeman strategy using dual MA crossover with RSI filter.
/// </summary>
public class FreemanStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	public FreemanStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for filter", "Indicators");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 55m)
			.SetDisplay("RSI Buy Level", "RSI below which buys are allowed", "Levels");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 45m)
			.SetDisplay("RSI Sell Level", "RSI above which sells are allowed", "Levels");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = null;
		_prevSlow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastSma = new SimpleMovingAverage { Length = FastPeriod };
		var slowSma = new SimpleMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, rsi, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast == null || _prevSlow == null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var prevAbove = _prevFast.Value > _prevSlow.Value;
		var currAbove = fast > slow;

		_prevFast = fast;
		_prevSlow = slow;

		// MA crossover
		if (!prevAbove && currAbove)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (prevAbove && !currAbove)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
