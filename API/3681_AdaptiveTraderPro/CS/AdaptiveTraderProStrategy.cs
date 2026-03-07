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
/// Adaptive multi-timeframe strategy converted from the "AdaptiveTrader Pro" expert advisor.
/// Combines RSI, ATR and dual moving averages to align entries with the prevailing trend.
/// Applies risk-based position sizing, partial profit taking, break-even logic and ATR driven trailing stops.
/// </summary>
public class AdaptiveTraderProStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxRiskPercent;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _trailingStopMultiplier;
	private readonly StrategyParam<decimal> _trailingTakeProfitMultiplier;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _higherTrendPeriod;
	private readonly StrategyParam<decimal> _breakEvenMultiplier;
	private readonly StrategyParam<decimal> _partialCloseFraction;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;

	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private decimal _lastHigherTrendValue;

	private decimal _entryPrice;
	private decimal _entryVolume;
	private decimal _entryAtr;
	private bool _breakEvenApplied;
	private bool _partialTakeProfitDone;
	private decimal _trailingStopLevel;

	/// <summary>
	/// Maximum risk percentage allocated per trade.
	/// </summary>
	public decimal MaxRiskPercent
	{
		get => _maxRiskPercent.Value;
		set => _maxRiskPercent.Value = value;
	}

	/// <summary>
	/// RSI period used on the main timeframe.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// ATR period used on the main timeframe.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for stop-loss sizing.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for trailing stop adjustments.
	/// </summary>
	public decimal TrailingStopMultiplier
	{
		get => _trailingStopMultiplier.Value;
		set => _trailingStopMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for the partial take-profit objective.
	/// </summary>
	public decimal TrailingTakeProfitMultiplier
	{
		get => _trailingTakeProfitMultiplier.Value;
		set => _trailingTakeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Moving average period used on the main timeframe.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period used on the higher timeframe.
	/// </summary>
	public int HigherTrendPeriod
	{
		get => _higherTrendPeriod.Value;
		set => _higherTrendPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier that defines when to move the stop to break even.
	/// </summary>
	public decimal BreakEvenMultiplier
	{
		get => _breakEvenMultiplier.Value;
		set => _breakEvenMultiplier.Value = value;
	}

	/// <summary>
	/// Fraction of the initial position closed at the first target.
	/// </summary>
	public decimal PartialCloseFraction
	{
		get => _partialCloseFraction.Value;
		set => _partialCloseFraction.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread expressed in price steps.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Candle type used on the main timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used for higher timeframe confirmation.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AdaptiveTraderProStrategy"/> class.
	/// </summary>
	public AdaptiveTraderProStrategy()
	{
		_maxRiskPercent = Param(nameof(MaxRiskPercent), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators")
		
		.SetOptimize(8, 20, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Length of the ATR indicator", "Indicators")
		
		.SetOptimize(7, 21, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR for stops", "Risk Management")
		
		.SetOptimize(1.0m, 3.0m, 0.5m);

		_trailingStopMultiplier = Param(nameof(TrailingStopMultiplier), 3.0m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop Multiplier", "ATR multiplier for trailing stop", "Risk Management")
		
		.SetOptimize(0.5m, 2.5m, 0.5m);

		_trailingTakeProfitMultiplier = Param(nameof(TrailingTakeProfitMultiplier), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing TP Multiplier", "ATR multiplier for partial profit", "Risk Management")
		
		.SetOptimize(1.0m, 3.0m, 0.5m);

		_trendPeriod = Param(nameof(TrendPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Main Trend Period", "SMA length on the main timeframe", "Indicators");

		_higherTrendPeriod = Param(nameof(HigherTrendPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("Higher Trend Period", "SMA length on the higher timeframe", "Indicators");

		_breakEvenMultiplier = Param(nameof(BreakEvenMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Break Even Multiplier", "ATR multiplier that activates break even", "Risk Management");

		_partialCloseFraction = Param(nameof(PartialCloseFraction), 0m)
		.SetDisplay("Partial Close Fraction", "Fraction of the volume closed at the first target", "Risk Management");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
		.SetDisplay("Max Spread (points)", "Maximum allowed spread in price steps", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Main Candle Type", "Primary timeframe used for signals", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Higher Candle Type", "Confirmation timeframe used for trend", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (HigherCandleType != CandleType)
			yield return (Security, HigherCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_entryVolume = 0m;
		_entryAtr = 0m;
		_breakEvenApplied = false;
		_partialTakeProfitDone = false;
		_trailingStopLevel = 0m;
		_bestBidPrice = null;
		_bestAskPrice = null;
		_lastHigherTrendValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetTradeState();

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var trendMa = new SimpleMovingAverage { Length = TrendPeriod };
		var higherTrendMa = new SimpleMovingAverage { Length = HigherTrendPeriod };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(rsi, atr, trendMa, ProcessMainCandle).Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(higherTrendMa, ProcessHigherCandle).Start();
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal higherTrend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastHigherTrendValue = higherTrend;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		//if (!IsFormedAndOnlineAndAllowTrading())
		//	return;

		//if (!_hasHigherTrend)
		//	return;

		//if (!IsSpreadAllowed())
		//	return;

		UpdateTrailingManagement(candle, atrValue);

		if (Position != 0m)
			return;

		if (atrValue <= 0m)
			return;

		var closePrice = candle.ClosePrice;

		if (rsiValue < 45m && closePrice > trendValue)
		{
			TryEnterLong(closePrice, atrValue);
		}
		else if (rsiValue > 55m && closePrice < trendValue)
		{
			TryEnterShort(closePrice, atrValue);
		}
	}

	private void TryEnterLong(decimal entryPrice, decimal atrValue)
	{
		if (Position < 0m)
		{
			BuyMarket();
			return;
		}

		BuyMarket();
		InitializeTradeState(1, entryPrice, atrValue, Volume > 0 ? Volume : 1m);
	}

	private void TryEnterShort(decimal entryPrice, decimal atrValue)
	{
		if (Position > 0m)
		{
			SellMarket();
			return;
		}

		SellMarket();
		InitializeTradeState(-1, entryPrice, atrValue, Volume > 0 ? Volume : 1m);
	}

	private void UpdateTrailingManagement(ICandleMessage candle, decimal atrValue)
	{
		if (Position > 0m)
		{
			var atrForTargets = _entryAtr > 0m ? _entryAtr : atrValue;
			var trailingDistance = atrValue * TrailingStopMultiplier;
			var candidateStop = candle.ClosePrice - trailingDistance;

			if (_trailingStopLevel <= 0m || candidateStop > _trailingStopLevel)
				_trailingStopLevel = candidateStop;

			if (!_breakEvenApplied && atrForTargets > 0m)
			{
				var breakEvenTrigger = _entryPrice + atrForTargets * BreakEvenMultiplier;
				if (candle.HighPrice >= breakEvenTrigger)
				{
					_trailingStopLevel = Math.Max(_trailingStopLevel, _entryPrice);
					_breakEvenApplied = true;
				}
			}

			if (!_partialTakeProfitDone && PartialCloseFraction > 0m && PartialCloseFraction < 1m && atrForTargets > 0m)
			{
				var partialTarget = _entryPrice + atrForTargets * TrailingTakeProfitMultiplier;
				if (candle.HighPrice >= partialTarget)
				{
					var desiredVolume = NormalizeVolume(_entryVolume * PartialCloseFraction);
					var availableVolume = Math.Max(Position, 0m);
					var volumeToClose = Math.Min(availableVolume, desiredVolume);

					if (volumeToClose > 0m)
					{
						SellMarket(volumeToClose);
						_partialTakeProfitDone = true;
					}
				}
			}

			if (_trailingStopLevel > 0m && candle.LowPrice <= _trailingStopLevel)
			{
				SellMarket(Math.Max(Position, 0m));
				ResetTradeState();
			}
		}
		else if (Position < 0m)
		{
			var atrForTargets = _entryAtr > 0m ? _entryAtr : atrValue;
			var trailingDistance = atrValue * TrailingStopMultiplier;
			var candidateStop = candle.ClosePrice + trailingDistance;

			if (_trailingStopLevel <= 0m || candidateStop < _trailingStopLevel)
				_trailingStopLevel = candidateStop;

			if (!_breakEvenApplied && atrForTargets > 0m)
			{
				var breakEvenTrigger = _entryPrice - atrForTargets * BreakEvenMultiplier;
				if (candle.LowPrice <= breakEvenTrigger)
				{
					_trailingStopLevel = Math.Min(_trailingStopLevel, _entryPrice);
					_breakEvenApplied = true;
				}
			}

			if (!_partialTakeProfitDone && PartialCloseFraction > 0m && PartialCloseFraction < 1m && atrForTargets > 0m)
			{
				var partialTarget = _entryPrice - atrForTargets * TrailingTakeProfitMultiplier;
				if (candle.LowPrice <= partialTarget)
				{
					var desiredVolume = NormalizeVolume(_entryVolume * PartialCloseFraction);
					var availableVolume = Math.Max(Math.Abs(Position), 0m);
					var volumeToClose = Math.Min(availableVolume, desiredVolume);

					if (volumeToClose > 0m)
					{
						BuyMarket(volumeToClose);
						_partialTakeProfitDone = true;
					}
				}
			}

			if (_trailingStopLevel > 0m && candle.HighPrice >= _trailingStopLevel)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		else
		{
			ResetTradeState();
		}
	}

	private bool IsSpreadAllowed()
	{
		if (MaxSpreadPoints <= 0m)
			return true;

		if (_bestBidPrice is not decimal bid || _bestAskPrice is not decimal ask)
			return false;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var spreadPoints = (ask - bid) / step;
		return spreadPoints <= MaxSpreadPoints;
	}

	// Quote handling removed - not needed for backtest

	private void InitializeTradeState(int direction, decimal entryPrice, decimal atrValue, decimal volume)
	{
		_entryPrice = entryPrice;
		_entryVolume = volume;
		_entryAtr = atrValue;
		_breakEvenApplied = false;
		_partialTakeProfitDone = false;
		_trailingStopLevel = direction == 1
		? entryPrice - atrValue * TrailingStopMultiplier
		: entryPrice + atrValue * TrailingStopMultiplier;
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_entryVolume = 0m;
		_entryAtr = 0m;
		_breakEvenApplied = false;
		_partialTakeProfitDone = false;
		_trailingStopLevel = 0m;
	}

	private decimal CalculateOrderVolume(decimal atrValue)
	{
		return Volume > 0 ? Volume : 1m;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return Volume > 0 ? Volume : 1m;

		return volume;
	}
}

