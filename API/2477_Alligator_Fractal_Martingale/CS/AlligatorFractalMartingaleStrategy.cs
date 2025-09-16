using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the classic Alligator + Fractals expert advisor with martingale and trailing stop management.
/// The strategy opens trades when the Alligator "mouth" widens in the signal direction and an optional
/// fractal breakout filter confirms momentum. After the initial market order, a configurable martingale
/// ladder can average into adverse moves using limit-style levels. Protective stop-loss, take-profit and
/// trailing updates are managed inside the strategy rather than relying on exchange-native orders.
/// </summary>
public class AlligatorFractalMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<decimal> _entrySpread;
	private readonly StrategyParam<decimal> _exitSpread;
	private readonly StrategyParam<bool> _useAlligatorEntry;
	private readonly StrategyParam<bool> _useFractalFilter;
	private readonly StrategyParam<bool> _useAlligatorExit;
	private readonly StrategyParam<bool> _allowMultipleEntries;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _manualMode;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<int> _fractalLookback;
	private readonly StrategyParam<decimal> _fractalBuffer;
	private readonly StrategyParam<int> _martingaleSteps;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _martingaleStepDistance;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private readonly List<decimal> _jawHistory = new();
	private readonly List<decimal> _teethHistory = new();
	private readonly List<decimal> _lipsHistory = new();

	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();

	private readonly List<(int Index, decimal Value)> _upFractals = new();
	private readonly List<(int Index, decimal Value)> _downFractals = new();

	private readonly List<MartingaleLevel> _longMartingaleLevels = new();
	private readonly List<MartingaleLevel> _shortMartingaleLevels = new();

	private bool _currentBuyState = true;
	private bool _currentSellState = true;
	private bool _prevBuyState = true;
	private bool _prevSellState = true;

	private decimal? _activeUpFractal;
	private decimal? _activeDownFractal;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private int _finishedBarIndex = -1;
	private int _historyOffset;
	private int _maxAlligatorBuffer;

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order volume used for the initial entry.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Length of the jaw smoothed moving average.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Forward shift of the jaw line in bars.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Length of the teeth smoothed moving average.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Forward shift of the teeth line in bars.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Length of the lips smoothed moving average.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// Forward shift of the lips line in bars.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Minimum spread between lips and jaw required to enable long entries.
	/// </summary>
	public decimal EntrySpread
	{
		get => _entrySpread.Value;
		set => _entrySpread.Value = value;
	}

	/// <summary>
	/// Spread threshold that closes the Alligator mouth and disables entries.
	/// </summary>
	public decimal ExitSpread
	{
		get => _exitSpread.Value;
		set => _exitSpread.Value = value;
	}

	/// <summary>
	/// Enable Alligator based entry triggers.
	/// </summary>
	public bool UseAlligatorEntry
	{
		get => _useAlligatorEntry.Value;
		set => _useAlligatorEntry.Value = value;
	}

	/// <summary>
	/// Require fractal breakout confirmation before opening a position.
	/// </summary>
	public bool UseFractalFilter
	{
		get => _useFractalFilter.Value;
		set => _useFractalFilter.Value = value;
	}

	/// <summary>
	/// Close positions when the Alligator mouth closes.
	/// </summary>
	public bool UseAlligatorExit
	{
		get => _useAlligatorExit.Value;
		set => _useAlligatorExit.Value = value;
	}

	/// <summary>
	/// Allow multiple market entries in the same direction.
	/// </summary>
	public bool AllowMultipleEntries
	{
		get => _allowMultipleEntries.Value;
		set => _allowMultipleEntries.Value = value;
	}

	/// <summary>
	/// Enable the martingale averaging ladder.
	/// </summary>
	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	/// <summary>
	/// Enable trailing stop updates.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Disable automatic entries when true.
	/// </summary>
	public bool ManualMode
	{
		get => _manualMode.Value;
		set => _manualMode.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Minimum step that price must travel before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Number of bars to keep fractal levels active.
	/// </summary>
	public int FractalLookback
	{
		get => _fractalLookback.Value;
		set => _fractalLookback.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and a fractal for validation.
	/// </summary>
	public decimal FractalBuffer
	{
		get => _fractalBuffer.Value;
		set => _fractalBuffer.Value = value;
	}

	/// <summary>
	/// Number of martingale averaging levels.
	/// </summary>
	public int MartingaleSteps
	{
		get => _martingaleSteps.Value;
		set => _martingaleSteps.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume on each martingale level.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Distance between martingale levels in price units.
	/// </summary>
	public decimal MartingaleStepDistance
	{
		get => _martingaleStepDistance.Value;
		set => _martingaleStepDistance.Value = value;
	}

	/// <summary>
	/// Maximum volume allowed per order.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Create <see cref="AlligatorFractalMartingaleStrategy"/>.
	/// </summary>
	public AlligatorFractalMartingaleStrategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Length", "SMMA length for the jaw", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Jaw Shift", "Forward shift of the jaw", "Alligator");

		_teethLength = Param(nameof(TeethLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Length", "SMMA length for the teeth", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Teeth Shift", "Forward shift of the teeth", "Alligator");

		_lipsLength = Param(nameof(LipsLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Length", "SMMA length for the lips", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Lips Shift", "Forward shift of the lips", "Alligator");

		_entrySpread = Param(nameof(EntrySpread), 0.0005m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Entry Spread", "Required jaw-lips spread to enable entries", "Alligator");

		_exitSpread = Param(nameof(ExitSpread), 0.0001m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Exit Spread", "Spread that closes the mouth", "Alligator");

		_useAlligatorEntry = Param(nameof(UseAlligatorEntry), true)
		.SetDisplay("Use Alligator Entry", "Trigger trades on jaw/lips widening", "Logic");

		_useFractalFilter = Param(nameof(UseFractalFilter), false)
		.SetDisplay("Use Fractal Filter", "Require fractal breakout confirmation", "Logic");

		_useAlligatorExit = Param(nameof(UseAlligatorExit), false)
		.SetDisplay("Use Alligator Exit", "Close positions when mouth closes", "Logic");

		_allowMultipleEntries = Param(nameof(AllowMultipleEntries), false)
		.SetDisplay("Allow Multiple Entries", "Permit repeated market entries", "Trading");

		_enableMartingale = Param(nameof(EnableMartingale), true)
		.SetDisplay("Enable Martingale", "Build averaging ladder after entry", "Trading");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Move stop when price advances", "Protection");

		_manualMode = Param(nameof(ManualMode), false)
		.SetDisplay("Manual Mode", "Disable automatic entries", "Trading");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 0.008m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Take Profit Distance", "Fixed distance for profit taking", "Protection");

		_stopLossDistance = Param(nameof(StopLossDistance), 0.008m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Stop Loss Distance", "Fixed distance for protective stop", "Protection");

		_trailingStep = Param(nameof(TrailingStep), 0.001m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Trailing Step", "Minimum move before trailing", "Protection");

		_fractalLookback = Param(nameof(FractalLookback), 10)
		.SetGreaterThanOrEqual(1)
		.SetDisplay("Fractal Lookback", "Bars to keep fractal levels", "Fractals");

		_fractalBuffer = Param(nameof(FractalBuffer), 0.003m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Fractal Buffer", "Extra distance to validate fractals", "Fractals");

		_martingaleSteps = Param(nameof(MartingaleSteps), 10)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Martingale Steps", "Number of averaging levels", "Martingale");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.3m)
		.SetGreaterThan(0m)
		.SetDisplay("Martingale Multiplier", "Volume multiplier per level", "Martingale");

		_martingaleStepDistance = Param(nameof(MartingaleStepDistance), 0.005m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Martingale Step", "Distance between averaging levels", "Martingale");

		_maxVolume = Param(nameof(MaxVolume), 0.5m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Max Volume", "Absolute cap for any order", "Trading");

		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThan(0m)
		.SetDisplay("Volume", "Base volume for entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Source candles", "General");
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

		_jawHistory.Clear();
		_teethHistory.Clear();
		_lipsHistory.Clear();
		_highHistory.Clear();
		_lowHistory.Clear();
		_upFractals.Clear();
		_downFractals.Clear();
		_longMartingaleLevels.Clear();
		_shortMartingaleLevels.Clear();
		_currentBuyState = true;
		_currentSellState = true;
		_prevBuyState = true;
		_prevSellState = true;
		_activeUpFractal = null;
		_activeDownFractal = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_finishedBarIndex = -1;
		_historyOffset = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawLength };
		_teeth = new SmoothedMovingAverage { Length = TeethLength };
		_lips = new SmoothedMovingAverage { Length = LipsLength };

		_maxAlligatorBuffer = Math.Max(Math.Max(JawShift, TeethShift), LipsShift) + 10;

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

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
		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median, candle.ServerTime, candle.State == CandleStates.Finished);
		if (jawValue.IsFinal)
		AddIndicatorValue(_jawHistory, jawValue.ToDecimal());

		var teethValue = _teeth.Process(median, candle.ServerTime, candle.State == CandleStates.Finished);
		if (teethValue.IsFinal)
		AddIndicatorValue(_teethHistory, teethValue.ToDecimal());

		var lipsValue = _lips.Process(median, candle.ServerTime, candle.State == CandleStates.Finished);
		if (lipsValue.IsFinal)
		AddIndicatorValue(_lipsHistory, lipsValue.ToDecimal());

		if (candle.State != CandleStates.Finished)
		return;

		_finishedBarIndex++;

		UpdateAlligatorStates();
		UpdateFractals(candle);
		UpdateTrailingAndStops(candle);
		ProcessMartingaleLevels(candle);

		if (Position == 0)
		{
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!ManualMode)
		{
			TryOpenPositions(candle);
		}

		TryClosePositionsOnAlligator(candle);
	}

	private void TryOpenPositions(ICandleMessage candle)
	{
		var allowLong = !UseFractalFilter || _activeUpFractal.HasValue;
		var allowShort = !UseFractalFilter || _activeDownFractal.HasValue;

		var longSignal = !UseAlligatorEntry || (_currentBuyState && !_prevBuyState);
		var shortSignal = !UseAlligatorEntry || (_currentSellState && !_prevSellState);

		var initialVolume = GetInitialVolume();
		if (initialVolume <= 0m)
		return;

		if (longSignal && allowLong)
		{
			var canAdd = AllowMultipleEntries || Position <= 0;
			if (canAdd)
			OpenLong(candle.ClosePrice, initialVolume);
		}

		if (shortSignal && allowShort)
		{
			var canAdd = AllowMultipleEntries || Position >= 0;
			if (canAdd)
			OpenShort(candle.ClosePrice, initialVolume);
		}
	}

	private void TryClosePositionsOnAlligator(ICandleMessage candle)
	{
		if (!UseAlligatorExit)
		return;

		if (_prevBuyState && !_currentBuyState && Position > 0)
		{
			SellMarket(Position);
			ClearLongState();
		}

		if (_prevSellState && !_currentSellState && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ClearShortState();
		}
	}

	private void UpdateAlligatorStates()
	{
		_prevBuyState = _currentBuyState;
		_prevSellState = _currentSellState;

		var jaw = GetShiftedValue(_jawHistory, JawShift);
		var teeth = GetShiftedValue(_teethHistory, TeethShift);
		var lips = GetShiftedValue(_lipsHistory, LipsShift);

		if (jaw is null || teeth is null || lips is null)
		return;

		var jawValue = jaw.Value;
		var teethValue = teeth.Value;
		var lipsValue = lips.Value;

		if (lipsValue > jawValue + EntrySpread)
		_currentBuyState = true;

		if (lipsValue + ExitSpread < teethValue)
		_currentBuyState = false;

		if (jawValue > lipsValue + EntrySpread)
		_currentSellState = true;

		if (jawValue + ExitSpread < teethValue)
		_currentSellState = false;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);

		var maxHistory = Math.Max(FractalLookback + 10, 10);
		while (_highHistory.Count > maxHistory)
		{
			_highHistory.RemoveAt(0);
			_lowHistory.RemoveAt(0);
			_historyOffset++;
		}

		var count = _highHistory.Count;
		if (count >= 5)
		{
			var center = count - 3;

			var h2 = _highHistory[center];
			var h1 = _highHistory[center - 1];
			var h0 = _highHistory[center - 2];
			var h3 = _highHistory[center + 1];
			var h4 = _highHistory[center + 2];

			if (h2 > h0 && h2 > h1 && h2 > h3 && h2 > h4)
			{
				_upFractals.Add((_historyOffset + center, h2));
			}

			var l2 = _lowHistory[center];
			var l1 = _lowHistory[center - 1];
			var l0 = _lowHistory[center - 2];
			var l3 = _lowHistory[center + 1];
			var l4 = _lowHistory[center + 2];

			if (l2 < l0 && l2 < l1 && l2 < l3 && l2 < l4)
			{
				_downFractals.Add((_historyOffset + center, l2));
			}
		}

		var lookback = FractalLookback;

		for (var i = _upFractals.Count - 1; i >= 0; i--)
		{
			if (_finishedBarIndex - _upFractals[i].Index > lookback)
			_upFractals.RemoveAt(i);
		}

		for (var i = _downFractals.Count - 1; i >= 0; i--)
		{
			if (_finishedBarIndex - _downFractals[i].Index > lookback)
			_downFractals.RemoveAt(i);
		}

		_activeUpFractal = null;
		for (var i = 0; i < _upFractals.Count; i++)
		{
			var value = _upFractals[i].Value;
			if (value >= candle.ClosePrice + FractalBuffer)
			{
				if (_activeUpFractal is null || value > _activeUpFractal.Value)
				_activeUpFractal = value;
			}
		}

		_activeDownFractal = null;
		for (var i = 0; i < _downFractals.Count; i++)
		{
			var value = _downFractals[i].Value;
			if (value <= candle.ClosePrice - FractalBuffer)
			{
				if (_activeDownFractal is null || value < _activeDownFractal.Value)
				_activeDownFractal = value;
			}
		}
	}

	private void UpdateTrailingAndStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (StopLossDistance > 0m)
			{
				var desired = candle.ClosePrice - StopLossDistance;
				if (_longStop is null)
				{
					_longStop = desired;
				}
				else if (EnableTrailing && desired > _longStop.Value + TrailingStep)
				{
					_longStop = desired;
				}
			}

			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ClearLongState();
				return;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ClearLongState();
			}
		}
		else if (Position < 0)
		{
			var shortVolume = Math.Abs(Position);

			if (StopLossDistance > 0m)
			{
				var desired = candle.ClosePrice + StopLossDistance;
				if (_shortStop is null)
				{
					_shortStop = desired;
				}
				else if (EnableTrailing && desired < _shortStop.Value - TrailingStep)
				{
					_shortStop = desired;
				}
			}

			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(shortVolume);
				ClearShortState();
				return;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(shortVolume);
				ClearShortState();
			}
		}
	}

	private void ProcessMartingaleLevels(ICandleMessage candle)
	{
		if (!EnableMartingale)
		return;

		if (Position >= 0)
		{
			for (var i = 0; i < _longMartingaleLevels.Count; i++)
			{
				var level = _longMartingaleLevels[i];
				if (level.Executed)
				continue;

				if (candle.LowPrice <= level.Price)
				{
					var volume = RoundVolume(level.Volume);
					if (volume <= 0m)
					{
						level.Executed = true;
						continue;
					}

					if (Position < 0)
					BuyMarket(Math.Abs(Position));

					BuyMarket(volume);
					level.Executed = true;

					if (StopLossDistance > 0m)
					{
						var desired = candle.ClosePrice - StopLossDistance;
						_longStop = _longStop is decimal stop && stop < desired ? stop : desired;
					}
				}
			}

			_longMartingaleLevels.RemoveAll(l => l.Executed);
		}

		if (Position <= 0)
		{
			for (var i = 0; i < _shortMartingaleLevels.Count; i++)
			{
				var level = _shortMartingaleLevels[i];
				if (level.Executed)
				continue;

				if (candle.HighPrice >= level.Price)
				{
					var volume = RoundVolume(level.Volume);
					if (volume <= 0m)
					{
						level.Executed = true;
						continue;
					}

					if (Position > 0)
					SellMarket(Position);

					SellMarket(volume);
					level.Executed = true;

					if (StopLossDistance > 0m)
					{
						var desired = candle.ClosePrice + StopLossDistance;
						_shortStop = _shortStop is decimal stop && stop > desired ? stop : desired;
					}
				}
			}

			_shortMartingaleLevels.RemoveAll(l => l.Executed);
		}
	}

	private void OpenLong(decimal entryPrice, decimal volume)
	{
		volume = RoundVolume(volume);
		if (volume <= 0m)
		return;

		if (Position < 0)
		BuyMarket(Math.Abs(Position));

		BuyMarket(volume);

		_longStop = StopLossDistance > 0m ? entryPrice - StopLossDistance : null;
		_longTake = TakeProfitDistance > 0m ? entryPrice + TakeProfitDistance : null;
		_shortStop = null;
		_shortTake = null;

		if (EnableMartingale)
		BuildMartingaleLevels(true, entryPrice, volume);
		else
		_longMartingaleLevels.Clear();

		_shortMartingaleLevels.Clear();
	}

	private void OpenShort(decimal entryPrice, decimal volume)
	{
		volume = RoundVolume(volume);
		if (volume <= 0m)
		return;

		if (Position > 0)
		SellMarket(Position);

		SellMarket(volume);

		_shortStop = StopLossDistance > 0m ? entryPrice + StopLossDistance : null;
		_shortTake = TakeProfitDistance > 0m ? entryPrice - TakeProfitDistance : null;
		_longStop = null;
		_longTake = null;

		if (EnableMartingale)
		BuildMartingaleLevels(false, entryPrice, volume);
		else
		_shortMartingaleLevels.Clear();

		_longMartingaleLevels.Clear();
	}

	private void ClearLongState()
	{
		_longStop = null;
		_longTake = null;
		_longMartingaleLevels.Clear();
	}

	private void ClearShortState()
	{
		_shortStop = null;
		_shortTake = null;
		_shortMartingaleLevels.Clear();
	}

	private void BuildMartingaleLevels(bool isLong, decimal entryPrice, decimal baseVolume)
	{
		var targetList = isLong ? _longMartingaleLevels : _shortMartingaleLevels;
		targetList.Clear();

		var volume = baseVolume;

		for (var i = 1; i <= MartingaleSteps; i++)
		{
			volume *= MartingaleMultiplier;
			volume = Math.Min(volume, MaxVolume);

			var roundedVolume = RoundVolume(volume);
			if (roundedVolume <= 0m)
			break;

			var distance = MartingaleStepDistance * i;
			if (distance <= 0m)
			break;

			var price = isLong ? entryPrice - distance : entryPrice + distance;

			targetList.Add(new MartingaleLevel
			{
				Price = price,
				Volume = roundedVolume
			});
		}
	}

	private decimal GetInitialVolume()
	{
		var volume = Volume;
		if (MaxVolume > 0m && volume > MaxVolume)
		volume = MaxVolume;

		return RoundVolume(volume);
	}

	private void AddIndicatorValue(List<decimal> list, decimal value)
	{
		list.Add(value);
		if (list.Count > _maxAlligatorBuffer)
		list.RemoveAt(0);
	}

	private static decimal? GetShiftedValue(List<decimal> list, int shift)
	{
		if (shift < 0)
		return null;

		var index = list.Count - 1 - shift;
		if (index < 0 || index >= list.Count)
		return null;

		return list[index];
	}

	private decimal RoundVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (volume < 0m)
		volume = 0m;

		if (MaxVolume > 0m && volume > MaxVolume)
		volume = MaxVolume;

		return volume;
	}

	private sealed class MartingaleLevel
	{
		public decimal Price { get; set; }
		public decimal Volume { get; set; }
		public bool Executed { get; set; }
	}
}
