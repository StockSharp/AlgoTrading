using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following system converted from the MetaTrader expert advisor "SniperJawEA.mq4".
/// The strategy aligns the Alligator jaw, teeth, and lips smoothed moving averages on the median price.
/// A long signal appears when all three lines stack upward and each line rises compared with the previous candle.
/// A short signal requires the inverse stacking and downward slope. Optional settings mirror the original EA: pip-based
/// stop-loss and take-profit distances plus an "entry-to-exit" switch that liquidates the opposite position before opening a new trade.
/// </summary>
public class SniperJawStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _enableTrading;
	private readonly StrategyParam<bool> _useEntryToExit;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _minimumBars;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<DataType> _candleType;

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
	private int _finishedCandles;
	private DateTimeOffset? _lastSignalTime;

	/// <summary>
	/// Initializes <see cref="SniperJawStrategy"/> parameters.
	/// </summary>
	public SniperJawStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade size in lots or contracts", "Trading");

		_enableTrading = Param(nameof(EnableTrading), true)
			.SetDisplay("Enable Trading", "Master switch for signal execution", "Trading");

		_useEntryToExit = Param(nameof(UseEntryToExit), true)
			.SetDisplay("Use Entry To Exit", "Close opposite exposure before opening a new trade", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance converted with the price step", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Optional profit target distance; zero disables it", "Risk");

		_minimumBars = Param(nameof(MinimumBars), 60)
			.SetGreaterOrEqual(1)
			.SetDisplay("Minimum Bars", "Required number of finished candles before trading", "Filters");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Period", "Smoothed moving average length for the jaw line", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
			.SetNotNegative()
			.SetDisplay("Jaw Shift", "Forward shift applied to jaw readings", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Smoothed moving average length for the teeth line", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
			.SetNotNegative()
			.SetDisplay("Teeth Shift", "Forward shift applied to teeth readings", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Period", "Smoothed moving average length for the lips line", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetNotNegative()
			.SetDisplay("Lips Shift", "Forward shift applied to lips readings", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used for signals", "Data");
	}

	/// <summary>
	/// Trade volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Master switch for enabling or disabling signal execution.
	/// </summary>
	public bool EnableTrading
	{
		get => _enableTrading.Value;
		set => _enableTrading.Value = value;
	}

	/// <summary>
	/// Close the opposite position before opening a new trade when a fresh signal arrives.
	/// </summary>
	public bool UseEntryToExit
	{
		get => _useEntryToExit.Value;
		set => _useEntryToExit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips; zero disables the protective stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips; zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimum number of finished candles required before the system evaluates signals.
	/// </summary>
	public int MinimumBars
	{
		get => _minimumBars.Value;
		set => _minimumBars.Value = value;
	}

	/// <summary>
	/// Length of the jaw smoothed moving average.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to jaw readings when aligning them with candles.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Length of the teeth smoothed moving average.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to teeth readings when aligning them with candles.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Length of the lips smoothed moving average.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to lips readings when aligning them with candles.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Candle type used for the primary signal series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_finishedCandles = 0;
		_lastSignalTime = null;
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

		_pipSize = CalculatePipSize();
		_finishedCandles = 0;
		_lastSignalTime = null;

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
			_shortExitRequested = false;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : (decimal?)null;
			_shortTakePrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : (decimal?)null;
			_shortExitRequested = false;
			_longExitRequested = false;
			_longStopPrice = null;
			_longTakePrice = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_finishedCandles++;

		if (Position > 0)
		{
			ManageLong(candle);
		}
		else if (Position < 0)
		{
			ManageShort(candle);
		}

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

		if (_finishedCandles < MinimumBars)
			return;

		if (!TryGetShiftedValue(_jawHistory, JawShift + 1, out var jawCurrent) ||
				!TryGetShiftedValue(_teethHistory, TeethShift + 1, out var teethCurrent) ||
				!TryGetShiftedValue(_lipsHistory, LipsShift + 1, out var lipsCurrent) ||
				!TryGetShiftedValue(_jawHistory, JawShift + 2, out var jawPrevious) ||
				!TryGetShiftedValue(_teethHistory, TeethShift + 2, out var teethPrevious) ||
				!TryGetShiftedValue(_lipsHistory, LipsShift + 2, out var lipsPrevious))
		{
			return;
		}

		var isUptrend = jawCurrent < teethCurrent && teethCurrent < lipsCurrent &&
			jawCurrent > jawPrevious && teethCurrent > teethPrevious && lipsCurrent > lipsPrevious;

		var isDowntrend = jawCurrent > teethCurrent && teethCurrent > lipsCurrent &&
			jawCurrent < jawPrevious && teethCurrent < teethPrevious && lipsCurrent < lipsPrevious;

		if (!EnableTrading)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (isUptrend)
		{
			if (Position < 0 && UseEntryToExit)
			{
				RequestShortExit();
				return;
			}

			if (Position != 0)
				return;

			if (_lastSignalTime == candle.OpenTime)
				return;

			BuyMarket(volume: OrderVolume);
			_lastSignalTime = candle.OpenTime;
		}
		else if (isDowntrend)
		{
			if (Position > 0 && UseEntryToExit)
			{
				RequestLongExit();
				return;
			}

			if (Position != 0)
				return;

			if (_lastSignalTime == candle.OpenTime)
				return;

			SellMarket(volume: OrderVolume);
			_lastSignalTime = candle.OpenTime;
		}
	}

	private void ManageLong(ICandleMessage candle)
	{
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			RequestLongExit();
			return;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			RequestLongExit();
		}
	}

	private void ManageShort(ICandleMessage candle)
	{
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			RequestShortExit();
			return;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			RequestShortExit();
		}
	}

	private void RequestLongExit()
	{
		if (_longExitRequested || Position <= 0)
			return;

		_longExitRequested = true;
		SellMarket(volume: Position);
	}

	private void RequestShortExit()
	{
		if (_shortExitRequested || Position >= 0)
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
