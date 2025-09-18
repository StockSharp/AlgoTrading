using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the MetaTrader expert advisor "MultiStrategyEA v1.2".
/// Combines seven oscillators (AC, ADX, AO, DeMarker, Force/Bollinger, MFI and MACD+Stochastic)
/// and requires a configurable number of bullish or bearish confirmations before entering a trade.
/// </summary>
public class MultiStrategyEaV12Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _tradeAllStrategies;
	private readonly StrategyParam<int> _requiredConfirmations;
	private readonly StrategyParam<bool> _closeInReverse;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private readonly StrategyParam<bool> _acEnabled;
	private readonly StrategyParam<decimal> _acOpenLevel;

	private readonly StrategyParam<bool> _adxEnabled;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxTrendLevel;
	private readonly StrategyParam<decimal> _adxDirectionalLevel;

	private readonly StrategyParam<bool> _aoEnabled;
	private readonly StrategyParam<decimal> _aoOpenLevel;

	private readonly StrategyParam<bool> _demEnabled;
	private readonly StrategyParam<int> _demPeriod;
	private readonly StrategyParam<decimal> _demThreshold;

	private readonly StrategyParam<bool> _fbbEnabled;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _forceConfirmationLevel;
	private readonly StrategyParam<decimal> _bandDistanceFilter;

	private readonly StrategyParam<bool> _mfiEnabled;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiThreshold;

	private readonly StrategyParam<bool> _msEnabled;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stochasticSignal;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _macdLevel;
	private readonly StrategyParam<decimal> _stochasticLevel;

	private AwesomeOscillator _ao = null!;
	private SimpleMovingAverage _aoSma = null!;
	private AverageDirectionalIndex _adx = null!;
	private DeMarker _demarker = null!;
	private BollingerBands _bollinger = null!;
	private ForceIndex _forceIndex = null!;
	private MoneyFlowIndex _mfi = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal? _acPrev;
	private decimal? _acLast;
	private decimal? _aoPrev;
	private decimal? _aoLast;
	private decimal? _demLast;
	private decimal? _mfiLast;
	private decimal? _forceLast;
	private decimal? _macdMainLast;
	private decimal? _macdSignalLast;
	private decimal? _stochasticKLast;
	private decimal? _stochasticDLast;

	private decimal? _adxStrength;
	private decimal? _adxPlus;
	private decimal? _adxMinus;

	private decimal? _bollingerUpper;
	private decimal? _bollingerLower;

	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="MultiStrategyEaV12Strategy"/>.
	/// </summary>
	public MultiStrategyEaV12Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by every indicator", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Order Volume", "Base trade volume when a consensus signal appears", "Orders")
			.SetGreaterThanZero();

		_tradeAllStrategies = Param(nameof(TradeAllStrategies), true)
			.SetDisplay("Use Consensus", "Require multiple modules to agree before trading", "Consensus");

		_requiredConfirmations = Param(nameof(RequiredConfirmations), 3)
			.SetDisplay("Required Confirmations", "Number of bullish/bearish modules required for an entry", "Consensus")
			.SetRange(1, 7);

		_closeInReverse = Param(nameof(CloseInReverse), true)
			.SetDisplay("Close On Opposite", "Exit the current position before opening the opposite direction", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 60m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance applied to consensus positions", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 95m)
			.SetDisplay("Take Profit (pips)", "Target profit distance for consensus positions", "Risk");

		_acEnabled = Param(nameof(UseAcModule), true)
			.SetDisplay("Use AC", "Enable Accelerator Oscillator module", "Acceleration");

		_acOpenLevel = Param(nameof(AcLevel), 80m)
			.SetDisplay("AC Threshold", "Acceleration value required to count as a signal", "Acceleration")
			.SetGreaterThanZero();

		_adxEnabled = Param(nameof(UseAdxModule), true)
			.SetDisplay("Use ADX", "Enable ADX strength filter", "ADX");

		_adxPeriod = Param(nameof(AdxPeriod), 20)
			.SetDisplay("ADX Period", "Lookback period for ADX calculation", "ADX")
			.SetGreaterThanZero();

		_adxTrendLevel = Param(nameof(AdxTrendLevel), 55m)
			.SetDisplay("ADX Trend Level", "ADX value above which trend signals are accepted", "ADX")
			.SetGreaterThanZero();

		_adxDirectionalLevel = Param(nameof(AdxDirectionalLevel), 15m)
			.SetDisplay("Directional Level", "Minimum +DI/-DI reading required", "ADX")
			.SetGreaterThanZero();

		_aoEnabled = Param(nameof(UseAoModule), true)
			.SetDisplay("Use AO", "Enable Awesome Oscillator momentum check", "AO");

		_aoOpenLevel = Param(nameof(AoLevel), 55m)
			.SetDisplay("AO Threshold", "Awesome Oscillator level required for a signal", "AO")
			.SetGreaterThanZero();

		_demEnabled = Param(nameof(UseDeMarkerModule), true)
			.SetDisplay("Use DeMarker", "Enable DeMarker reversal module", "DeMarker");

		_demPeriod = Param(nameof(DeMarkerPeriod), 20)
			.SetDisplay("DeMarker Period", "Number of bars used by the DeMarker oscillator", "DeMarker")
			.SetGreaterThanZero();

		_demThreshold = Param(nameof(DeMarkerThreshold), 75m)
			.SetDisplay("DeMarker Threshold", "Overbought/oversold threshold (0..100)", "DeMarker")
			.SetRange(1m, 99m);

		_fbbEnabled = Param(nameof(UseForceBollingerModule), true)
			.SetDisplay("Use Force+Bollinger", "Enable Force Index confirmation together with Bollinger bands", "Bollinger/Force");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Averaging length for the Bollinger band", "Bollinger/Force")
			.SetGreaterThanZero();

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1.8m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Bollinger/Force")
			.SetGreaterThanZero();

		_forceConfirmationLevel = Param(nameof(ForceConfirmationLevel), 50m)
			.SetDisplay("Force Confirmation", "Force Index level confirming momentum", "Bollinger/Force")
			.SetGreaterThanZero();

		_bandDistanceFilter = Param(nameof(BandDistanceFilter), 0m)
			.SetDisplay("Band Distance Filter", "Minimum band width in pips required (use negative to invert)", "Bollinger/Force");

		_mfiEnabled = Param(nameof(UseMfiModule), true)
			.SetDisplay("Use MFI", "Enable Money Flow Index reversals", "MFI");

		_mfiPeriod = Param(nameof(MfiPeriod), 12)
			.SetDisplay("MFI Period", "Number of bars used to calculate the MFI", "MFI")
			.SetGreaterThanZero();

		_mfiThreshold = Param(nameof(MfiThreshold), 90m)
			.SetDisplay("MFI Threshold", "Overbought/oversold threshold (0..100)", "MFI")
			.SetRange(1m, 99m);

		_msEnabled = Param(nameof(UseMacdStochasticModule), true)
			.SetDisplay("Use MACD+Stochastic", "Enable combined MACD and Stochastic confirmation", "MACD+Stochastic");

		_macdFast = Param(nameof(MacdFastPeriod), 3)
			.SetDisplay("MACD Fast Period", "Fast EMA length for MACD", "MACD+Stochastic")
			.SetGreaterThanZero();

		_macdSlow = Param(nameof(MacdSlowPeriod), 9)
			.SetDisplay("MACD Slow Period", "Slow EMA length for MACD", "MACD+Stochastic")
			.SetGreaterThanZero();

		_macdSignal = Param(nameof(MacdSignalPeriod), 2)
			.SetDisplay("MACD Signal Period", "Signal EMA length", "MACD+Stochastic")
			.SetGreaterThanZero();

		_stochasticPeriod = Param(nameof(StochasticPeriod), 5)
			.SetDisplay("Stochastic %K", "Stochastic %K length", "MACD+Stochastic")
			.SetGreaterThanZero();

		_stochasticSignal = Param(nameof(StochasticSignalPeriod), 3)
			.SetDisplay("Stochastic %D", "Stochastic signal length", "MACD+Stochastic")
			.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 12)
			.SetDisplay("Stochastic Slowing", "Slowing value applied to %K", "MACD+Stochastic")
			.SetGreaterThanZero();

		_macdLevel = Param(nameof(MacdLevel), 60m)
			.SetDisplay("MACD Level", "Absolute MACD value required for a signal", "MACD+Stochastic")
			.SetGreaterThanZero();

		_stochasticLevel = Param(nameof(StochasticLevel), 80m)
			.SetDisplay("Stochastic Level", "Upper/lower bound used to detect extreme momentum", "MACD+Stochastic")
			.SetRange(1m, 99m);
	}

	/// <summary>
	/// Main candle series used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicates whether the strategy requires multiple modules to agree before trading.
	/// </summary>
	public bool TradeAllStrategies
	{
		get => _tradeAllStrategies.Value;
		set => _tradeAllStrategies.Value = value;
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

		_acPrev = null;
		_acLast = null;
		_aoPrev = null;
		_aoLast = null;
		_demLast = null;
		_mfiLast = null;
		_forceLast = null;
		_macdMainLast = null;
		_macdSignalLast = null;
		_stochasticKLast = null;
		_stochasticDLast = null;
		_adxStrength = null;
		_adxPlus = null;
		_adxMinus = null;
		_bollingerUpper = null;
		_bollingerLower = null;
		_pipSize = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = _volume.Value;
		_pipSize = CalculatePipSize();

		_ao = new AwesomeOscillator();
		_aoSma = new SimpleMovingAverage { Length = 5 };

		_adx = new AverageDirectionalIndex { Length = _adxPeriod.Value };
		_demarker = new DeMarker { Length = _demPeriod.Value };
		_bollinger = new BollingerBands
		{
			Length = _bollingerPeriod.Value,
			Width = _bollingerDeviation.Value
		};
		_forceIndex = new ForceIndex { Length = _bollingerPeriod.Value };
		_mfi = new MoneyFlowIndex { Length = _mfiPeriod.Value };
		_macd = new MovingAverageConvergenceDivergence
		{
			LongPeriod = _macdSlow.Value,
			ShortPeriod = _macdFast.Value,
			SignalPeriod = _macdSignal.Value
		};
		_stochastic = new StochasticOscillator
		{
			Length = _stochasticPeriod.Value,
			KPeriod = _stochasticPeriod.Value,
			DPeriod = _stochasticSignal.Value,
			Slowing = _stochasticSlowing.Value
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!ProcessIndicators(candle))
			return;

		var signals = new List<SignalDirection>();

		if (_acEnabled.Value)
			CollectSignal(signals, EvaluateAcSignal());

		if (_adxEnabled.Value)
			CollectSignal(signals, EvaluateAdxSignal());

		if (_aoEnabled.Value)
			CollectSignal(signals, EvaluateAoSignal());

		if (_demEnabled.Value)
			CollectSignal(signals, EvaluateDeMarkerSignal());

		if (_fbbEnabled.Value)
			CollectSignal(signals, EvaluateForceBollingerSignal(candle));

		if (_mfiEnabled.Value)
			CollectSignal(signals, EvaluateMfiSignal());

		if (_msEnabled.Value)
			CollectSignal(signals, EvaluateMacdStochasticSignal());

		var decision = ResolveConsensus(signals);

		ManagePositions(decision, candle);
	}

	private bool ProcessIndicators(ICandleMessage candle)
	{
		var aoValue = _ao.Process(candle);
		if (!aoValue.IsFinal || !aoValue.TryGetValue(out decimal currentAo))
			return false;

		var aoSmaValue = _aoSma.Process(aoValue);
		if (!aoSmaValue.IsFinal || !aoSmaValue.TryGetValue(out decimal aoSma))
			return false;

		var currentAc = currentAo - aoSma;

		var adxValue = _adx.Process(candle);
		if (adxValue is AverageDirectionalIndexValue typedAdx && adxValue.IsFinal)
		{
			_adxStrength = typedAdx.MovingAverage as decimal?;
			_adxPlus = typedAdx.PlusDi as decimal?;
			_adxMinus = typedAdx.MinusDi as decimal?;
		}

		var demValue = _demarker.Process(candle);
		var demarkerReady = demValue.IsFinal && demValue.TryGetValue(out decimal currentDemarker);

		var bollingerValue = _bollinger.Process(candle);
		var bollingerReady = false;
		if (bollingerValue.IsFinal)
		{
			if (bollingerValue is BollingerBandsValue bands)
			{
				_bollingerUpper = bands.UpBand as decimal?;
				_bollingerLower = bands.LowBand as decimal?;
				bollingerReady = _bollingerUpper is not null && _bollingerLower is not null;
			}
		}

		var forceValue = _forceIndex.Process(candle);
		var forceReady = forceValue.IsFinal && forceValue.TryGetValue(out decimal currentForce);

		var mfiValue = _mfi.Process(candle);
		var mfiReady = mfiValue.IsFinal && mfiValue.TryGetValue(out decimal currentMfi);

		var macdValue = _macd.Process(candle);
		var macdReady = false;
		decimal? macdMain = null;
		decimal? macdSignal = null;
		if (macdValue.IsFinal)
		{
			if (macdValue is MovingAverageConvergenceDivergenceValue macdTyped)
			{
				macdMain = macdTyped.Macd as decimal?;
				macdSignal = macdTyped.Signal as decimal?;
				macdReady = macdMain is not null && macdSignal is not null;
			}
		}

		var stochasticValue = _stochastic.Process(candle);
		var stochasticReady = false;
		decimal? stochasticK = null;
		decimal? stochasticD = null;
		if (stochasticValue.IsFinal)
		{
			if (stochasticValue is StochasticOscillatorValue stochTyped)
			{
				stochasticK = stochTyped.K as decimal?;
				stochasticD = stochTyped.D as decimal?;
				stochasticReady = stochasticK is not null && stochasticD is not null;
			}
		}

		if (!_ao.IsFormed || !_aoSma.IsFormed)
			return false;

		if (_adxEnabled.Value && !_adx.IsFormed)
			return false;

		if (_demEnabled.Value && (!demarkerReady || !_demarker.IsFormed))
			return false;

		if (_fbbEnabled.Value && (!bollingerReady || !_bollinger.IsFormed || !forceReady))
			return false;

		if (_mfiEnabled.Value && (!mfiReady || !_mfi.IsFormed))
			return false;

		if (_msEnabled.Value && (!macdReady || !_macd.IsFormed || !stochasticReady || !_stochastic.IsFormed))
			return false;

		_acPrev = _acLast;
		_acLast = currentAc;
		_aoPrev = _aoLast;
		_aoLast = currentAo;

		if (demarkerReady)
			_demLast = currentDemarker;

		if (mfiReady)
			_mfiLast = currentMfi;

		if (forceReady)
			_forceLast = currentForce * 20m;

		if (macdReady)
		{
			_macdMainLast = macdMain;
			_macdSignalLast = macdSignal;
		}

		if (stochasticReady)
		{
			_stochasticKLast = stochasticK;
			_stochasticDLast = stochasticD;
		}

		return true;
	}

	private void CollectSignal(List<SignalDirection> signals, SignalDirection signal)
	{
		if (signal != SignalDirection.None)
			signals.Add(signal);
	}

	private SignalDirection EvaluateAcSignal()
	{
		if (_acLast is not decimal current || _acPrev is not decimal prev)
			return SignalDirection.None;

		var level = _acOpenLevel.Value;

		if (current > level && current > prev)
			return SignalDirection.Buy;

		if (current < -level && current < prev)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection EvaluateAdxSignal()
	{
		if (_adxStrength is not decimal adx || _adxPlus is not decimal plus || _adxMinus is not decimal minus)
			return SignalDirection.None;

		if (adx < _adxTrendLevel.Value)
			return SignalDirection.None;

		if (plus > minus && plus > _adxDirectionalLevel.Value)
			return SignalDirection.Buy;

		if (minus > plus && minus > _adxDirectionalLevel.Value)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection EvaluateAoSignal()
	{
		if (_aoLast is not decimal current || _aoPrev is not decimal prev)
			return SignalDirection.None;

		var level = _aoOpenLevel.Value;

		if (current > level && current > prev)
			return SignalDirection.Buy;

		if (current < -level && current < prev)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection EvaluateDeMarkerSignal()
	{
		if (_demLast is not decimal current)
			return SignalDirection.None;

		var threshold = _demThreshold.Value;
		var lower = 100m - threshold;

		if (current < lower)
			return SignalDirection.Buy;

		if (current > threshold)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection EvaluateForceBollingerSignal(ICandleMessage candle)
	{
		if (_bollingerUpper is not decimal upper || _bollingerLower is not decimal lower)
			return SignalDirection.None;

		if (_forceLast is not decimal force)
			return SignalDirection.None;

		var distance = (upper - lower) / (_pipSize <= 0m ? 1m : _pipSize);
		var distanceFilter = _bandDistanceFilter.Value;

		var acceptDistance = distanceFilter switch
		{
			> 0m => distance >= distanceFilter,
			< 0m => distance <= Math.Abs(distanceFilter),
			_ => true
		};

		if (!acceptDistance)
			return SignalDirection.None;

		var forceLevel = _forceConfirmationLevel.Value;
		var touchedLower = candle.LowPrice <= lower;
		var touchedUpper = candle.HighPrice >= upper;

		if (touchedLower && force > forceLevel)
			return SignalDirection.Buy;

		if (touchedUpper && force < -forceLevel)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection EvaluateMfiSignal()
	{
		if (_mfiLast is not decimal current)
			return SignalDirection.None;

		var threshold = _mfiThreshold.Value;
		var lower = 100m - threshold;

		if (current < lower)
			return SignalDirection.Buy;

		if (current > threshold)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection EvaluateMacdStochasticSignal()
	{
		if (_macdMainLast is not decimal macdMain || _macdSignalLast is not decimal macdSignal)
			return SignalDirection.None;

		if (_stochasticKLast is not decimal stochK || _stochasticDLast is not decimal stochD)
			return SignalDirection.None;

		var macdLevel = _macdLevel.Value;
		var stochasticLevel = _stochasticLevel.Value;
		var lowerStochastic = 100m - stochasticLevel;

		var macdBullish = macdMain > macdLevel && macdMain > macdSignal;
		var macdBearish = macdMain < -macdLevel && macdMain < macdSignal;

		var stochasticBullish = stochK > stochasticLevel && stochK > stochD;
		var stochasticBearish = stochK < lowerStochastic && stochK < stochD;

		if (macdBullish && stochasticBullish)
			return SignalDirection.Buy;

		if (macdBearish && stochasticBearish)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private SignalDirection ResolveConsensus(IReadOnlyCollection<SignalDirection> signals)
	{
		if (signals.Count == 0)
			return SignalDirection.None;

		var minConfirmations = TradeAllStrategies ? Math.Max(1, _requiredConfirmations.Value) : 1;

		var bullish = 0;
		var bearish = 0;

		foreach (var signal in signals)
		{
			if (signal == SignalDirection.Buy)
				bullish++;
			else if (signal == SignalDirection.Sell)
				bearish++;
		}

		if (bullish >= minConfirmations && bearish == 0)
			return SignalDirection.Buy;

		if (bearish >= minConfirmations && bullish == 0)
			return SignalDirection.Sell;

		return SignalDirection.None;
	}

	private void ManagePositions(SignalDirection decision, ICandleMessage candle)
	{
		if (decision == SignalDirection.Buy)
		{
			if (CloseInReverse && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
			{
				BuyMarket(Volume);
				_longEntryPrice = candle.ClosePrice;
				_shortEntryPrice = null;
			}
		}
		else if (decision == SignalDirection.Sell)
		{
			if (CloseInReverse && Position > 0)
				SellMarket(Position);

			if (Position >= 0)
			{
				SellMarket(Volume);
				_shortEntryPrice = candle.ClosePrice;
				_longEntryPrice = null;
			}
		}

		ApplyRiskManagement(candle);
	}

	private void ApplyRiskManagement(ICandleMessage candle)
	{
		if (_pipSize <= 0m)
			return;

		if (Position > 0 && _longEntryPrice is decimal entry)
		{
			var stop = _stopLossPips.Value > 0m ? entry - _stopLossPips.Value * _pipSize : (decimal?)null;
			var take = _takeProfitPips.Value > 0m ? entry + _takeProfitPips.Value * _pipSize : (decimal?)null;

			if (stop is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}
			else if (take is decimal takePrice && candle.HighPrice >= takePrice)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal shortEntry)
		{
			var stop = _stopLossPips.Value > 0m ? shortEntry + _stopLossPips.Value * _pipSize : (decimal?)null;
			var take = _takeProfitPips.Value > 0m ? shortEntry - _takeProfitPips.Value * _pipSize : (decimal?)null;

			if (stop is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_shortEntryPrice = null;
			}
			else if (take is decimal takePrice && candle.LowPrice <= takePrice)
			{
				BuyMarket(Math.Abs(Position));
				_shortEntryPrice = null;
			}
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var digits = GetDecimalDigits(step);
		return digits is 3 or 5 ? step * 10m : step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Floor(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private bool CloseInReverse => _closeInReverse.Value;

	private enum SignalDirection
	{
		None,
		Buy,
		Sell
	}
}