using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple Moving Average crossover strategy.
/// Buys when the short SMA crosses above the long SMA and sells on opposite signal.
/// </summary>
public class SmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _shortSma = null!;
	private SMA _longSma = null!;

	private bool _isInitialized;
	private bool _wasShortAboveLong;

	/// <summary>
	/// Short SMA period.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long SMA period.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
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
	/// Initializes a new instance of <see cref="SmaCrossoverStrategy"/>.
	/// </summary>
	public SmaCrossoverStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Short SMA Length", "Period of the short SMA", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_longLength = Param(nameof(LongLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("Long SMA Length", "Period of the long SMA", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_shortSma = new SMA { Length = ShortLength };
		_longSma = new SMA { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortSma, _longSma, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortSma);
			DrawIndicator(area, _longSma);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			if (_shortSma.IsFormed && _longSma.IsFormed)
			{
				_wasShortAboveLong = shortValue > longValue;
				_isInitialized = true;
			}
			return;
		}

		var isShortAboveLong = shortValue > longValue;

		if (_wasShortAboveLong != isShortAboveLong)
		{
			if (isShortAboveLong)
			{
				if (Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else
			{
				if (Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}

			_wasShortAboveLong = isShortAboveLong;
		}
	}
}
