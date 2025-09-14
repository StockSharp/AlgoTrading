using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Forex Profit Boost reversal strategy.
/// Opens long when fast EMA crosses below slow SMA.
/// Opens short when fast EMA crosses above slow SMA.
/// Uses optional stop-loss and take-profit in price points.
/// </summary>
public class ForexProfitBoostStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _wasFastAboveSlow;
	private decimal _entryPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// The type of candles used for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ForexProfitBoostStrategy"/>.
	/// </summary>
	public ForexProfitBoostStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Period of the fast EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Period", "Period of the slow SMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit distance in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1000m, 4000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
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

		_wasFastAboveSlow = null;
		_entryPrice = 0m;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new EMA { Length = FastPeriod };
		var slowSma = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isFastAboveSlow = fastValue > slowValue;

		if (_wasFastAboveSlow is null)
		{
			_wasFastAboveSlow = isFastAboveSlow;
			return;
		}

		// Detect crossover and trade against the direction (reversal)
		if (_wasFastAboveSlow == true && !isFastAboveSlow)
		{
			// Fast EMA crossed below slow SMA -> open long
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_isLongPosition = true;
			}
		}
		else if (_wasFastAboveSlow == false && isFastAboveSlow)
		{
			// Fast EMA crossed above slow SMA -> open short
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_isLongPosition = false;
			}
		}

		// Update crossover state
		_wasFastAboveSlow = isFastAboveSlow;

		// Check stop loss and take profit
		CheckRisk(candle.ClosePrice);
	}

	private void CheckRisk(decimal currentPrice)
	{
		if (Position == 0 || _entryPrice == 0)
			return;

		if (_isLongPosition)
		{
			if (_stopLoss.Value > 0m && currentPrice <= _entryPrice - _stopLoss.Value)
			{
				SellMarket(Position);
				return;
			}

			if (_takeProfit.Value > 0m && currentPrice >= _entryPrice + _takeProfit.Value)
				SellMarket(Position);
		}
		else
		{
			if (_stopLoss.Value > 0m && currentPrice >= _entryPrice + _stopLoss.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_takeProfit.Value > 0m && currentPrice <= _entryPrice - _takeProfit.Value)
				BuyMarket(Math.Abs(Position));
		}
	}
}

