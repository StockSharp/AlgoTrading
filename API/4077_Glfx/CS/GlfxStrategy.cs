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
/// Port of the MetaTrader 4 expert advisor "GLFX".
/// Uses higher timeframe RSI and moving average confirmations before opening trades.
/// Supports repeated signal confirmations and static stop-loss / take-profit levels.
/// </summary>
public class GlfxStrategy : Strategy
{
	private static readonly int[] TimeFrameMinutes = { 1, 5, 15, 30, 60, 240, 1440, 10080, 43200 };

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _higherTimeFrameShift;
	private readonly StrategyParam<bool> _useRsiSignal;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperThreshold;
	private readonly StrategyParam<decimal> _rsiLowerThreshold;
	private readonly StrategyParam<bool> _useMaSignal;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _signalsRepeat;
	private readonly StrategyParam<bool> _signalsReset;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;

	private RelativeStrengthIndex _higherRsi = null!;
	private SimpleMovingAverage _higherMa = null!;

	private decimal? _previousRsi;
	private decimal? _currentRsi;
	private decimal? _previousMa;
	private decimal? _currentMa;

	private int _buyConfirmations;
	private int _sellConfirmations;

	private DataType _resolvedHigherType;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="GlfxStrategy"/> class.
	/// </summary>
	public GlfxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe used for price actions.", "General");

		_higherTimeFrameShift = Param(nameof(HigherTimeFrameShift), 1)
			.SetDisplay("Higher TF Shift", "Number of MetaTrader timeframe steps used for confirmations.", "General");

		_useRsiSignal = Param(nameof(UseRsiSignal), true)
			.SetDisplay("Use RSI", "Enable higher timeframe RSI confirmation.", "Signals");

		_rsiPeriod = Param(nameof(RsiPeriod), 57)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of the higher timeframe RSI filter.", "Signals");

		_rsiUpperThreshold = Param(nameof(RsiUpperThreshold), 65m)
			.SetDisplay("RSI Upper", "Overbought threshold that blocks new longs.", "Signals");

		_rsiLowerThreshold = Param(nameof(RsiLowerThreshold), 25m)
			.SetDisplay("RSI Lower", "Oversold threshold that blocks new shorts.", "Signals");

		_useMaSignal = Param(nameof(UseMaSignal), true)
			.SetDisplay("Use MA", "Enable higher timeframe moving average confirmation.", "Signals");

