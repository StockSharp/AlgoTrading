using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kaufman Adaptive Moving Average (KAMA) strategy.
/// </summary>
public class KaufmanAdaptiveMovingAverageStrategy : Strategy
{
	public enum TradeSide
	{
		Long,
		Short,
		Both
	}

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _fast;
	private readonly StrategyParam<int> _slow;
	private readonly StrategyParam<int> _risingPeriod;
	private readonly StrategyParam<int> _fallingPeriod;
	private readonly StrategyParam<TradeSide> _orderDirection;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevKama;
	private int _risingCount;
	private int _fallingCount;
	private bool _isFirst = true;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int Fast { get => _fast.Value; set => _fast.Value = value; }
	public int Slow { get => _slow.Value; set => _slow.Value = value; }
	public int RisingPeriod { get => _risingPeriod.Value; set => _risingPeriod.Value = value; }
	public int FallingPeriod { get => _fallingPeriod.Value; set => _fallingPeriod.Value = value; }
	public TradeSide OrderDirection { get => _orderDirection.Value; set => _orderDirection.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KaufmanAdaptiveMovingAverageStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "KAMA lookback period", "KAMA")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_fast = Param(nameof(Fast), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast period", "Fast EMA length for KAMA", "KAMA")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slow = Param(nameof(Slow), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow period", "Slow EMA length for KAMA", "KAMA")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_risingPeriod = Param(nameof(RisingPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Rising period", "Bars for KAMA rising condition", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_fallingPeriod = Param(nameof(FallingPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Falling period", "Bars for KAMA falling condition", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_orderDirection = Param(nameof(OrderDirection), TradeSide.Long)
			.SetDisplay("Order direction", "Allowed trade direction", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevKama = default;
		_risingCount = 0;
		_fallingCount = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var kama = new KaufmanAdaptiveMovingAverage
		{
			Length = Length,
			FastSCPeriod = Fast,
			SlowSCPeriod = Slow
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(kama, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kama);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevKama = kamaValue;
			_isFirst = false;
			return;
		}

		if (kamaValue > _prevKama)
		{
			_risingCount++;
			_fallingCount = 0;
		}
		else if (kamaValue < _prevKama)
		{
			_fallingCount++;
			_risingCount = 0;
		}
		else
		{
			_risingCount = 0;
			_fallingCount = 0;
		}

		var isRising = _risingCount >= RisingPeriod;
		var isFalling = _fallingCount >= FallingPeriod;

		if (isRising)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			var allowLong = OrderDirection == TradeSide.Long || OrderDirection == TradeSide.Both;
			if (allowLong && Position == 0)
				BuyMarket(Volume);
		}
		else if (isFalling)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			var allowShort = OrderDirection == TradeSide.Short || OrderDirection == TradeSide.Both;
			if (allowShort && Position == 0)
				SellMarket(Volume);
		}

		_prevKama = kamaValue;
	}
}

