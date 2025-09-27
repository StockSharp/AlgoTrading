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
/// No Nonsense Forex style tester that combines a baseline, two confirmations, a volume filter and exit rules.
/// The strategy uses an EMA baseline, RSI and CCI confirmations, ATR based volatility filtering and an RSI exit overlay.
/// Position management is performed with ATR based protective levels and optional trailing stop logic.
/// </summary>
public class NoNonsenseTesterStrategy : Strategy
{
	private readonly StrategyParam<int> _baselineLength;
	private readonly StrategyParam<int> _confirmationRsiLength;
	private readonly StrategyParam<decimal> _confirmationRsiThreshold;
	private readonly StrategyParam<int> _confirmationCciLength;
	private readonly StrategyParam<decimal> _confirmationCciThreshold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrEntryMultiplier;
	private readonly StrategyParam<decimal> _atrTakeProfitMultiplier;
	private readonly StrategyParam<decimal> _atrStopLossMultiplier;
	private readonly StrategyParam<decimal> _atrTrailingMultiplier;
	private readonly StrategyParam<decimal> _atrMinimum;
	private readonly StrategyParam<int> _exitRsiLength;
	private readonly StrategyParam<decimal> _exitRsiUpperLevel;
	private readonly StrategyParam<decimal> _exitRsiLowerLevel;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _baseline;
	private RelativeStrengthIndex _confirmationRsi;
	private CommodityChannelIndex _confirmationCci;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _exitRsi;

	private decimal _previousBaselineValue;
	private decimal _previousClosePrice;
	private bool _hasBaselineHistory;
	private decimal _lastAtrValue;
	private decimal _lastExitRsiValue;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Length for the EMA baseline.
	/// </summary>
	public int BaselineLength
	{
		get => _baselineLength.Value;
		set => _baselineLength.Value = value;
	}

	/// <summary>
	/// Length for the RSI confirmation indicator.
	/// </summary>
	public int ConfirmationRsiLength
	{
		get => _confirmationRsiLength.Value;
		set => _confirmationRsiLength.Value = value;
	}

	/// <summary>
	/// RSI level that defines bullish confirmation above and bearish confirmation below.
	/// </summary>
	public decimal ConfirmationRsiThreshold
	{
		get => _confirmationRsiThreshold.Value;
		set => _confirmationRsiThreshold.Value = value;
	}

	/// <summary>
	/// Length for the CCI confirmation indicator.
	/// </summary>
	public int ConfirmationCciLength
	{
		get => _confirmationCciLength.Value;
		set => _confirmationCciLength.Value = value;
	}

	/// <summary>
	/// Absolute CCI threshold used to filter weak signals.
	/// </summary>
	public decimal ConfirmationCciThreshold
	{
		get => _confirmationCciThreshold.Value;
		set => _confirmationCciThreshold.Value = value;
	}

