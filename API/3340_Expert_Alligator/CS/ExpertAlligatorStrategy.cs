using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 5 "Expert Alligator" strategy that trades on Bill Williams Alligator line crosses.
/// The strategy watches the smoothed moving average lines (jaw, teeth, lips) shifted forward as in the EA and
/// reproduces the original entry and exit conditions.
/// </summary>
public class ExpertAlligatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _crossMeasurePoints;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();

	private decimal _pointValue;
	private decimal _crossThreshold;
	private bool _crossed;

	/// <summary>
	/// Trade volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle data type used for Alligator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Alligator jaw smoothed moving average period.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw line.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Alligator teeth smoothed moving average period.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth line.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Alligator lips smoothed moving average period.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips line.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Distance in MetaTrader "points" required before another cross can trigger.
	/// </summary>
	public int CrossMeasurePoints
	{
		get => _crossMeasurePoints.Value;
		set => _crossMeasurePoints.Value = value;
	}

	/// <summary>
	/// Create <see cref="ExpertAlligatorStrategy"/> with default MetaTrader parameters.
	/// </summary>
	public ExpertAlligatorStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade size in lots or contracts", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candle subscription", "General");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Period", "Smoothed moving average length for the jaw", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
			.SetDisplay("Jaw Shift", "Forward shift applied to the jaw", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Smoothed moving average length for the teeth", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
			.SetDisplay("Teeth Shift", "Forward shift applied to the teeth", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Period", "Smoothed moving average length for the lips", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetDisplay("Lips Shift", "Forward shift applied to the lips", "Alligator");

		_crossMeasurePoints = Param(nameof(CrossMeasurePoints), 5)
			.SetDisplay("Cross Measure", "Minimum jaw/teeth/lips separation to reset entries", "Alligator");
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

		_jawHistory = Array.Empty<decimal?>();
		_teethHistory = Array.Empty<decimal?>();
		_lipsHistory = Array.Empty<decimal?>();

		_pointValue = 0m;
		_crossThreshold = 0m;
		_crossed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);

		_pointValue = CalculatePointValue();
		_crossThreshold = CrossMeasurePoints > 0 ? CrossMeasurePoints * _pointValue : 0m;
		_crossed = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median, candle.OpenTime, true);
		var teethValue = _teeth.Process(median, candle.OpenTime, true);
		var lipsValue = _lips.Process(median, candle.OpenTime, true);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
			return;

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();

		UpdateHistory(_jawHistory, jaw);
		UpdateHistory(_teethHistory, teeth);
		UpdateHistory(_lipsHistory, lips);

		if (Position > 0 && TryShouldCloseLong(out var closeLong) && closeLong)
		{
			SellMarket(volume: Position);
		}
		else if (Position < 0 && TryShouldCloseShort(out var closeShort) && closeShort)
		{
			BuyMarket(volume: Math.Abs(Position));
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (IsCrossBlocked())
			return;

		if (TryCheckLongEntry(out var goLong) && goLong)
		{
			BuyMarket(volume: OrderVolume);
			return;
		}

		if (TryCheckShortEntry(out var goShort) && goShort)
		{
			SellMarket(volume: OrderVolume);
		}
	}

	private bool TryCheckLongEntry(out bool goLong)
	{
		goLong = false;

		if (!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, -2, out var lipsTeethMinus2) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, -1, out var lipsTeethMinus1) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 0, out var lipsTeeth0) ||
			!TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, -2, out var teethJawMinus2) ||
			!TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, -1, out var teethJawMinus1) ||
			!TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, 0, out var teethJaw0))
		{
			return false;
		}

		if (lipsTeethMinus2 >= lipsTeethMinus1 && lipsTeethMinus1 >= lipsTeeth0 && lipsTeeth0 >= 0m &&
			teethJawMinus2 >= teethJawMinus1 && teethJawMinus1 >= teethJaw0 && teethJaw0 >= 0m)
		{
			_crossed = true;
			goLong = true;
		}

		return true;
	}

	private bool TryCheckShortEntry(out bool goShort)
	{
		goShort = false;

		if (!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, -2, out var lipsTeethMinus2) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, -1, out var lipsTeethMinus1) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 0, out var lipsTeeth0) ||
			!TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, -2, out var teethJawMinus2) ||
			!TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, -1, out var teethJawMinus1) ||
			!TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, 0, out var teethJaw0))
		{
			return false;
		}

		if (lipsTeethMinus2 <= lipsTeethMinus1 && lipsTeethMinus1 <= lipsTeeth0 && lipsTeeth0 <= 0m &&
			teethJawMinus2 <= teethJawMinus1 && teethJawMinus1 <= teethJaw0 && teethJaw0 <= 0m)
		{
			_crossed = true;
			goShort = true;
		}

		return true;
	}

	private bool TryShouldCloseLong(out bool closeLong)
	{
		closeLong = false;

		if (!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, -1, out var diffMinus1) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 0, out var diff0) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 1, out var diff1))
		{
			return false;
		}

		closeLong = diffMinus1 < 0m && diff0 >= 0m && diff1 > 0m;
		return true;
	}

	private bool TryShouldCloseShort(out bool closeShort)
	{
		closeShort = false;

		if (!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, -1, out var diffMinus1) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 0, out var diff0) ||
			!TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 1, out var diff1))
		{
			return false;
		}

		closeShort = diffMinus1 > 0m && diff0 <= 0m && diff1 < 0m;
		return true;
	}

	private bool IsCrossBlocked()
	{
		if (!_crossed)
			return false;

		if (TryGetDifference(_lipsHistory, LipsShift, _teethHistory, TeethShift, 1, out var lipsTeeth) &&
			TryGetDifference(_teethHistory, TeethShift, _jawHistory, JawShift, 1, out var teethJaw) &&
			TryGetDifference(_lipsHistory, LipsShift, _jawHistory, JawShift, 1, out var lipsJaw))
		{
			var threshold = _crossThreshold;
			if (Math.Abs(lipsTeeth) > threshold || Math.Abs(teethJaw) > threshold || Math.Abs(lipsJaw) > threshold)
			{
				_crossed = false;
				return false;
			}
		}

		return _crossed;
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var size = Math.Max(shift + 5, 5);
		return new decimal?[size];
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
			return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static bool TryGetDifference(decimal?[] first, int firstShift, decimal?[] second, int secondShift, int mqlIndex, out decimal diff)
	{
		diff = 0m;

		if (!TryGetShiftedValue(first, firstShift, mqlIndex, out var firstValue) ||
			!TryGetShiftedValue(second, secondShift, mqlIndex, out var secondValue))
		{
			return false;
		}

		diff = firstValue - secondValue;
		return true;
	}

	private static bool TryGetShiftedValue(decimal?[] buffer, int shift, int mqlIndex, out decimal value)
	{
		value = 0m;

		if (buffer.Length == 0)
			return false;

		var relativeIndex = shift - mqlIndex;
		if (relativeIndex < 0)
			return false;

		var offsetFromEnd = relativeIndex + 1;
		if (offsetFromEnd <= 0 || offsetFromEnd > buffer.Length)
			return false;

		var index = buffer.Length - offsetFromEnd;
		if (index < 0 || index >= buffer.Length)
			return false;

		if (buffer[index] is not decimal stored)
			return false;

		value = stored;
		return true;
	}

	private decimal CalculatePointValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		var decimals = Security?.Decimals ?? 0;
		if (decimals > 0)
		{
			var value = (decimal)Math.Pow(10, -decimals);
			return value > 0m ? value : 0.0001m;
		}

		return 0.0001m;
	}
}
