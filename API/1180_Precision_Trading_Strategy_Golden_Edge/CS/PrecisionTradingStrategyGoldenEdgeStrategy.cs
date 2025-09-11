using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Precision Trading Strategy: Golden Edge.
/// Aligns EMA cross with HMA trend and volatility filter.
/// </summary>
public class PrecisionTradingStrategyGoldenEdgeStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _hmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<decimal> _rangeFilterMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private HullMovingAverage _hma = null!;
	private RelativeStrengthIndex _rsi = null!;
	private AverageTrueRange _atr = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevHma;
	private bool _wasFastBelowSlow;
	private bool _initialized;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// Hull moving average length.
	/// </summary>
	public int HmaLength { get => _hmaLength.Value; set => _hmaLength.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Minimum ATR threshold.
	/// </summary>
	public decimal AtrThreshold { get => _atrThreshold.Value; set => _atrThreshold.Value = value; }

	/// <summary>
	/// ATR multiplier for range filter.
	/// </summary>
	public decimal RangeFilterMultiplier { get => _rangeFilterMultiplier.Value; set => _rangeFilterMultiplier.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PrecisionTradingStrategyGoldenEdgeStrategy"/> class.
	/// </summary>
	public PrecisionTradingStrategyGoldenEdgeStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 3)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
			.SetCanOptimize(true);
		_slowEmaLength = Param(nameof(SlowEmaLength), 33)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
			.SetCanOptimize(true);
		_hmaLength = Param(nameof(HmaLength), 66)
			.SetDisplay("HMA Length", "Hull MA period", "Indicators")
			.SetCanOptimize(true);
		_rsiLength = Param(nameof(RsiLength), 12)
			.SetDisplay("RSI Length", "RSI period", "Indicators")
			.SetCanOptimize(true);
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Indicators")
			.SetCanOptimize(true);
		_atrThreshold = Param(nameof(AtrThreshold), 0.1m)
			.SetDisplay("ATR Threshold", "Minimum ATR", "Filters")
			.SetCanOptimize(true);
		_rangeFilterMultiplier = Param(nameof(RangeFilterMultiplier), 0.5m)
			.SetDisplay("Range Multiplier", "ATR multiplier for range filter", "Filters")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_hma = new HullMovingAverage { Length = HmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_highest = new Highest { Length = AtrLength };
		_lowest = new Lowest { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _hma, _rsi, _atr, _highest, _lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal hma, decimal rsi, decimal atr, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized && _fastEma.IsFormed && _slowEma.IsFormed && _hma.IsFormed && _rsi.IsFormed && _atr.IsFormed && _highest.IsFormed && _lowest.IsFormed)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevHma = hma;
			_wasFastBelowSlow = fast < slow;
			_initialized = true;
			return;
		}

		if (!_initialized)
			return;

		var isFastBelowSlow = fast < slow;
		var crossedUp = _wasFastBelowSlow && !isFastBelowSlow;
		var crossedDown = !_wasFastBelowSlow && isFastBelowSlow;

		var trendUp = hma > _prevHma;
		var trendDown = hma < _prevHma;

		var rangeOk = (highest - lowest) > atr * RangeFilterMultiplier && atr > AtrThreshold;

		if (rangeOk)
		{
			if (crossedUp && rsi > 55 && trendUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossedDown && rsi < 45 && trendDown && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_wasFastBelowSlow = isFastBelowSlow;
		_prevHma = hma;
	}
}

