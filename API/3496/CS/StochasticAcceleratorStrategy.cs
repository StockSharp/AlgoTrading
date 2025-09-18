namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stochastic Accelerator strategy converted from the MetaTrader expert "#2 stoch mt5".
/// Combines three stochastic oscillators with the Accelerator and Awesome oscillators
/// to open long positions when momentum confirms bullish pressure and short positions
/// when bearish criteria align. Positions are closed when the Awesome Oscillator crosses
/// back through a configurable threshold.
/// </summary>
public class StochasticAcceleratorStrategy : Strategy
{
	private const decimal Epsilon = 0.000001m; // Same tolerance used by the original expert to avoid equality issues.

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private readonly StrategyParam<int> _signalKPeriod;
	private readonly StrategyParam<int> _signalDPeriod;
	private readonly StrategyParam<int> _signalSlowing;

	private readonly StrategyParam<int> _entryKPeriod;
	private readonly StrategyParam<int> _entryDPeriod;
	private readonly StrategyParam<int> _entrySlowing;
	private readonly StrategyParam<decimal> _entryLevel;

	private readonly StrategyParam<int> _filterKPeriod;
	private readonly StrategyParam<int> _filterDPeriod;
	private readonly StrategyParam<int> _filterSlowing;
	private readonly StrategyParam<decimal> _filterLevel;

	private readonly StrategyParam<decimal> _acceleratorLevel;
	private readonly StrategyParam<decimal> _awesomeLevel;

	private StochasticOscillator _signalStochastic = null!;
	private StochasticOscillator _entryStochastic = null!;
	private StochasticOscillator _filterStochastic = null!;
	private AcceleratorOscillator _accelerator = null!;
	private AwesomeOscillator _awesome = null!;

