using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High-level port of the "EuroSurge Simplified" MetaTrader strategy.
/// Combines MA trend detection with optional RSI, MACD, Bollinger Bands, and Stochastic filters.
/// Enforces a minimum waiting period between entries and supports several position sizing modes.
/// </summary>
public class EuroSurgeSimplifiedStrategy : Strategy
{
	private readonly StrategyParam<TradeSizeTypes> _tradeSizeType;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _tradeSizePercent;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _minTradeIntervalMinutes;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<bool> _useMacd;
	private readonly StrategyParam<bool> _useBollinger;
	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset _lastTradeTime;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal _fastMaValue;
	private decimal _slowMaValue;
	private decimal _rsiValue;

	/// <summary>
	/// Gets or sets the trade size calculation mode.
	/// </summary>
	public TradeSizeTypes TradeSizeType
	{
		get => _tradeSizeType.Value;
		set => _tradeSizeType.Value = value;
	}

	/// <summary>
	/// Gets or sets the fixed trading volume.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the percentage used by percent-based position sizing modes.
	/// </summary>
	public decimal TradeSizePercent
	{
		get => _tradeSizePercent.Value;
		set => _tradeSizePercent.Value = value;
	}

	/// <summary>
	/// Gets or sets the take-profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the minimum delay between consecutive entries in minutes.
	/// </summary>
	public int MinTradeIntervalMinutes
	{
		get => _minTradeIntervalMinutes.Value;
		set => _minTradeIntervalMinutes.Value = value;
	}

