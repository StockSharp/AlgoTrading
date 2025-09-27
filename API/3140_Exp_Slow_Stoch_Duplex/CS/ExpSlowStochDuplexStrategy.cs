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
/// Dual slow stochastic crossover strategy converted from the MetaTrader 5 expert advisor "Exp_Slow-Stoch_Duplex".
/// The strategy monitors two stochastic oscillators on independent timeframes and coordinates long and short signals.
/// </summary>
public class ExpSlowStochDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longKPeriod;
	private readonly StrategyParam<int> _longDPeriod;
	private readonly StrategyParam<int> _longSlowing;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<SmoothingMethod> _longSmoothingMethod;
	private readonly StrategyParam<int> _longSmoothingLength;
	private readonly StrategyParam<bool> _longEnableOpen;
	private readonly StrategyParam<bool> _longEnableClose;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortKPeriod;
	private readonly StrategyParam<int> _shortDPeriod;
	private readonly StrategyParam<int> _shortSlowing;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<SmoothingMethod> _shortSmoothingMethod;
	private readonly StrategyParam<int> _shortSmoothingLength;
	private readonly StrategyParam<bool> _shortEnableOpen;
	private readonly StrategyParam<bool> _shortEnableClose;

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private StochasticOscillator _longStochastic;
	private StochasticOscillator _shortStochastic;
	private LengthIndicator<decimal> _longKSmoother;
	private LengthIndicator<decimal> _longDSmoother;
	private LengthIndicator<decimal> _shortKSmoother;
	private LengthIndicator<decimal> _shortDSmoother;

	private decimal?[] _longKHistory = Array.Empty<decimal?>();
	private decimal?[] _longDHistory = Array.Empty<decimal?>();
	private decimal?[] _shortKHistory = Array.Empty<decimal?>();
	private decimal?[] _shortDHistory = Array.Empty<decimal?>();

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSlowStochDuplexStrategy"/> class.
	/// </summary>
	public ExpSlowStochDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Long Timeframe", "Timeframe used by the long stochastic oscillator", "General");

		_longKPeriod = Param(nameof(LongKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long %K", "%K calculation period for the long stochastic", "Indicators");

		_longDPeriod = Param(nameof(LongDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long %D", "%D smoothing period for the long stochastic", "Indicators");

		_longSlowing = Param(nameof(LongSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long Slowing", "Additional smoothing for the long stochastic", "Indicators");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Long Signal Bar", "Shift in bars used to detect the crossover for longs", "Logic");

		_longSmoothingMethod = Param(nameof(LongSmoothingMethod), SmoothingMethod.Smoothed)
			.SetDisplay("Long Smoothing", "Post-processing method applied to %K and %D", "Indicators");

		_longSmoothingLength = Param(nameof(LongSmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long Smoothing Length", "Length of the secondary smoothing applied to %K/%D", "Indicators");

		_longEnableOpen = Param(nameof(LongEnableOpen), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_longEnableClose = Param(nameof(LongEnableClose), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Short Timeframe", "Timeframe used by the short stochastic oscillator", "General");

		_shortKPeriod = Param(nameof(ShortKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short %K", "%K calculation period for the short stochastic", "Indicators");

		_shortDPeriod = Param(nameof(ShortDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short %D", "%D smoothing period for the short stochastic", "Indicators");

		_shortSlowing = Param(nameof(ShortSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short Slowing", "Additional smoothing for the short stochastic", "Indicators");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Short Signal Bar", "Shift in bars used to detect the crossover for shorts", "Logic");

		_shortSmoothingMethod = Param(nameof(ShortSmoothingMethod), SmoothingMethod.Smoothed)
			.SetDisplay("Short Smoothing", "Post-processing method applied to %K and %D", "Indicators");

		_shortSmoothingLength = Param(nameof(ShortSmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Smoothing Length", "Length of the secondary smoothing applied to %K/%D", "Indicators");

		_shortEnableOpen = Param(nameof(ShortEnableOpen), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_shortEnableClose = Param(nameof(ShortEnableClose), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume submitted on each entry", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Protective take profit distance in price points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protective stop loss distance in price points", "Risk");
	}

	/// <summary>
	/// Candle type used for the long stochastic oscillator.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// %K period for the long stochastic.
	/// </summary>
	public int LongKPeriod
	{
		get => _longKPeriod.Value;
		set => _longKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the long stochastic.
	/// </summary>
	public int LongDPeriod
	{
		get => _longDPeriod.Value;
		set => _longDPeriod.Value = value;
	}

	/// <summary>
	/// Slowing period for the long stochastic.
	/// </summary>
	public int LongSlowing
	{
		get => _longSlowing.Value;
		set => _longSlowing.Value = value;
	}

	/// <summary>
	/// Bar offset used to evaluate long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the long stochastic output.
	/// </summary>
	public SmoothingMethod LongSmoothingMethod
	{
		get => _longSmoothingMethod.Value;
		set => _longSmoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the secondary smoothing applied to the long stochastic.
	/// </summary>
	public int LongSmoothingLength
	{
		get => _longSmoothingLength.Value;
		set => _longSmoothingLength.Value = value;
	}

	/// <summary>
	/// Enables long entries when true.
	/// </summary>
	public bool LongEnableOpen
	{
		get => _longEnableOpen.Value;
		set => _longEnableOpen.Value = value;
	}

	/// <summary>
	/// Enables long exits when true.
	/// </summary>
	public bool LongEnableClose
	{
		get => _longEnableClose.Value;
		set => _longEnableClose.Value = value;
	}

	/// <summary>
	/// Candle type used for the short stochastic oscillator.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// %K period for the short stochastic.
	/// </summary>
	public int ShortKPeriod
	{
		get => _shortKPeriod.Value;
		set => _shortKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the short stochastic.
	/// </summary>
	public int ShortDPeriod
	{
		get => _shortDPeriod.Value;
		set => _shortDPeriod.Value = value;
	}

	/// <summary>
	/// Slowing period for the short stochastic.
	/// </summary>
	public int ShortSlowing
	{
		get => _shortSlowing.Value;
		set => _shortSlowing.Value = value;
	}

	/// <summary>
	/// Bar offset used to evaluate short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the short stochastic output.
	/// </summary>
	public SmoothingMethod ShortSmoothingMethod
	{
		get => _shortSmoothingMethod.Value;
		set => _shortSmoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the secondary smoothing applied to the short stochastic.
	/// </summary>
	public int ShortSmoothingLength
	{
		get => _shortSmoothingLength.Value;
		set => _shortSmoothingLength.Value = value;
	}

	/// <summary>
	/// Enables short entries when true.
	/// </summary>
	public bool ShortEnableOpen
	{
		get => _shortEnableOpen.Value;
		set => _shortEnableOpen.Value = value;
	}

	/// <summary>
	/// Enables short exits when true.
	/// </summary>
	public bool ShortEnableClose
	{
		get => _shortEnableClose.Value;
		set => _shortEnableClose.Value = value;
	}

	/// <summary>
	/// Volume submitted when a new position is opened.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, LongCandleType);

		if (ShortCandleType != LongCandleType)
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longStochastic = null;
		_shortStochastic = null;
		_longKSmoother = null;
		_longDSmoother = null;
		_shortKSmoother = null;
		_shortDSmoother = null;

		_longKHistory = Array.Empty<decimal?>();
		_longDHistory = Array.Empty<decimal?>();
		_shortKHistory = Array.Empty<decimal?>();
		_shortDHistory = Array.Empty<decimal?>();

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_longStochastic = new StochasticOscillator
		{
			KPeriod = LongKPeriod,
			DPeriod = LongDPeriod,
			Smooth = LongSlowing,
		};

		_shortStochastic = new StochasticOscillator
		{
			KPeriod = ShortKPeriod,
			DPeriod = ShortDPeriod,
			Smooth = ShortSlowing,
		};

		_longKSmoother = CreateSmoother(LongSmoothingMethod, LongSmoothingLength);
		_longDSmoother = CreateSmoother(LongSmoothingMethod, LongSmoothingLength);
		_shortKSmoother = CreateSmoother(ShortSmoothingMethod, ShortSmoothingLength);
		_shortDSmoother = CreateSmoother(ShortSmoothingMethod, ShortSmoothingLength);

		_longKHistory = new decimal?[Math.Max(LongSignalBar + 2, 2)];
		_longDHistory = new decimal?[Math.Max(LongSignalBar + 2, 2)];
		_shortKHistory = new decimal?[Math.Max(ShortSignalBar + 2, 2)];
		_shortDHistory = new decimal?[Math.Max(ShortSignalBar + 2, 2)];

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
			.BindEx(_longStochastic, ProcessLongStochastic)
			.Start();

		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
			.BindEx(_shortStochastic, ProcessShortStochastic)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Point),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, longSubscription);
			DrawIndicator(area, _longStochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLongStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		if (indicatorValue is not StochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal rawK || stoch.D is not decimal rawD)
			return;

		var smoothedK = ApplySmoothing(_longKSmoother, rawK, candle);
		var smoothedD = ApplySmoothing(_longDSmoother, rawD, candle);

		if (smoothedK is not decimal k || smoothedD is not decimal d)
			return;

		UpdateHistory(_longKHistory, k);
		UpdateHistory(_longDHistory, d);

		if (!TryGetPair(_longKHistory, LongSignalBar, out var currentK, out var previousK))
			return;

		if (!TryGetPair(_longDHistory, LongSignalBar, out var currentD, out var previousD))
			return;

		var openSignal = LongEnableOpen && previousK <= previousD && currentK > currentD;
		var closeSignal = LongEnableClose && currentK < currentD;

		if (closeSignal && Position > 0)
		{
			SellMarket(Position);
			LogInfo($"Closing long position because %K ({currentK:F2}) crossed below %D ({currentD:F2}) on the long timeframe.");
		}

		if (openSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Opening long position because %K ({currentK:F2}) crossed above %D ({currentD:F2}) on the long timeframe.");
			}
		}
	}

	private void ProcessShortStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		if (indicatorValue is not StochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal rawK || stoch.D is not decimal rawD)
			return;

		var smoothedK = ApplySmoothing(_shortKSmoother, rawK, candle);
		var smoothedD = ApplySmoothing(_shortDSmoother, rawD, candle);

		if (smoothedK is not decimal k || smoothedD is not decimal d)
			return;

		UpdateHistory(_shortKHistory, k);
		UpdateHistory(_shortDHistory, d);

		if (!TryGetPair(_shortKHistory, ShortSignalBar, out var currentK, out var previousK))
			return;

		if (!TryGetPair(_shortDHistory, ShortSignalBar, out var currentD, out var previousD))
			return;

		var openSignal = ShortEnableOpen && previousK >= previousD && currentK < currentD;
		var closeSignal = ShortEnableClose && currentK > currentD;

		if (closeSignal && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Closing short position because %K ({currentK:F2}) crossed above %D ({currentD:F2}) on the short timeframe.");
		}

		if (openSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Opening short position because %K ({currentK:F2}) crossed below %D ({currentD:F2}) on the short timeframe.");
			}
		}
	}

	private static LengthIndicator<decimal> CreateSmoother(SmoothingMethod method, int length)
	{
		if (method == SmoothingMethod.None || length <= 1)
			return null;

		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			_ => null,
		};
	}

	private static decimal? ApplySmoothing(LengthIndicator<decimal> smoother, decimal value, ICandleMessage candle)
	{
		if (smoother == null)
			return value;

		var indicatorValue = smoother.Process(new DecimalIndicatorValue(smoother, value, candle.OpenTime));
		return indicatorValue.ToNullableDecimal();
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
			return;

		for (var index = buffer.Length - 1; index > 0; index--)
			buffer[index] = buffer[index - 1];

		buffer[0] = value;
	}

	private static bool TryGetPair(decimal?[] buffer, int shift, out decimal current, out decimal previous)
	{
		current = 0m;
		previous = 0m;

		var currentIndex = shift;
		var previousIndex = shift + 1;

		if (buffer.Length <= previousIndex)
			return false;

		if (buffer[currentIndex] is not decimal currentValue)
			return false;

		if (buffer[previousIndex] is not decimal previousValue)
			return false;

		current = currentValue;
		previous = previousValue;
		return true;
	}

	/// <summary>
	/// Available smoothing methods for the secondary averaging stage.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>
		/// No additional smoothing.
		/// </summary>
		None,

		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted,
	}
}

