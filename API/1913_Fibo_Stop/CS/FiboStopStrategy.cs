using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trails stop loss along predefined Fibonacci levels.
/// The stop is moved when the price crosses each Fibonacci retracement level.
/// </summary>
public class FiboStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fiboStart;
	private readonly StrategyParam<decimal> _fiboEnd;
	private readonly StrategyParam<int> _offsetPoints;
	private readonly StrategyParam<DataType> _candleType;

	private static readonly decimal[] _coefficients = { 0m, 0.236m, 0.386m, 0.5m, 0.618m, 0.786m, 1m, 1.27m };
	private decimal _diff;
	private bool _isLong;
	private decimal _stopPrice;
	private int _nextLevelIndex;

	/// <summary>
	/// Fibonacci start price.
	/// </summary>
	public decimal FiboStart
	{
		get => _fiboStart.Value;
		set => _fiboStart.Value = value;
	}

	/// <summary>
	/// Fibonacci end price.
	/// </summary>
	public decimal FiboEnd
	{
		get => _fiboEnd.Value;
		set => _fiboEnd.Value = value;
	}

	/// <summary>
	/// Distance from Fibonacci level to stop in price steps.
	/// </summary>
	public int OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public FiboStopStrategy()
	{
		_fiboStart = Param(nameof(FiboStart), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Start Price", "Fibonacci start level", "Fibonacci")
			.SetCanOptimize(true);

		_fiboEnd = Param(nameof(FiboEnd), 110m)
			.SetGreaterThanZero()
			.SetDisplay("End Price", "Fibonacci end level", "Fibonacci")
			.SetCanOptimize(true);

		_offsetPoints = Param(nameof(OffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Offset Points", "Distance from level to stop in price steps", "Risk")
			.SetCanOptimize(true);

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
		_diff = 0m;
		_isLong = false;
		_stopPrice = 0m;
		_nextLevelIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_diff = FiboEnd - FiboStart;
		_isLong = _diff > 0m;
		_nextLevelIndex = 1;

		var direction = _isLong ? 1m : -1m;
		_stopPrice = FiboStart - direction * OffsetPoints * Security.PriceStep;

		if (_isLong)
			BuyMarket(Volume);
		else
			SellMarket(Volume);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var direction = _isLong ? 1m : -1m;

		while (_nextLevelIndex < _coefficients.Length)
		{
			var level = FiboStart + _diff * _coefficients[_nextLevelIndex];
			if ((_isLong && price > level) || (!_isLong && price < level))
			{
				_stopPrice = level - direction * OffsetPoints * Security.PriceStep;
				_nextLevelIndex++;
			}
			else
			{
				break;
			}
		}

		if (_isLong)
		{
			if (price <= _stopPrice && Position > 0)
				SellMarket(Position);
		}
		else
		{
			if (price >= _stopPrice && Position < 0)
				BuyMarket(-Position);
		}
	}
}
