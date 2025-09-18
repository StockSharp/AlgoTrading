using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average breakout strategy that enters long positions after a strong bullish impulse.
/// </summary>
public class MABreakImpulseBuyStrategy : Strategy
{
	private readonly StrategyParam<int> _firstFastPeriod;
	private readonly StrategyParam<int> _firstSlowPeriod;
	private readonly StrategyParam<int> _secondFastPeriod;
	private readonly StrategyParam<int> _secondSlowPeriod;
	private readonly StrategyParam<int> _trendMaPeriod;
	private readonly StrategyParam<int> _breakoutMaPeriod;
	private readonly StrategyParam<int> _quietBarsCount;
	private readonly StrategyParam<decimal> _quietBarsMinRange;
	private readonly StrategyParam<decimal> _impulseStrength;
	private readonly StrategyParam<decimal> _upperWickLimit;
	private readonly StrategyParam<decimal> _lowerWickFloor;
	private readonly StrategyParam<decimal> _candleMinSize;
	private readonly StrategyParam<decimal> _candleMaxSize;
	private readonly StrategyParam<decimal> _volumeSize;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _history = new();

	private decimal? _prevFirstFast;
	private decimal? _prevFirstSlow;
	private decimal? _prevSecondFast;
	private decimal? _prevSecondSlow;
	private decimal? _prevTrendMa;
	private decimal? _prevBreakoutMa;

	/// <summary>
	/// First fast EMA period.
	/// </summary>
	public int FirstFastPeriod
	{
		get => _firstFastPeriod.Value;
		set => _firstFastPeriod.Value = value;
	}

	/// <summary>
	/// First slow EMA period.
	/// </summary>
	public int FirstSlowPeriod
	{
		get => _firstSlowPeriod.Value;
		set => _firstSlowPeriod.Value = value;
	}

	/// <summary>
	/// Second fast EMA period.
	/// </summary>
	public int SecondFastPeriod
	{
		get => _secondFastPeriod.Value;
		set => _secondFastPeriod.Value = value;
	}

	/// <summary>
	/// Second slow EMA period.
	/// </summary>
	public int SecondSlowPeriod
	{
		get => _secondSlowPeriod.Value;
		set => _secondSlowPeriod.Value = value;
	}

	/// <summary>
	/// EMA period that validates the breakout candle open.
	/// </summary>
	public int TrendMaPeriod
	{
		get => _trendMaPeriod.Value;
		set => _trendMaPeriod.Value = value;
	}

	/// <summary>
	/// EMA period that should be touched by the impulse candle low.
	/// </summary>
	public int BreakoutMaPeriod
	{
		get => _breakoutMaPeriod.Value;
		set => _breakoutMaPeriod.Value = value;
	}

	/// <summary>
	/// Number of quiet bars before the impulse.
	/// </summary>
	public int QuietBarsCount
	{
		get => _quietBarsCount.Value;
		set => _quietBarsCount.Value = value;
	}

	/// <summary>
	/// Minimum quiet range in pips.
	/// </summary>
	public decimal QuietBarsMinRange
	{
		get => _quietBarsMinRange.Value;
		set => _quietBarsMinRange.Value = value;
	}

	/// <summary>
	/// Impulse strength multiplier.
	/// </summary>
	public decimal ImpulseStrength
	{
		get => _impulseStrength.Value;
		set => _impulseStrength.Value = value;
	}

	/// <summary>
	/// Maximum upper wick size in percent of the candle range.
	/// </summary>
	public decimal UpperWickLimit
	{
		get => _upperWickLimit.Value;
		set => _upperWickLimit.Value = value;
	}

	/// <summary>
	/// Minimum lower wick size in percent of the candle range.
	/// </summary>
	public decimal LowerWickFloor
	{
		get => _lowerWickFloor.Value;
		set => _lowerWickFloor.Value = value;
	}

	/// <summary>
	/// Minimum candle height in pips.
	/// </summary>
	public decimal CandleMinSize
	{
		get => _candleMinSize.Value;
		set => _candleMinSize.Value = value;
	}