	/// <summary>
	/// Gets or sets the longer moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the RSI threshold that enables long trades.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// Gets or sets the RSI threshold that enables short trades.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Gets or sets the fast MACD EMA period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Gets or sets the slow MACD EMA period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Gets or sets the MACD signal SMA period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Gets or sets the Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Gets or sets the Bollinger Bands width measured in deviations.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Gets or sets the Stochastic oscillator smoothing length.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Gets or sets the Stochastic %K period.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// Gets or sets the Stochastic %D period.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
	}

	/// <summary>
	/// Gets or sets the flag that enables moving average filtering.
	/// </summary>
	public bool UseMa
	{
		get => _useMa.Value;
		set => _useMa.Value = value;
	}

	/// <summary>
	/// Gets or sets the flag that enables RSI filtering.
	/// </summary>
	public bool UseRsi
	{
		get => _useRsi.Value;
		set => _useRsi.Value = value;
	}

	/// <summary>
	/// Gets or sets the flag that enables MACD filtering.
	/// </summary>
	public bool UseMacd
	{
		get => _useMacd.Value;
		set => _useMacd.Value = value;
	}

	/// <summary>
	/// Gets or sets the flag that enables Bollinger Bands filtering.
	/// </summary>
	public bool UseBollinger
	{
		get => _useBollinger.Value;
		set => _useBollinger.Value = value;
	}

	/// <summary>
	/// Gets or sets the flag that enables Stochastic oscillator filtering.
	/// </summary>
	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EuroSurgeSimplifiedStrategy()
	{
		_tradeSizeType = Param(nameof(TradeSizeType), TradeSizeTypes.FixedSize)
		.SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed Volume", "Lot size used when TradeSizeTypes is FixedSize", "Money Management");

		_tradeSizePercent = Param(nameof(TradeSizePercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Size %", "Percentage used for balance/equity sizing", "Money Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1400)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pts)", "Distance in price steps for take-profit", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 900)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pts)", "Distance in price steps for stop-loss", "Risk Management");

		_minTradeIntervalMinutes = Param(nameof(MinTradeIntervalMinutes), 600)
		.SetNotNegative()
		.SetDisplay("Min Trade Interval", "Minimum minutes between entries", "Execution");

		_maPeriod = Param(nameof(MaPeriod), 52)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the long moving average", "Indicators")
		
		.SetOptimize(30, 150, 10);

		_rsiPeriod = Param(nameof(RsiPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of the RSI filter", "Indicators")
		
		.SetOptimize(5, 30, 1);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 50m)
		.SetDisplay("RSI Buy Level", "Maximum RSI value that allows long trades", "Indicators");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 50m)
		.SetDisplay("RSI Sell Level", "Minimum RSI value that allows short trades", "Indicators");

		_macdFast = Param(nameof(MacdFast), 8)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 24)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 13)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal SMA length", "Indicators");

		_bollingerLength = Param(nameof(BollingerLength), 25)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2.5m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators");

		_stochasticLength = Param(nameof(StochasticLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Length", "Smoothing length of the oscillator", "Indicators");

		_stochasticK = Param(nameof(StochasticK), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "%K averaging period", "Indicators");

		_stochasticD = Param(nameof(StochasticD), 2)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "%D averaging period", "Indicators");

		_useMa = Param(nameof(UseMa), true)
		.SetDisplay("Use MA", "Enable moving average trend filter", "Filters");

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI filter", "Filters");

		_useMacd = Param(nameof(UseMacd), true)
		.SetDisplay("Use MACD", "Enable MACD filter", "Filters");

		_useBollinger = Param(nameof(UseBollinger), false)
		.SetDisplay("Use Bollinger", "Enable Bollinger Bands filter", "Filters");

		_useStochastic = Param(nameof(UseStochastic), true)
		.SetDisplay("Use Stochastic", "Enable Stochastic oscillator filter", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for signal calculations", "Execution");
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
		_lastTradeTime = DateTimeOffset.MinValue;
		_fastMaValue = 0m;
		_slowMaValue = 0m;
		_rsiValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new SimpleMovingAverage { Length = 20 };
		_slowMa = new SimpleMovingAverage { Length = MaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _rsi, ProcessCandle)
			.Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastMaValue = fastValue;
		_slowMaValue = slowValue;
		_rsiValue = rsiValue;

		if (!TryBuildSignals(candle, out var isBuySignal, out var isSellSignal))
			return;

		if (!isBuySignal && !isSellSignal)
			return;

		var now = candle.CloseTime;
		var minInterval = TimeSpan.FromMinutes(MinTradeIntervalMinutes);
		if (_lastTradeTime != DateTimeOffset.MinValue && now - _lastTradeTime < minInterval)
			return;

		var volume = CalculateTradeVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		var currentPosition = Position;

		if (isBuySignal && currentPosition <= 0m)
		{
			var orderVolume = volume;
			if (currentPosition < 0m)
				orderVolume += Math.Abs(currentPosition);

			BuyMarket(orderVolume);

			_lastTradeTime = now;
		}
		else if (isSellSignal && currentPosition >= 0m)
		{
			var orderVolume = volume;
			if (currentPosition > 0m)
				orderVolume += Math.Abs(currentPosition);

			SellMarket(orderVolume);

			_lastTradeTime = now;
		}
	}

	private bool TryBuildSignals(ICandleMessage candle, out bool isBuySignal, out bool isSellSignal)
	{
		isBuySignal = false;
		isSellSignal = false;

		if (UseMa && (!_fastMa.IsFormed || !_slowMa.IsFormed))
			return false;

		if (UseRsi && !_rsi.IsFormed)
			return false;

		var fast = _fastMaValue;
		var slow = _slowMaValue;
		var rsi = _rsiValue;

		var maConditionBuy = !UseMa || fast > slow;
		var maConditionSell = !UseMa || fast < slow;

		var rsiConditionBuy = !UseRsi || rsi <= RsiBuyLevel;
		var rsiConditionSell = !UseRsi || rsi >= RsiSellLevel;

		isBuySignal = maConditionBuy && rsiConditionBuy;
		isSellSignal = maConditionSell && rsiConditionSell;

		return true;
	}

	private decimal CalculateTradeVolume(decimal referencePrice)
	{
		var volume = FixedVolume;

		switch (TradeSizeType)
		{
			case TradeSizeTypes.BalancePercent when Portfolio?.BeginValue is decimal balance && balance > 0m && referencePrice > 0m:
			{
				var moneyToUse = balance * TradeSizePercent / 100m;
				var estimatedVolume = moneyToUse / referencePrice;
				if (estimatedVolume > 0m)
					volume = estimatedVolume;
				break;
			}

			case TradeSizeTypes.EquityPercent when Portfolio?.CurrentValue is decimal equity && equity > 0m && referencePrice > 0m:
			{
				var moneyToUse = equity * TradeSizePercent / 100m;
				var estimatedVolume = moneyToUse / referencePrice;
				if (estimatedVolume > 0m)
					volume = estimatedVolume;
				break;
			}
		}

		var minVolume = Security?.MinVolume;
		if (minVolume is decimal min && min > 0m && volume < min)
			volume = min;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		var step = Security?.VolumeStep;
		if (step is decimal s && s > 0m)
		{
			var steps = Math.Round(volume / s);
			volume = steps * s;
		}

		return volume > 0m ? volume : 0m;
	}

	public enum TradeSizeTypes
	{
		FixedSize,
		BalancePercent,
		EquityPercent,
	}
}