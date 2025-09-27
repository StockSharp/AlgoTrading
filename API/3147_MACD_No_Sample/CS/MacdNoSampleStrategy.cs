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
/// Port of the MetaTrader 5 expert advisor "MACD No Sample".
/// Combines a moving-average slope filter with MACD signal line crossovers and amplitude gating.
/// Includes configurable pip-based risk controls, trailing management, and optional risk-based position sizing.
/// </summary>
public class MacdNoSampleStrategy : Strategy
{
	/// <summary>
	/// Moving-average calculation options mirroring the original EA inputs.
	/// </summary>
	public enum MaMethodOptions
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}

	/// <summary>
	/// Candle price used when feeding the indicators.
	/// </summary>
	public enum AppliedPriceOptions
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}

	/// <summary>
	/// Position sizing mode: fixed trade volume or percentage risk per trade.
	/// </summary>
	public enum PositionSizingModes
	{
		FixedVolume,
		RiskPercent,
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<PositionSizingModes> _sizingMode;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MaMethodOptions> _maMethod;
	private readonly StrategyParam<AppliedPriceOptions> _maPrice;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<AppliedPriceOptions> _macdPrice;
	private readonly StrategyParam<decimal> _macdLevelPips;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _ma = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _previousMa;
	private decimal? _previousMacd;
	private decimal? _previousSignal;

	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longBestPrice;
	private decimal? _shortBestPrice;

	/// <summary>
	/// Default constructor wires up all public parameters.
	/// </summary>
	public MacdNoSampleStrategy()
	{
		_volume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trade volume used for market orders.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips. Zero disables the level.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Fixed take-profit distance in pips. Zero disables the level.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Distance in pips for trailing stop activation.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Minimal pip improvement required before moving the trailing stop.", "Risk");

		_sizingMode = Param(nameof(SizingMode), PositionSizingModes.FixedVolume)
		.SetDisplay("Position Sizing", "Select between fixed volume or risk percentage per trade.", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Percent", "Portfolio percentage risked when position sizing is set to risk mode.", "Trading");

		_maPeriod = Param(nameof(MaPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Averaging period for the trend filter.", "Indicator");

		_maMethod = Param(nameof(MaMethod), MaMethodOptions.Weighted)
		.SetDisplay("MA Method", "Moving-average smoothing algorithm.", "Indicator");

		_maPrice = Param(nameof(MaPrice), AppliedPriceOptions.Weighted)
		.SetDisplay("MA Price", "Candle price used for the moving-average filter.", "Indicator");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD.", "Indicator");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD.", "Indicator");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal-line EMA length.", "Indicator");

		_macdPrice = Param(nameof(MacdPrice), AppliedPriceOptions.Weighted)
		.SetDisplay("MACD Price", "Candle price type fed into MACD.", "Indicator");

		_macdLevelPips = Param(nameof(MacdLevelPips), 1m)
		.SetNotNegative()
		.SetDisplay("MACD Level (pips)", "Minimal absolute MACD distance from zero expressed in pips.", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe driving the analysis.", "Data");
	}

	/// <summary>
	/// Fixed volume submitted with each market order when sizing mode is set to <see cref="PositionSizingModes.FixedVolume"/>.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Chooses between fixed volume and risk-based sizing.
	/// </summary>
	public PositionSizingModes SizingMode
	{
		get => _sizingMode.Value;
		set => _sizingMode.Value = value;
	}

	/// <summary>
	/// Portfolio percentage used to size trades when <see cref="SizingMode"/> equals <see cref="PositionSizingModes.RiskPercent"/>.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Moving-average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving-average smoothing method.
	/// </summary>
	public MaMethodOptions MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the moving-average filter.
	/// </summary>
	public AppliedPriceOptions MaPrice
	{
		get => _maPrice.Value;
		set => _maPrice.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD calculation.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD calculation.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal-line EMA length for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Applied price for the MACD calculation.
	/// </summary>
	public AppliedPriceOptions MacdPrice
	{
		get => _macdPrice.Value;
		set => _macdPrice.Value = value;
	}

	/// <summary>
	/// Minimal MACD magnitude (in pips) required before entries are allowed.
	/// </summary>
	public decimal MacdLevelPips
	{
		get => _macdLevelPips.Value;
		set => _macdLevelPips.Value = value;
	}

	/// <summary>
	/// Candle type that drives indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousMa = null;
		_previousMacd = null;
		_previousSignal = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longBestPrice = null;
		_shortBestPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		_ma = CreateMovingAverage(MaMethod);
		_ma.Length = MaPeriod;

		_macd = new()
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = MacdFastPeriod },
				LongMa = new ExponentialMovingAverage { Length = MacdSlowPeriod },
			},
			SignalMa = new ExponentialMovingAverage { Length = MacdSignalPeriod }
		};

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageOpenPositions(candle);

		var maInput = GetAppliedPrice(candle, MaPrice);
		var maValue = _ma.Process(maInput, candle.OpenTime, true).ToDecimal();

		var macdInput = GetAppliedPrice(candle, MacdPrice);
		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)_macd.Process(macdInput, candle.OpenTime, true);
		var macd = macdValue.Macd as decimal?;
		var signal = macdValue.Signal as decimal?;

		if (!_ma.IsFormed || !_macd.IsFormed || macd is null || signal is null)
		{
			_previousMa = maValue;
			_previousMacd = macd;
			_previousSignal = signal;
			return;
		}

		var previousMa = _previousMa;
		var previousMacd = _previousMacd;
		var previousSignal = _previousSignal;

		_previousMa = maValue;
		_previousMacd = macd;
		_previousSignal = signal;

		if (!previousMa.HasValue || !previousMacd.HasValue || !previousSignal.HasValue)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var macdThreshold = ConvertPipsToPrice(MacdLevelPips);
		var macdMagnitude = Math.Abs(macd.Value);

		var buySignal = maValue > previousMa.Value &&
		macd.Value < 0m &&
		macd.Value > signal.Value &&
		previousMacd.Value < previousSignal.Value &&
		macdMagnitude > macdThreshold;

		var sellSignal = maValue < previousMa.Value &&
		macd.Value > 0m &&
		macd.Value < signal.Value &&
		previousMacd.Value > previousSignal.Value &&
		macdMagnitude > macdThreshold;

		if (buySignal)
		TryEnterLong(candle);

		if (sellSignal)
		TryEnterShort(candle);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position > 0m)
		return;

		if (Position < 0m)
		{
		ClosePosition();
		ResetShortState();
		}

		var volume = GetOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		_longEntryPrice = candle.ClosePrice;
		_longBestPrice = candle.HighPrice;

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		_longStopPrice = stopDistance > 0m ? candle.ClosePrice - stopDistance : null;

		_shortEntryPrice = null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position < 0m)
		return;

		if (Position > 0m)
		{
		ClosePosition();
		ResetLongState();
		}

		var volume = GetOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		SellMarket(volume);

		_shortEntryPrice = candle.ClosePrice;
		_shortBestPrice = candle.LowPrice;

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		_shortStopPrice = stopDistance > 0m ? candle.ClosePrice + stopDistance : null;

		_longEntryPrice = null;
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0m)
		{
		UpdateLongTrailing(candle);

		if (ShouldExitLong(candle))
		{
		ClosePosition();
		ResetLongState();
		}
		}
		else if (Position < 0m)
		{
		UpdateShortTrailing(candle);

		if (ShouldExitShort(candle))
		{
		ClosePosition();
		ResetShortState();
		}
		}
		else
		{
		ResetLongState();
		ResetShortState();
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
		return;

		var bestPrice = _longBestPrice ?? candle.HighPrice;
		if (candle.HighPrice > bestPrice)
		bestPrice = candle.HighPrice;

		_longBestPrice = bestPrice;

		var trailingDistance = ConvertPipsToPrice(TrailingStopPips);
		if (trailingDistance <= 0m)
		return;

		var candidateStop = bestPrice - trailingDistance;
		var step = ConvertPipsToPrice(TrailingStepPips);
		if (step <= 0m)
		step = _pipSize;

		if (candidateStop <= 0m)
		return;

		if (_longStopPrice is null)
		{
		_longStopPrice = candidateStop;
		return;
		}

		if (candidateStop > _longStopPrice.Value && candidateStop - _longStopPrice.Value >= step)
		_longStopPrice = candidateStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
		return;

		var bestPrice = _shortBestPrice ?? candle.LowPrice;
		if (candle.LowPrice < bestPrice)
		bestPrice = candle.LowPrice;

		_shortBestPrice = bestPrice;

		var trailingDistance = ConvertPipsToPrice(TrailingStopPips);
		if (trailingDistance <= 0m)
		return;

		var candidateStop = bestPrice + trailingDistance;
		var step = ConvertPipsToPrice(TrailingStepPips);
		if (step <= 0m)
		step = _pipSize;

		if (candidateStop <= 0m)
		return;

		if (_shortStopPrice is null)
		{
		_shortStopPrice = candidateStop;
		return;
		}

		if (candidateStop < _shortStopPrice.Value && _shortStopPrice.Value - candidateStop >= step)
		_shortStopPrice = candidateStop;
	}

	private bool ShouldExitLong(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
		return false;

		var takeDistance = ConvertPipsToPrice(TakeProfitPips);
		if (takeDistance > 0m)
		{
		var takePrice = _longEntryPrice.Value + takeDistance;
		if (candle.HighPrice >= takePrice)
		return true;
		}

		var stopPrice = _longStopPrice;
		if (stopPrice.HasValue && candle.LowPrice <= stopPrice.Value)
		return true;

		return false;
	}

	private bool ShouldExitShort(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
		return false;

		var takeDistance = ConvertPipsToPrice(TakeProfitPips);
		if (takeDistance > 0m)
		{
		var takePrice = _shortEntryPrice.Value - takeDistance;
		if (candle.LowPrice <= takePrice)
		return true;
		}

		var stopPrice = _shortStopPrice;
		if (stopPrice.HasValue && candle.HighPrice >= stopPrice.Value)
		return true;

		return false;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longBestPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortBestPrice = null;
	}

	private decimal GetOrderVolume(decimal price)
	{
		if (SizingMode == PositionSizingModes.FixedVolume)
		return TradeVolume;

		if (Portfolio is null || price <= 0m)
		return TradeVolume;

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		if (stopDistance <= 0m)
		return TradeVolume;

		var portfolioValue = Portfolio.CurrentValue;
		if (portfolioValue <= 0m)
		return TradeVolume;

		var riskAmount = portfolioValue * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return TradeVolume;

		var estimatedVolume = riskAmount / stopDistance;

		var volumeStep = Security?.VolumeStep;
		if (volumeStep is > 0m)
		estimatedVolume = Math.Floor(estimatedVolume / volumeStep.Value) * volumeStep.Value;

		var minVolume = Security?.MinVolume;
		if (minVolume is > 0m && estimatedVolume < minVolume.Value)
		estimatedVolume = minVolume.Value;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is > 0m && estimatedVolume > maxVolume.Value)
		estimatedVolume = maxVolume.Value;

		if (estimatedVolume <= 0m)
		estimatedVolume = TradeVolume;

		return estimatedVolume;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		var pip = _pipSize;
		if (pip <= 0m)
		pip = 0.0001m;

		return pips * pip;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep is null || priceStep.Value <= 0m)
		return 0.0001m;

		var decimals = Security?.Decimals;
		if (decimals is 3 or 5)
		return priceStep.Value * 10m;

		return priceStep.Value;
	}

	private decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceOptions priceOption)
	{
		return priceOption switch
		{
		AppliedPriceOptions.Open => candle.OpenPrice,
		AppliedPriceOptions.High => candle.HighPrice,
		AppliedPriceOptions.Low => candle.LowPrice,
		AppliedPriceOptions.Median => (candle.HighPrice + candle.LowPrice) / 2m,
		AppliedPriceOptions.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
		AppliedPriceOptions.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
		_ => candle.ClosePrice,
		};
	}

	private MovingAverage CreateMovingAverage(MaMethodOptions method)
	{
		return method switch
		{
		MaMethodOptions.Exponential => new ExponentialMovingAverage(),
		MaMethodOptions.Smoothed => new SmoothedMovingAverage(),
		MaMethodOptions.Weighted => new WeightedMovingAverage(),
		_ => new SimpleMovingAverage(),
		};
	}
}