	/// <summary>
	/// Maximum candle height in pips.
	/// </summary>
	public decimal CandleMaxSize
	{
		get => _candleMaxSize.Value;
		set => _candleMaxSize.Value = value;
	}

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal VolumeSize
	{
		get => _volumeSize.Value;
		set => _volumeSize.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MABreakImpulseBuyStrategy"/>.
	/// </summary>
	public MABreakImpulseBuyStrategy()
	{
		_firstFastPeriod = Param(nameof(FirstFastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA 1", "First fast EMA period", "Trend");

		_firstSlowPeriod = Param(nameof(FirstSlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA 1", "First slow EMA period", "Trend");

		_secondFastPeriod = Param(nameof(SecondFastPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA 2", "Second fast EMA period", "Trend");

		_secondSlowPeriod = Param(nameof(SecondSlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA 2", "Second slow EMA period", "Trend");

		_trendMaPeriod = Param(nameof(TrendMaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "EMA that validates the breakout open", "Trend");

		_breakoutMaPeriod = Param(nameof(BreakoutMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Breakout EMA", "EMA that should be touched by the impulse low", "Trend");

		_quietBarsCount = Param(nameof(QuietBarsCount), 2)
			.SetGreaterThanZero()
			.SetDisplay("Quiet Bars", "Number of calm bars before the impulse", "Impulse");

		_quietBarsMinRange = Param(nameof(QuietBarsMinRange), 0m)
			.SetDisplay("Quiet Range (pips)", "Minimum range of quiet bars in pips", "Impulse");

		_impulseStrength = Param(nameof(ImpulseStrength), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Impulse Strength", "Multiplier applied to the quiet range", "Impulse");

		_upperWickLimit = Param(nameof(UpperWickLimit), 100m)
			.SetDisplay("Upper Wick (%)", "Maximum upper wick as percent of range", "Impulse");

		_lowerWickFloor = Param(nameof(LowerWickFloor), 0m)
			.SetDisplay("Lower Wick (%)", "Minimum lower wick as percent of range", "Impulse");

		_candleMinSize = Param(nameof(CandleMinSize), 0m)
			.SetDisplay("Min Candle (pips)", "Minimum height of impulse candle", "Impulse");

		_candleMaxSize = Param(nameof(CandleMaxSize), 100m)
			.SetDisplay("Max Candle (pips)", "Maximum height of impulse candle", "Impulse");

		_volumeSize = Param(nameof(VolumeSize), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume", "Orders");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Orders");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for analysis", "General");
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

		_history.Clear();
		_prevFirstFast = null;
		_prevFirstSlow = null;
		_prevSecondFast = null;
		_prevSecondSlow = null;
		_prevTrendMa = null;
		_prevBreakoutMa = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 0m;
		var stopDistance = StopLossPips > 0m && priceStep > 0m ? StopLossPips * priceStep : 0m;
		var takeDistance = TakeProfitPips > 0m && priceStep > 0m ? TakeProfitPips * priceStep : 0m;

		if (stopDistance > 0m || takeDistance > 0m)
		{
			StartProtection(
				takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : default,
				stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : default);
		}
		else
		{
			StartProtection();
		}

		var firstFast = new ExponentialMovingAverage { Length = FirstFastPeriod };
		var firstSlow = new ExponentialMovingAverage { Length = FirstSlowPeriod };
		var secondFast = new ExponentialMovingAverage { Length = SecondFastPeriod };
		var secondSlow = new ExponentialMovingAverage { Length = SecondSlowPeriod };
		var trendMa = new ExponentialMovingAverage { Length = TrendMaPeriod };
		var breakoutMa = new ExponentialMovingAverage { Length = BreakoutMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal firstFast, decimal firstSlow, decimal secondFast, decimal secondSlow, decimal trendMa, decimal breakoutMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var clone = (ICandleMessage)candle.Clone();
		_history.Add(clone);

		var capacity = Math.Max(QuietBarsCount + 3, 6);
		while (_history.Count > capacity)
		{
			_history.RemoveAt(0);
		}

		if (_prevFirstFast is null || _prevFirstSlow is null || _prevSecondFast is null || _prevSecondSlow is null || _prevTrendMa is null || _prevBreakoutMa is null)
		{
			UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
			return;
		}

		if (Position != 0)
		{
			UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
			return;
		}

		if (!IsTrendAligned())
		{
			UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
			return;
		}

		if (!TryGetImpulse(out var impulse, out var quietRange))
		{
			UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
			return;
		}

		var pip = Security?.PriceStep ?? 0m;
		if (pip <= 0m)
		{
			pip = 1m;
		}

		if (!ValidateImpulse(impulse, quietRange, pip, _prevTrendMa.Value, _prevBreakoutMa.Value))
		{
			UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
			return;
		}

		var volume = NormalizeVolume(VolumeSize);
		if (volume > 0m)
		{
			BuyMarket(volume);
		}

		UpdateIndicatorState(firstFast, firstSlow, secondFast, secondSlow, trendMa, breakoutMa);
	}

	private bool IsTrendAligned()
	{
		if (_prevFirstFast is null || _prevFirstSlow is null || _prevSecondFast is null || _prevSecondSlow is null)
			return false;

		return _prevFirstFast.Value > _prevFirstSlow.Value && _prevSecondFast.Value > _prevSecondSlow.Value;
	}

	private bool TryGetImpulse(out ICandleMessage impulse, out decimal quietRange)
	{
		impulse = null;
		quietRange = 0m;

		var quietBars = QuietBarsCount;
		if (quietBars < 0)
			return false;

		var required = quietBars + 2;
		if (_history.Count < required)
			return false;

		impulse = _history[^2];

		for (var i = 0; i < quietBars; i++)
		{
			var index = _history.Count - 3 - i;
			if (index < 0)
				return false;

			var bar = _history[index];
			var range = bar.HighPrice - bar.LowPrice;
			if (range > quietRange)
			{
				quietRange = range;
			}
		}

		return true;
	}

	private bool ValidateImpulse(ICandleMessage impulse, decimal quietRange, decimal pip, decimal trendMa, decimal breakoutMa)
	{
		var range = impulse.HighPrice - impulse.LowPrice;
		if (range <= 0m)
			return false;

		if (impulse.ClosePrice <= impulse.OpenPrice)
			return false;

		if (impulse.OpenPrice <= trendMa)
			return false;

		if (impulse.LowPrice > breakoutMa)
			return false;

		if (CandleMinSize > 0m && range < CandleMinSize * pip)
			return false;

		if (CandleMaxSize > 0m && range > CandleMaxSize * pip)
			return false;

		var minQuiet = QuietBarsMinRange > 0m ? QuietBarsMinRange * pip : 0m;
		if (quietRange <= minQuiet)
			return false;

		var body = impulse.ClosePrice - impulse.OpenPrice;
		if (body < quietRange * ImpulseStrength)
			return false;

		var upperWick = impulse.HighPrice - impulse.ClosePrice;
		var lowerWick = impulse.OpenPrice - impulse.LowPrice;

		if (UpperWickLimit < 100m)
		{
			var maxUpper = range * UpperWickLimit / 100m;
			if (upperWick > maxUpper)
				return false;
		}

		if (LowerWickFloor > 0m)
		{
			var minLower = range * LowerWickFloor / 100m;
			if (lowerWick < minLower)
				return false;
		}

		return true;
	}

	private void UpdateIndicatorState(decimal firstFast, decimal firstSlow, decimal secondFast, decimal secondSlow, decimal trendMa, decimal breakoutMa)
	{
		_prevFirstFast = firstFast;
		_prevFirstSlow = firstSlow;
		_prevSecondFast = secondFast;
		_prevSecondSlow = secondSlow;
		_prevTrendMa = trendMa;
		_prevBreakoutMa = breakoutMa;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
		}

		var minVolume = Security?.MinVolume;
		if (minVolume != null && minVolume.Value > 0m && volume < minVolume.Value)
		{
			volume = minVolume.Value;
		}

		var maxVolume = Security?.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
		{
			volume = maxVolume.Value;
		}

		return volume;
	}
}