	/// <summary>
	/// Period of the ATR volatility filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR when calculating risk based position sizing.
	/// </summary>
	public decimal AtrEntryMultiplier
	{
		get => _atrEntryMultiplier.Value;
		set => _atrEntryMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR used to place the take profit level.
	/// </summary>
	public decimal AtrTakeProfitMultiplier
	{
		get => _atrTakeProfitMultiplier.Value;
		set => _atrTakeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR used to place the stop loss level.
	/// </summary>
	public decimal AtrStopLossMultiplier
	{
		get => _atrStopLossMultiplier.Value;
		set => _atrStopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR used by the trailing stop engine.
	/// </summary>
	public decimal AtrTrailingMultiplier
	{
		get => _atrTrailingMultiplier.Value;
		set => _atrTrailingMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum ATR value required to allow new trades.
	/// </summary>
	public decimal AtrMinimum
	{
		get => _atrMinimum.Value;
		set => _atrMinimum.Value = value;
	}

	/// <summary>
	/// RSI period that controls exit confirmation.
	/// </summary>
	public int ExitRsiLength
	{
		get => _exitRsiLength.Value;
		set => _exitRsiLength.Value = value;
	}

	/// <summary>
	/// Upper RSI level that triggers short exits and blocks new longs.
	/// </summary>
	public decimal ExitRsiUpperLevel
	{
		get => _exitRsiUpperLevel.Value;
		set => _exitRsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI level that triggers long exits and blocks new shorts.
	/// </summary>
	public decimal ExitRsiLowerLevel
	{
		get => _exitRsiLowerLevel.Value;
		set => _exitRsiLowerLevel.Value = value;
	}

	/// <summary>
	/// The candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NoNonsenseTesterStrategy"/> class.
	/// </summary>
	public NoNonsenseTesterStrategy()
	{
		_baselineLength = Param(nameof(BaselineLength), 34)
		.SetGreaterThanZero()
		.SetDisplay("Baseline Length", "Period for the EMA baseline", "Baseline")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 2);

		_confirmationRsiLength = Param(nameof(ConfirmationRsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Confirmation RSI Length", "Period for the RSI confirmation", "Confirmations")
		.SetCanOptimize(true)
		.SetOptimize(8, 28, 2);

		_confirmationRsiThreshold = Param(nameof(ConfirmationRsiThreshold), 50m)
		.SetDisplay("RSI Threshold", "RSI value that separates bullish and bearish signals", "Confirmations")
		.SetCanOptimize(true)
		.SetOptimize(45m, 55m, 1m);

		_confirmationCciLength = Param(nameof(ConfirmationCciLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Confirmation CCI Length", "Period for the CCI confirmation", "Confirmations")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_confirmationCciThreshold = Param(nameof(ConfirmationCciThreshold), 0m)
		.SetDisplay("CCI Threshold", "Absolute CCI value that must be exceeded", "Confirmations")
		.SetCanOptimize(true)
		.SetOptimize(0m, 50m, 5m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR period for volatility checks", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_atrEntryMultiplier = Param(nameof(AtrEntryMultiplier), 1m)
		.SetDisplay("ATR Entry Multiplier", "Multiplier that scales entry size based on ATR", "Volatility")
		.SetOptimize(0.5m, 2m, 0.1m);

		_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 2m)
		.SetDisplay("ATR Take Profit", "ATR multiple for take profit", "Risk")
		.SetOptimize(1m, 4m, 0.5m);

		_atrStopLossMultiplier = Param(nameof(AtrStopLossMultiplier), 1.5m)
		.SetDisplay("ATR Stop Loss", "ATR multiple for stop loss", "Risk")
		.SetOptimize(1m, 4m, 0.5m);

		_atrTrailingMultiplier = Param(nameof(AtrTrailingMultiplier), 0m)
		.SetDisplay("ATR Trailing", "ATR multiple for trailing stop (0 disables)", "Risk")
		.SetOptimize(0m, 3m, 0.5m);

		_atrMinimum = Param(nameof(AtrMinimum), 0m)
		.SetDisplay("ATR Minimum", "Minimum ATR value required for entries", "Volatility")
		.SetOptimize(0m, 1m, 0.1m);

		_exitRsiLength = Param(nameof(ExitRsiLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Exit RSI Length", "Period for the exit RSI", "Exit")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_exitRsiUpperLevel = Param(nameof(ExitRsiUpperLevel), 60m)
		.SetDisplay("Exit RSI Upper", "RSI level that signals short exit", "Exit")
		.SetOptimize(55m, 75m, 1m);

		_exitRsiLowerLevel = Param(nameof(ExitRsiLowerLevel), 40m)
		.SetDisplay("Exit RSI Lower", "RSI level that signals long exit", "Exit")
		.SetOptimize(25m, 45m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for the calculations", "General");
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

		_hasBaselineHistory = false;
		_previousBaselineValue = 0m;
		_previousClosePrice = 0m;
		_lastAtrValue = 0m;
		_lastExitRsiValue = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_baseline = new EMA { Length = BaselineLength };
		_confirmationRsi = new RelativeStrengthIndex { Length = ConfirmationRsiLength };
		_confirmationCci = new CommodityChannelIndex { Length = ConfirmationCciLength };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_exitRsi = new RelativeStrengthIndex { Length = ExitRsiLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_baseline, _confirmationRsi, _confirmationCci, _atr, _exitRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _baseline);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal baselineValue, decimal confirmationRsiValue, decimal confirmationCciValue, decimal atrValue, decimal exitRsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_baseline.IsFormed || !_confirmationRsi.IsFormed || !_confirmationCci.IsFormed || !_atr.IsFormed || !_exitRsi.IsFormed)
		return;

		_lastAtrValue = atrValue;
		_lastExitRsiValue = exitRsiValue;

		if (!_hasBaselineHistory)
		{
		_previousBaselineValue = baselineValue;
		_previousClosePrice = candle.ClosePrice;
		_hasBaselineHistory = true;
		return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var previousDiff = _previousClosePrice - _previousBaselineValue;
		var currentDiff = candle.ClosePrice - baselineValue;

		var baselineCrossUp = previousDiff <= 0m && currentDiff > 0m;
		var baselineCrossDown = previousDiff >= 0m && currentDiff < 0m;

		var confirmation1Long = confirmationRsiValue >= ConfirmationRsiThreshold;
		var confirmation1Short = confirmationRsiValue <= 100m - ConfirmationRsiThreshold;

		var absCci = Math.Abs(confirmationCciValue);
		var confirmation2Long = confirmationCciValue >= 0m && absCci >= ConfirmationCciThreshold;
		var confirmation2Short = confirmationCciValue <= 0m && absCci >= ConfirmationCciThreshold;

		var volatilityOk = atrValue >= AtrMinimum;

		var canEnterLong = baselineCrossUp && confirmation1Long && confirmation2Long && volatilityOk;
		var canEnterShort = baselineCrossDown && confirmation1Short && confirmation2Short && volatilityOk;

		if (canEnterLong && Position <= 0)
		{
		var volumeToTrade = Volume + Math.Abs(Position);
		if (AtrEntryMultiplier > 0m && atrValue > 0m)
		{
		volumeToTrade = Math.Max(Volume, (int)Math.Round((double)(Volume * AtrEntryMultiplier)));
		}

		BuyMarket(volumeToTrade);
		SetupLongProtection(candle.ClosePrice);
		}
		else if (canEnterShort && Position >= 0)
		{
		var volumeToTrade = Volume + Math.Abs(Position);
		if (AtrEntryMultiplier > 0m && atrValue > 0m)
		{
		volumeToTrade = Math.Max(Volume, (int)Math.Round((double)(Volume * AtrEntryMultiplier)));
		}

		SellMarket(volumeToTrade);
		SetupShortProtection(candle.ClosePrice);
		}

		ManageLongPosition(candle);
		ManageShortPosition(candle);

		_previousBaselineValue = baselineValue;
		_previousClosePrice = candle.ClosePrice;
	}

	private void SetupLongProtection(decimal entryPrice)
	{
		if (AtrStopLossMultiplier > 0m && _lastAtrValue > 0m)
		_longStop = entryPrice - _lastAtrValue * AtrStopLossMultiplier;
		else
		_longStop = null;

		if (AtrTakeProfitMultiplier > 0m && _lastAtrValue > 0m)
		_longTake = entryPrice + _lastAtrValue * AtrTakeProfitMultiplier;
		else
		_longTake = null;

		_shortStop = null;
		_shortTake = null;
	}

	private void SetupShortProtection(decimal entryPrice)
	{
		if (AtrStopLossMultiplier > 0m && _lastAtrValue > 0m)
		_shortStop = entryPrice + _lastAtrValue * AtrStopLossMultiplier;
		else
		_shortStop = null;

		if (AtrTakeProfitMultiplier > 0m && _lastAtrValue > 0m)
		_shortTake = entryPrice - _lastAtrValue * AtrTakeProfitMultiplier;
		else
		_shortTake = null;

		_longStop = null;
		_longTake = null;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0)
		return;

		if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
		{
		SellMarket(Math.Abs(Position));
		_longTake = null;
		_longStop = null;
		return;
		}

		if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
		{
		SellMarket(Math.Abs(Position));
		_longTake = null;
		_longStop = null;
		return;
		}

		if (AtrTrailingMultiplier > 0m && _lastAtrValue > 0m)
		{
		var trailingCandidate = candle.ClosePrice - _lastAtrValue * AtrTrailingMultiplier;
		if (!_longStop.HasValue || trailingCandidate > _longStop.Value)
		_longStop = trailingCandidate;
		}

		if (_lastExitRsiValue <= ExitRsiLowerLevel)
		{
		SellMarket(Math.Abs(Position));
		_longTake = null;
		_longStop = null;
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0)
		return;

		if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
		{
		BuyMarket(Math.Abs(Position));
		_shortTake = null;
		_shortStop = null;
		return;
		}

		if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
		{
		BuyMarket(Math.Abs(Position));
		_shortTake = null;
		_shortStop = null;
		return;
		}

		if (AtrTrailingMultiplier > 0m && _lastAtrValue > 0m)
		{
		var trailingCandidate = candle.ClosePrice + _lastAtrValue * AtrTrailingMultiplier;
		if (!_shortStop.HasValue || trailingCandidate < _shortStop.Value)
		_shortStop = trailingCandidate;
		}

		if (_lastExitRsiValue >= ExitRsiUpperLevel)
		{
		BuyMarket(Math.Abs(Position));
		_shortTake = null;
		_shortStop = null;
		}
	}
}

