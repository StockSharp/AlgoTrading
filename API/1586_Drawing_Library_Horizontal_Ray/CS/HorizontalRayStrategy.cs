using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates drawing horizontal rays on SMA crosses.
/// </summary>
public class HorizontalRayStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fast;
	private SimpleMovingAverage _slow;
	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="HorizontalRayStrategy"/>.
	/// </summary>
	public HorizontalRayStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA length", "General")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA length", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fast = new SimpleMovingAverage { Length = FastLength };
		_slow = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fast, _slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fast);
			DrawIndicator(area, _slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp)
		{
			DrawRay(candle.OpenTime, candle.ClosePrice);
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + 1 : 1);
		}
		else if (crossDown)
		{
			DrawRay(candle.OpenTime, candle.ClosePrice);
			if (Position >= 0)
				SellMarket(Position > 0 ? Position + 1 : 1);
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private void DrawRay(DateTimeOffset time, decimal price)
	{
		var end = time + TimeSpan.FromDays(30);
		DrawLine(time, price, end, price);
	}
}