		_maPeriod = Param(nameof(MaPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the higher timeframe moving average.", "Signals");

		_signalsRepeat = Param(nameof(SignalsRepeat), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signals Repeat", "Number of consecutive confirmations required before entering.", "Signals");

		_signalsReset = Param(nameof(SignalsReset), true)
			.SetDisplay("Signals Reset", "Reset counters whenever momentum weakens.", "Signals");

		_takeProfitPips = Param(nameof(TakeProfitPips), 308m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Static take-profit distance in pips.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 290m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Static stop-loss distance in pips.", "Risk");


		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Allow the strategy to open long trades.", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Allow the strategy to open short trades.", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Allow automatic closing of long positions.", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Allow automatic closing of short positions.", "Trading");
	}

	/// <summary>
	/// Working timeframe used for price evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of MetaTrader timeframe steps applied to build the confirmation timeframe.
	/// </summary>
	public int HigherTimeFrameShift
	{
		get => _higherTimeFrameShift.Value;
		set => _higherTimeFrameShift.Value = value;
	}

	/// <summary>
	/// Enables the higher timeframe RSI confirmation.
	/// </summary>
	public bool UseRsiSignal
	{
		get => _useRsiSignal.Value;
		set => _useRsiSignal.Value = value;
	}

	/// <summary>
	/// Period for the higher timeframe RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold that prevents new long trades.
	/// </summary>
	public decimal RsiUpperThreshold
	{
		get => _rsiUpperThreshold.Value;
		set => _rsiUpperThreshold.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold that prevents new short trades.
	/// </summary>
	public decimal RsiLowerThreshold
	{
		get => _rsiLowerThreshold.Value;
		set => _rsiLowerThreshold.Value = value;
	}

	/// <summary>
	/// Enables the higher timeframe moving average confirmation.
	/// </summary>
	public bool UseMaSignal
	{
		get => _useMaSignal.Value;
		set => _useMaSignal.Value = value;
	}

	/// <summary>
	/// Period for the higher timeframe moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of consecutive full-strength signals required before opening a trade.
	/// </summary>
	public int SignalsRepeat
	{
		get => _signalsRepeat.Value;
		set => _signalsRepeat.Value = value;
	}

	/// <summary>
	/// Reset confirmation counters when signal strength weakens.
	/// </summary>
	public bool SignalsReset
	{
		get => _signalsReset.Value;
		set => _signalsReset.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}


	/// <summary>
	/// Allow long entries generated by the confirmation logic.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow short entries generated by the confirmation logic.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow automatic closing of long positions when an opposite signal appears.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow automatic closing of short positions when an opposite signal appears.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		var higherType = ResolveHigherTimeFrame(CandleType, HigherTimeFrameShift);
		if (higherType != CandleType)
			yield return (Security, higherType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherRsi = null!;
		_higherMa = null!;
		_previousRsi = null;
		_currentRsi = null;
		_previousMa = null;
		_currentMa = null;
		_buyConfirmations = 0;
		_sellConfirmations = 0;
		_resolvedHigherType = default;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_resolvedHigherType = ResolveHigherTimeFrame(CandleType, HigherTimeFrameShift);

		_higherRsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_higherMa = new SimpleMovingAverage
		{
			Length = MaPeriod
		};

		_pipSize = CalculatePipSize();

		var takeProfit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;
		var stopLoss = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;

		if (takeProfit != null || stopLoss != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);

		var mainSubscription = SubscribeCandles(CandleType);

		if (_resolvedHigherType == CandleType)
		{
			mainSubscription.Bind(_higherRsi, _higherMa, ProcessCandleWithIndicators).Start();
		}
		else
		{
			mainSubscription.Bind(ProcessBaseCandle).Start();
			SubscribeCandles(_resolvedHigherType).Bind(_higherRsi, _higherMa, ProcessHigherCandle).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _higherMa);
			DrawIndicator(area, _higherRsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandleWithIndicators(ICandleMessage candle, decimal rsiValue, decimal maValue)
	{
		UpdateHigherValues(candle, rsiValue, maValue);
		ProcessBaseCandleInternal(candle);
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal rsiValue, decimal maValue)
	{
		UpdateHigherValues(candle, rsiValue, maValue);
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		ProcessBaseCandleInternal(candle);
	}

	private void UpdateHigherValues(ICandleMessage candle, decimal rsiValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousRsi = _currentRsi;
		_currentRsi = rsiValue;

		_previousMa = _currentMa;
		_currentMa = maValue;
	}

	private void ProcessBaseCandleInternal(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if ((UseRsiSignal && !_higherRsi.IsFormed) || (UseMaSignal && !_higherMa.IsFormed))
			return;

	var rsiSignal = 0;
		if (UseRsiSignal && _currentRsi.HasValue && _previousRsi.HasValue)
		{
			var current = _currentRsi.Value;
			var previous = _previousRsi.Value;

			if (current > previous && current < RsiUpperThreshold)
				rsiSignal = 1;
			else if (current < previous && current > RsiLowerThreshold)
				rsiSignal = -1;
		}

		var maSignal = 0;
		if (UseMaSignal && _currentMa.HasValue && _previousMa.HasValue)
		{
			var current = _currentMa.Value;
			var previous = _previousMa.Value;

			if (current > previous && current > candle.ClosePrice)
				maSignal = 1;
			else if (current < previous && current < candle.ClosePrice)
				maSignal = -1;
		}

		var signalsRequired = (UseRsiSignal ? 1 : 0) + (UseMaSignal ? 1 : 0);
		if (signalsRequired == 0)
			return;

		var combinedSignal = rsiSignal + maSignal;

		if (SignalsReset)
		{
			if ((combinedSignal < 0 && combinedSignal > -signalsRequired) || (combinedSignal >= 0 && combinedSignal < signalsRequired))
			{
				_buyConfirmations = 0;
				_sellConfirmations = 0;
			}
		}

		if (combinedSignal >= signalsRequired)
		{
			_buyConfirmations++;
			_sellConfirmations = 0;
		}
		else if (combinedSignal <= -signalsRequired)
		{
			_sellConfirmations++;
			_buyConfirmations = 0;
		}

		if (Position == 0m && !HasActiveOrders())
		{
			if (AllowLongEntry && _buyConfirmations >= SignalsRepeat)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					BuyMarket(volume);
					_buyConfirmations = 0;
					_sellConfirmations = 0;
				}
			}
			else if (AllowShortEntry && _sellConfirmations >= SignalsRepeat)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					SellMarket(volume);
					_buyConfirmations = 0;
					_sellConfirmations = 0;
				}
			}
		}
		else
		{
			if (Position > 0m && AllowLongExit && combinedSignal <= -signalsRequired)
			{
				ClosePosition();
				_buyConfirmations = 0;
				_sellConfirmations = 0;
			}
			else if (Position < 0m && AllowShortExit && combinedSignal >= signalsRequired)
			{
				ClosePosition();
				_buyConfirmations = 0;
				_sellConfirmations = 0;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (delta == 0m)
			return;

		_buyConfirmations = 0;
		_sellConfirmations = 0;
	}

	private DataType ResolveHigherTimeFrame(DataType baseType, int shift)
	{
		if (shift <= 0)
			return baseType;

		if (baseType.MessageType != typeof(TimeFrameCandleMessage) || baseType.Arg is not TimeSpan baseFrame)
			return baseType;

		var minutes = (int)Math.Round(baseFrame.TotalMinutes);
		var index = Array.IndexOf(TimeFrameMinutes, minutes);

		if (index >= 0)
		{
			var newIndex = Math.Min(TimeFrameMinutes.Length - 1, index + shift);
			var resolvedMinutes = TimeFrameMinutes[newIndex];
			return TimeSpan.FromMinutes(resolvedMinutes).TimeFrame();
		}

		var multiplier = Math.Max(1, shift + 1);
		var resolved = TimeSpan.FromTicks(baseFrame.Ticks * multiplier);
		return resolved.TimeFrame();
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 1m;

		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}
}
