using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sidus strategy combining Bill Williams Alligator slopes with an RSI midline cross.
/// Long entries require all Alligator lines to expand upward while RSI crosses above 50.
/// Short entries trigger when the Alligator slopes compress downward and RSI crosses below 50.
/// </summary>
public class SidusStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _offsetPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();

	private decimal? _prevRsi;
	private decimal? _prevPrevRsi;

	private decimal? _entryPrice;
	private decimal? _stopLoss;
	private decimal? _takeProfit;

	private decimal _pipSize;
	private decimal _offsetDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	/// <summary>
	/// Trade volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss offset in pips placed beyond the previous candle extreme.
	/// </summary>
	public int OffsetPips
	{
		get => _offsetPips.Value;
		set => _offsetPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Zero disables trailing.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional distance that price must cover before the trailing stop is advanced.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum required separation between Alligator line slopes.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Close opposite positions before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Period for the Alligator jaw (blue) line.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the Alligator jaw.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Period for the Alligator teeth (red) line.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the Alligator teeth.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Period for the Alligator lips (green) line.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the Alligator lips.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Period for the RSI oscillator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters with default values.
	/// </summary>
	public SidusStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trade volume", "Trading");

		_offsetPips = Param(nameof(OffsetPips), 3)
		.SetNotNegative()
		.SetDisplay("Offset", "Stop loss offset in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 75)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 15)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Minimum move before trailing advances", "Risk");

		_delta = Param(nameof(Delta), 0.00003m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Delta", "Minimum Alligator slope difference", "Filters");

		_closeOpposite = Param(nameof(CloseOpposite), false)
		.SetDisplay("Close Opposite", "Close opposite positions before entry", "Trading");

		_jawPeriod = Param(nameof(JawPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Alligator jaw period", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetNotNegative()
		.SetDisplay("Jaw Shift", "Alligator jaw forward shift", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Period", "Alligator teeth period", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetNotNegative()
		.SetDisplay("Teeth Shift", "Alligator teeth forward shift", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Period", "Alligator lips period", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetNotNegative()
		.SetDisplay("Lips Shift", "Alligator lips forward shift", "Alligator");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI averaging period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame", "General");
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
		_prevRsi = null;
		_prevPrevRsi = null;
		_entryPrice = null;
		_stopLoss = null;
		_takeProfit = null;
		_pipSize = 0m;
		_offsetDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
		{
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");
		}

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);

		UpdatePipParameters();

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
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (ApplyStops(candle))
		return;

		ApplyTrailing(candle);

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median, candle.OpenTime, true);
		var teethValue = _teeth.Process(median, candle.OpenTime, true);
		var lipsValue = _lips.Process(median, candle.OpenTime, true);
		var rsiValue = _rsi.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal || !rsiValue.IsFinal)
		return;

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		UpdateHistory(_jawHistory, jaw);
		UpdateHistory(_teethHistory, teeth);
		UpdateHistory(_lipsHistory, lips);

		var prevRsi = _prevRsi;
		var prevPrevRsi = _prevPrevRsi;

		var hasJaw = TryGetShiftedValue(_jawHistory, JawShift + 2, out var jawPrev) &&
		TryGetShiftedValue(_jawHistory, JawShift + 3, out var jawPrevPrev);
		var hasTeeth = TryGetShiftedValue(_teethHistory, TeethShift + 2, out var teethPrev) &&
		TryGetShiftedValue(_teethHistory, TeethShift + 3, out var teethPrevPrev);
		var hasLips = TryGetShiftedValue(_lipsHistory, LipsShift + 2, out var lipsPrev) &&
		TryGetShiftedValue(_lipsHistory, LipsShift + 3, out var lipsPrevPrev);

		if (hasJaw && hasTeeth && hasLips && prevRsi.HasValue && prevPrevRsi.HasValue)
		{
			var diffJaw = jawPrev - jawPrevPrev;
			var diffTeeth = teethPrev - teethPrevPrev;
			var diffLips = lipsPrev - lipsPrevPrev;

			var bullishCross = prevPrevRsi.Value < 50m && prevRsi.Value > 50m;
			var bearishCross = prevPrevRsi.Value > 50m && prevRsi.Value < 50m;

			if (bullishCross && diffJaw > Delta && diffTeeth > Delta && diffLips > Delta)
			TryEnterLong(candle);
			else if (bearishCross && diffJaw < Delta && diffTeeth < Delta && diffLips < Delta)
			TryEnterShort(candle);
		}

		_prevPrevRsi = prevRsi;
		_prevRsi = rsi;
	}

	/// <summary>
	/// Attempts to open a long position using the previous candle low for risk placement.
	/// </summary>
	/// <param name="candle">Finished candle message.</param>
	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position > 0)
		return;

		if (Position < 0 && !CloseOpposite)
		return;

		var stop = candle.LowPrice - _offsetDistance;
		if (_offsetDistance > 0m && stop >= candle.ClosePrice)
		return;

		var target = TakeProfitPips > 0 ? candle.ClosePrice + _takeProfitDistance : (decimal?)null;

		if (Position < 0)
		BuyMarket(Math.Abs(Position));

		BuyMarket(OrderVolume);

		_entryPrice = candle.ClosePrice;
		_stopLoss = _offsetDistance > 0m ? stop : (decimal?)null;
		_takeProfit = target;
	}

	/// <summary>
	/// Attempts to open a short position using the previous candle high for risk placement.
	/// </summary>
	/// <param name="candle">Finished candle message.</param>
	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position < 0)
		return;

		if (Position > 0 && !CloseOpposite)
		return;

		var stop = candle.HighPrice + _offsetDistance;
		if (_offsetDistance > 0m && stop <= candle.ClosePrice)
		return;

		var target = TakeProfitPips > 0 ? candle.ClosePrice - _takeProfitDistance : (decimal?)null;

		if (Position > 0)
		SellMarket(Math.Abs(Position));

		SellMarket(OrderVolume);

		_entryPrice = candle.ClosePrice;
		_stopLoss = _offsetDistance > 0m ? stop : (decimal?)null;
		_takeProfit = target;
	}

	/// <summary>
	/// Applies static stop-loss and take-profit exits.
	/// </summary>
	/// <param name="candle">Finished candle message.</param>
	/// <returns>True if a position was closed.</returns>
	private bool ApplyStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLoss.HasValue && candle.LowPrice <= _stopLoss.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeLevels();
				return true;
			}

			if (_takeProfit.HasValue && candle.HighPrice >= _takeProfit.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopLoss.HasValue && candle.HighPrice >= _stopLoss.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeLevels();
				return true;
			}

			if (_takeProfit.HasValue && candle.LowPrice <= _takeProfit.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeLevels();
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Moves the trailing stop closer when price travels far enough in the trade direction.
	/// </summary>
	/// <param name="candle">Finished candle message.</param>
	private void ApplyTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || _trailingStepDistance <= 0m || !_entryPrice.HasValue)
		return;

		if (Position > 0)
		{
			var gain = candle.HighPrice - _entryPrice.Value;
			if (gain > _trailingStopDistance + _trailingStepDistance)
			{
				var newStop = candle.HighPrice - _trailingStopDistance;
				var minAllowed = candle.HighPrice - (_trailingStopDistance + _trailingStepDistance);
				if (!_stopLoss.HasValue || _stopLoss.Value < minAllowed)
				_stopLoss = newStop;
			}
		}
		else if (Position < 0)
		{
			var gain = _entryPrice.Value - candle.LowPrice;
			if (gain > _trailingStopDistance + _trailingStepDistance)
			{
				var newStop = candle.LowPrice + _trailingStopDistance;
				var maxAllowed = candle.LowPrice + (_trailingStopDistance + _trailingStepDistance);
				if (!_stopLoss.HasValue || _stopLoss.Value > maxAllowed)
				_stopLoss = newStop;
			}
		}
	}

	/// <summary>
	/// Recomputes price distances expressed in pips using the security price step.
	/// </summary>
	private void UpdatePipParameters()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		var decimals = Security?.Decimals ?? 0;

		_pipSize = step;
		if (decimals == 3 || decimals == 5)
		_pipSize = step * 10m;

		_offsetDistance = OffsetPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;
	}

	/// <summary>
	/// Clears trade-related levels after a position is closed.
	/// </summary>
	private void ResetTradeLevels()
	{
		_entryPrice = null;
		_stopLoss = null;
		_takeProfit = null;
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var size = Math.Max(shift + 4, 4);
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
}
