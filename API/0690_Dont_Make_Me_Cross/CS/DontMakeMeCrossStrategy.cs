using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with vertical shift.
/// </summary>
public class DontMakeMeCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<int> _shiftAmount;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Amount added to EMA values.
	/// </summary>
	public int ShiftAmount
	{
		get => _shiftAmount.Value;
		set => _shiftAmount.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DontMakeMeCrossStrategy"/>.
	/// </summary>
	public DontMakeMeCrossStrategy()
	{
		_shortEmaLength = Param(nameof(ShortEmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Length", "Period for the short EMA", "Indicators")
			.SetCanOptimize(true);

		_longEmaLength = Param(nameof(LongEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Length", "Period for the long EMA", "Indicators")
			.SetCanOptimize(true);

		_shiftAmount = Param(nameof(ShiftAmount), -50)
			.SetDisplay("Shift Amount", "Vertical shift applied to EMA values", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(-100, 0, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var shortEma = new EMA { Length = ShortEmaLength };
		var longEma = new EMA { Length = LongEmaLength };

		var subscription = SubscribeCandles(CandleType);

		var isInitialized = false;
		var prevShort = 0m;
		var prevLong = 0m;
		var shift = _shiftAmount.Value;

		subscription.Bind(shortEma, longEma, (candle, shortValue, longValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var shiftedShort = shortValue + shift;
			var shiftedLong = longValue + shift;

			if (!isInitialized)
			{
				prevShort = shiftedShort;
				prevLong = shiftedLong;
				isInitialized = true;
				return;
			}

			var wasShortAboveLong = prevShort > prevLong;
			var isShortAboveLong = shiftedShort > shiftedLong;

			if (wasShortAboveLong != isShortAboveLong)
			{
				var volume = Volume + Math.Abs(Position);

				if (isShortAboveLong && Position <= 0)
					BuyMarket(volume);
				else if (!isShortAboveLong && Position >= 0)
					SellMarket(volume);
			}

			prevShort = shiftedShort;
			prevLong = shiftedLong;
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}
}
