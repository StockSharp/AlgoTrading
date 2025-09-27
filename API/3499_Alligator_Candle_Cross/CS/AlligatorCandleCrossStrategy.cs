using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades candle breakouts through the Bill Williams Alligator.
/// Generates long and/or short signals when the previous close sits on one side of the Alligator teeth
/// and the current candle body crosses to the opposite side. Fixed pip stops and targets manage exits.
/// </summary>
public class AlligatorCandleCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<AlligatorCrossModes> _entryMode;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();

	private decimal _pipSize;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;
	private decimal? _previousClose;

	/// <summary>
	/// Trade volume in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips. Zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Alligator jaw moving average length.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw when evaluating signals.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Alligator teeth moving average length.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth when evaluating signals.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Alligator lips moving average length.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips when evaluating signals.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Candle data type consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Directional bias for the candle cross signals.
	/// </summary>
	public AlligatorCrossModes EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlligatorCandleCrossStrategy"/> class.
	/// </summary>
	public AlligatorCandleCrossStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trade size in lots or contracts", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk");

		_jawPeriod = Param(nameof(JawPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Alligator jaw moving average length", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetNotNegative()
		.SetDisplay("Jaw Shift", "Forward shift for the jaw line", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Period", "Alligator teeth moving average length", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetNotNegative()
		.SetDisplay("Teeth Shift", "Forward shift for the teeth line", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Period", "Alligator lips moving average length", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetNotNegative()
		.SetDisplay("Lips Shift", "Forward shift for the lips line", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for candle subscription", "General");

		_entryMode = Param(nameof(EntryMode), AlligatorCrossModes.Both)
		.SetDisplay("Entry Mode", "Directional bias for cross signals", "Trading");
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

		_pipSize = 0m;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);

		_pipSize = CalculatePipSize();

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

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_longExitRequested = false;
			_shortExitRequested = false;
			return;
		}

		var entryPrice = PositionPrice;

		if (Position > 0 && delta > 0)
		{
			_longStopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : (decimal?)null;
			_longTakePrice = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : (decimal?)null;
			_longExitRequested = false;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_shortExitRequested = false;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : (decimal?)null;
			_shortTakePrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : (decimal?)null;
			_shortExitRequested = false;
			_longStopPrice = null;
			_longTakePrice = null;
			_longExitRequested = false;
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
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		UpdateHistory(_jawHistory, jawValue.ToDecimal());
		UpdateHistory(_teethHistory, teethValue.ToDecimal());
		UpdateHistory(_lipsHistory, lipsValue.ToDecimal());

		if (!TryGetShiftedValue(_jawHistory, JawShift + 1, out var jawCurrent) ||
		!TryGetShiftedValue(_teethHistory, TeethShift + 1, out var teethCurrent) ||
		!TryGetShiftedValue(_lipsHistory, LipsShift + 1, out var lipsCurrent) ||
		!TryGetShiftedValue(_jawHistory, JawShift + 2, out var jawPrevious) ||
		!TryGetShiftedValue(_teethHistory, TeethShift + 2, out var teethPrevious) ||
		!TryGetShiftedValue(_lipsHistory, LipsShift + 2, out var lipsPrevious))
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		if (Position > 0)
		{
			ManageLong(candle, teethCurrent, lipsCurrent);
		}
		else if (Position < 0)
		{
			ManageShort(candle, teethCurrent, lipsCurrent);
		}

		if (Position == 0 && IsFormedAndOnlineAndAllowTrading() && _previousClose is decimal prevClose)
		{
			var allowLong = EntryMode is AlligatorCrossModes.Both or AlligatorCrossModes.LongOnly;
			var allowShort = EntryMode is AlligatorCrossModes.Both or AlligatorCrossModes.ShortOnly;

			var alligatorUpperPrev = Math.Max(jawPrevious, Math.Max(teethPrevious, lipsPrevious));
			var alligatorLowerPrev = Math.Min(jawPrevious, Math.Min(teethPrevious, lipsPrevious));
			var alligatorUpperCurrent = Math.Max(jawCurrent, Math.Max(teethCurrent, lipsCurrent));
			var alligatorLowerCurrent = Math.Min(jawCurrent, Math.Min(teethCurrent, lipsCurrent));

			var crossUp = allowLong && prevClose <= alligatorLowerPrev &&
			candle.OpenPrice <= alligatorUpperCurrent && candle.ClosePrice >= alligatorUpperCurrent;

			var crossDown = allowShort && prevClose >= alligatorUpperPrev &&
			candle.OpenPrice >= alligatorLowerCurrent && candle.ClosePrice <= alligatorLowerCurrent;

			if (crossUp)
			{
				BuyMarket(volume: OrderVolume);
			}
			else if (crossDown)
			{
				SellMarket(volume: OrderVolume);
			}
		}

		_previousClose = candle.ClosePrice;
	}

	private void ManageLong(ICandleMessage candle, decimal teethCurrent, decimal lipsCurrent)
	{
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			TryCloseLong();
			return;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			TryCloseLong();
			return;
		}

		var exitThreshold = Math.Min(teethCurrent, lipsCurrent);
		if (candle.ClosePrice < exitThreshold)
		{
			TryCloseLong();
		}
	}

	private void ManageShort(ICandleMessage candle, decimal teethCurrent, decimal lipsCurrent)
	{
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			TryCloseShort();
			return;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			TryCloseShort();
			return;
		}

		var exitThreshold = Math.Max(teethCurrent, lipsCurrent);
		if (candle.ClosePrice > exitThreshold)
		{
			TryCloseShort();
		}
	}

	private void TryCloseLong()
	{
		if (_longExitRequested)
		return;

		_longExitRequested = true;
		SellMarket(volume: Position);
	}

	private void TryCloseShort()
	{
		if (_shortExitRequested)
		return;

		_shortExitRequested = true;
		BuyMarket(volume: Math.Abs(Position));
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var size = Math.Max(shift + 3, 3);
		return new decimal?[size];
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
		return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static bool TryGetShiftedValue(decimal?[] buffer, int offsetFromEnd, out decimal value)
	{
		value = 0m;

		if (buffer.Length < offsetFromEnd)
		return false;

		var index = buffer.Length - offsetFromEnd;
		if (index < 0)
		return false;

		if (buffer[index] is not decimal stored)
		return false;

		value = stored;
		return true;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}
}

/// <summary>
/// Selects the trade direction for the Alligator candle cross logic.
/// </summary>
public enum AlligatorCrossModes
{
	/// <summary>
	/// Enable both long and short entries.
	/// </summary>
	Both,

	/// <summary>
	/// Only take long signals.
	/// </summary>
	LongOnly,

	/// <summary>
	/// Only take short signals.
	/// </summary>
	ShortOnly
}