	private decimal _pipSize;
	private decimal? _previousAccelerator;
	private decimal? _previousAwesome;

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticAcceleratorStrategy"/> class.
	/// </summary>
	public StochasticAcceleratorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series processed by the strategy.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Market volume used for every entry.", "Trading")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in MetaTrader pips.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 70m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Profit target distance expressed in MetaTrader pips.", "Risk")
			.SetCanOptimize(true);

		_signalKPeriod = Param(nameof(SignalKPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Signal %K", "Base period of the confirmation stochastic.", "Stochastic #1")
			.SetCanOptimize(true);

		_signalDPeriod = Param(nameof(SignalDPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal %D", "Signal smoothing period for the confirmation stochastic.", "Stochastic #1")
			.SetCanOptimize(true);

		_signalSlowing = Param(nameof(SignalSlowing), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Slowing", "Additional smoothing applied to the confirmation stochastic.", "Stochastic #1")
			.SetCanOptimize(true);

		_entryKPeriod = Param(nameof(EntryKPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Entry %K", "Base period for the overbought / oversold filter.", "Stochastic #2")
			.SetCanOptimize(true);

		_entryDPeriod = Param(nameof(EntryDPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry %D", "Signal smoothing for the overbought / oversold filter.", "Stochastic #2")
			.SetCanOptimize(true);

		_entrySlowing = Param(nameof(EntrySlowing), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry Slowing", "Additional smoothing for the overbought / oversold filter.", "Stochastic #2")
			.SetCanOptimize(true);

		_entryLevel = Param(nameof(EntryLevel), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Entry Level", "Lower threshold confirming bullish momentum; the bearish threshold uses 100 - level.", "Stochastic #2")
			.SetCanOptimize(true);

		_filterKPeriod = Param(nameof(FilterKPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Filter %K", "Base period for the upper stochastic band filter.", "Stochastic #3")
			.SetCanOptimize(true);

		_filterDPeriod = Param(nameof(FilterDPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Filter %D", "Signal smoothing for the upper stochastic band filter.", "Stochastic #3")
			.SetCanOptimize(true);

		_filterSlowing = Param(nameof(FilterSlowing), 10)
			.SetGreaterThanZero()
			.SetDisplay("Filter Slowing", "Additional smoothing for the upper stochastic band filter.", "Stochastic #3")
			.SetCanOptimize(true);

		_filterLevel = Param(nameof(FilterLevel), 75m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Filter Level", "Upper threshold limiting bullish setups; the bearish threshold uses 100 - level.", "Stochastic #3")
			.SetCanOptimize(true);

		_acceleratorLevel = Param(nameof(AcceleratorLevel), 0.0002m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Accelerator Level", "Minimum Accelerator Oscillator amplitude required for entries.", "Momentum")
			.SetCanOptimize(true);

		_awesomeLevel = Param(nameof(AwesomeLevel), 0.0013m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Awesome Level", "Threshold used to close positions when the Awesome Oscillator reverts.", "Momentum")
			.SetCanOptimize(true);

		_pipSize = 0m;
		_previousAccelerator = null;
		_previousAwesome = null;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Default market volume used for new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in MetaTrader pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in MetaTrader pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Base %K period for the confirmation stochastic.
	/// </summary>
	public int SignalKPeriod
	{
		get => _signalKPeriod.Value;
		set => _signalKPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing period for the confirmation stochastic.
	/// </summary>
	public int SignalDPeriod
	{
		get => _signalDPeriod.Value;
		set => _signalDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to the confirmation stochastic.
	/// </summary>
	public int SignalSlowing
	{
		get => _signalSlowing.Value;
		set => _signalSlowing.Value = value;
	}

	/// <summary>
	/// Base %K period for the overbought / oversold stochastic.
	/// </summary>
	public int EntryKPeriod
	{
		get => _entryKPeriod.Value;
		set => _entryKPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing for the overbought / oversold stochastic.
	/// </summary>
	public int EntryDPeriod
	{
		get => _entryDPeriod.Value;
		set => _entryDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing for the overbought / oversold stochastic.
	/// </summary>
	public int EntrySlowing
	{
		get => _entrySlowing.Value;
		set => _entrySlowing.Value = value;
	}

	/// <summary>
	/// Lower threshold used to validate bullish setups.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// Base %K period for the upper-band stochastic filter.
	/// </summary>
	public int FilterKPeriod
	{
		get => _filterKPeriod.Value;
		set => _filterKPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing for the upper-band stochastic filter.
	/// </summary>
	public int FilterDPeriod
	{
		get => _filterDPeriod.Value;
		set => _filterDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing for the upper-band stochastic filter.
	/// </summary>
	public int FilterSlowing
	{
		get => _filterSlowing.Value;
		set => _filterSlowing.Value = value;
	}

	/// <summary>
	/// Upper threshold limiting bullish setups.
	/// </summary>
	public decimal FilterLevel
	{
		get => _filterLevel.Value;
		set => _filterLevel.Value = value;
	}

	/// <summary>
	/// Minimum Accelerator Oscillator amplitude required for entries.
	/// </summary>
	public decimal AcceleratorLevel
	{
		get => _acceleratorLevel.Value;
		set => _acceleratorLevel.Value = value;
	}

	/// <summary>
	/// Awesome Oscillator threshold used to close positions.
	/// </summary>
	public decimal AwesomeLevel
	{
		get => _awesomeLevel.Value;
		set => _awesomeLevel.Value = value;
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

		_pipSize = 0m;
		_previousAccelerator = null;
		_previousAwesome = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align helper order methods with the configured trade volume.

		_pipSize = CalculatePipSize();

		var stopLoss = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;
		var takeProfit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;

		if (stopLoss != null || takeProfit != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);

		_signalStochastic = new StochasticOscillator
		{
			Length = SignalKPeriod,
			K = { Length = SignalSlowing },
			D = { Length = SignalDPeriod },
		};

		_entryStochastic = new StochasticOscillator
		{
			Length = EntryKPeriod,
			K = { Length = EntrySlowing },
			D = { Length = EntryDPeriod },
		};

		_filterStochastic = new StochasticOscillator
		{
			Length = FilterKPeriod,
			K = { Length = FilterSlowing },
			D = { Length = FilterDPeriod },
		};

		_accelerator = new AcceleratorOscillator();
		_awesome = new AwesomeOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_signalStochastic, _entryStochastic, _filterStochastic, _accelerator, _awesome, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _signalStochastic);
			DrawIndicator(area, _entryStochastic);
			DrawIndicator(area, _filterStochastic);
			DrawOwnTrades(area);

			var oscillatorArea = CreateChartArea();
			if (oscillatorArea != null)
			{
				DrawIndicator(oscillatorArea, _accelerator);
				DrawIndicator(oscillatorArea, _awesome);
			}
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue signalValue,
		IIndicatorValue entryValue,
		IIndicatorValue filterValue,
		IIndicatorValue acceleratorValue,
		IIndicatorValue awesomeValue)
	{
		if (candle.State != CandleStates.Finished)
			return; // Mimic the MetaTrader EA, which processes only completed candles.

		if (!signalValue.IsFinal || !entryValue.IsFinal || !filterValue.IsFinal || !acceleratorValue.IsFinal || !awesomeValue.IsFinal)
			return; // Wait until every indicator produces a final value for the closed candle.

		var signal = (StochasticOscillatorValue)signalValue;
		var entry = (StochasticOscillatorValue)entryValue;
		var filter = (StochasticOscillatorValue)filterValue;

		if (signal.K is not decimal signalK || signal.D is not decimal signalD)
			return;

		if (entry.K is not decimal entryK)
			return;

		if (filter.K is not decimal filterK)
			return;

		if (!acceleratorValue.TryGetValue(out decimal accelerator) || !awesomeValue.TryGetValue(out decimal awesome))
			return;

		var hasPrevAccelerator = _previousAccelerator is decimal prevAccelerator;
		var hasPrevAwesome = _previousAwesome is decimal prevAwesome;

		var signalLong = signalK > signalD + Epsilon;
		var signalShort = signalK < signalD - Epsilon;

		var entryLong = entryK > EntryLevel + Epsilon;
		var entryShort = entryK < 100m - EntryLevel - Epsilon;

		var filterLong = filterK < FilterLevel - Epsilon;
		var filterShort = filterK > 100m - FilterLevel + Epsilon;

		var acceleratorLong = hasPrevAccelerator && CrossAbove(prevAccelerator, accelerator, AcceleratorLevel);
		var acceleratorShort = hasPrevAccelerator && CrossBelow(prevAccelerator, accelerator, -AcceleratorLevel);

		var closeLong = hasPrevAwesome && CrossBelow(prevAwesome, awesome, AwesomeLevel);
		var closeShort = hasPrevAwesome && CrossAbove(prevAwesome, awesome, -AwesomeLevel);

		if (closeLong && Position > 0m)
			SellMarket(Position); // Close the existing long position before considering a new entry.
		else if (closeShort && Position < 0m)
			BuyMarket(Math.Abs(Position)); // Close the existing short position before considering a new entry.

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousAccelerator = accelerator;
			_previousAwesome = awesome;
			return;
		}

		var canOpenLong = signalLong && entryLong && filterLong && acceleratorLong;
		var canOpenShort = signalShort && entryShort && filterShort && acceleratorShort;

		if (canOpenLong && canOpenShort)
		{
			_previousAccelerator = accelerator;
			_previousAwesome = awesome;
			return; // Conflicting signals; stay flat.
		}

		if (Position == 0m)
		{
			if (canOpenLong)
			{
				EnterLong();
			}
			else if (canOpenShort)
			{
				EnterShort();
			}
		}

		_previousAccelerator = accelerator;
		_previousAwesome = awesome;
	}

	private void EnterLong()
	{
		var volume = TradeVolume;

		if (Position < 0m)
			volume += Math.Abs(Position); // Combine closing the short with the new long as in the MetaTrader implementation.

		volume = AdjustVolume(volume);

		if (volume <= 0m)
			return;

		BuyMarket(volume);
	}

	private void EnterShort()
	{
		var volume = TradeVolume;

		if (Position > 0m)
			volume += Position; // Combine closing the long with the new short as in the MetaTrader implementation.

		volume = AdjustVolume(volume);

		if (volume <= 0m)
			return;

		SellMarket(volume);
	}

	private static bool CrossAbove(decimal previous, decimal current, decimal level)
	{
		return previous <= level - Epsilon && current >= level + Epsilon;
	}

	private static bool CrossBelow(decimal previous, decimal current, decimal level)
	{
		return previous >= level + Epsilon && current <= level - Epsilon;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;

		if (security == null)
			return 1m;

		var priceStep = security.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep; // Match the MetaTrader pip definition for Forex symbols.
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;

		if (security == null)
			return volume;

		var step = security.VolumeStep;

		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
			volume = steps * step.Value;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}
}
