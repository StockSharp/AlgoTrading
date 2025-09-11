using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with configurable stop-loss.
/// Buys when the short EMA crosses above the long EMA and sells short on the opposite crossover.
/// </summary>
public class KyrieCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KyrieCrossoverStrategy()
	{
		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Period", "Period of the short EMA", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 323)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Period", "Period of the long EMA", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 10);

		_riskPercent = Param(nameof(RiskPercent), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Stop loss percentage from entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_entryPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortEma = new EMA { Length = ShortEmaPeriod };
		var longEma = new EMA { Length = LongEmaPeriod };

		var subscription = SubscribeCandles(CandleType);

		var prevShort = 0m;
		var prevLong = 0m;
		var wasShortBelowLong = false;
		var initialized = false;

		subscription
			.Bind(shortEma, longEma, (candle, shortValue, longValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!initialized && shortEma.IsFormed && longEma.IsFormed)
				{
					prevShort = shortValue;
					prevLong = longValue;
					wasShortBelowLong = shortValue < longValue;
					initialized = true;
					return;
				}

				if (!initialized)
					return;

				var isShortBelowLong = shortValue < longValue;

				if (wasShortBelowLong != isShortBelowLong)
				{
					if (!isShortBelowLong && Position <= 0)
					{
						_entryPrice = candle.ClosePrice;
						_isLong = true;
						BuyMarket(Volume + Math.Abs(Position));
					}
					else if (isShortBelowLong && Position >= 0)
					{
						_entryPrice = candle.ClosePrice;
						_isLong = false;
						SellMarket(Volume + Math.Abs(Position));
					}

					wasShortBelowLong = isShortBelowLong;
				}

				if (Position != 0 && _entryPrice != 0)
					CheckStopLoss(candle.ClosePrice);

				prevShort = shortValue;
				prevLong = longValue;
			})
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void CheckStopLoss(decimal currentPrice)
	{
		var stopLossThreshold = _riskPercent.Value / 100m;

		if (_isLong && Position > 0)
		{
			var stopPrice = _entryPrice * (1m - stopLossThreshold);
			if (currentPrice <= stopPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (!_isLong && Position < 0)
		{
			var stopPrice = _entryPrice * (1m + stopLossThreshold);
			if (currentPrice >= stopPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}
