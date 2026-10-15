using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PercentX trend follower.
/// The oscillator measures the distance of the close from the middle of a Keltner or Bollinger band (MaLength, width TrendMultiplier)
/// in percent of the half band width. Its highest and lowest values over LoopbackPeriod form the extremes, and the dynamic range is
/// the lowest of those highs and the highest of those lows over OuterLoopback. Crossing above the upper range goes long, crossing below
/// the lower range goes short, reversing an opposite position. With UseInitialStop an ATR stop of ReverseMultiplier * ATR is placed
/// from the entry close.
/// </summary>
public class PercentXTrendFollowerStrategy : Strategy
{
	/// <summary>
	/// Band used to normalize price.
	/// </summary>
	public enum BandTypes
	{
		/// <summary>
		/// Keltner channel: EMA middle, ATR width.
		/// </summary>
		Keltner,

		/// <summary>
		/// Bollinger bands: SMA middle, standard deviation width.
		/// </summary>
		Bollinger
	}

	private readonly StrategyParam<BandTypes> _bandType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _loopbackPeriod;
	private readonly StrategyParam<int> _outerLoopback;
	private readonly StrategyParam<bool> _useInitialStop;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trendMultiplier;
	private readonly StrategyParam<decimal> _reverseMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _oscHighest = null!;
	private Lowest _oscLowest = null!;
	private Lowest _upperRange = null!;
	private Highest _lowerRange = null!;

	private decimal? _prevOsc;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _stopPrice;

	/// <summary>
	/// Band used to normalize price.
	/// </summary>
	public BandTypes BandType
	{
		get => _bandType.Value;
		set => _bandType.Value = value;
	}

	/// <summary>
	/// Period of the band middle line and width.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Bars for the oscillator highest and lowest values.
	/// </summary>
	public int LoopbackPeriod
	{
		get => _loopbackPeriod.Value;
		set => _loopbackPeriod.Value = value;
	}

	/// <summary>
	/// Bars for the dynamic range built from the extremes.
	/// </summary>
	public int OuterLoopback
	{
		get => _outerLoopback.Value;
		set => _outerLoopback.Value = value;
	}

	/// <summary>
	/// Use the initial ATR stop.
	/// </summary>
	public bool UseInitialStop
	{
		get => _useInitialStop.Value;
		set => _useInitialStop.Value = value;
	}

	/// <summary>
	/// ATR period for the stop.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Band width multiplier.
	/// </summary>
	public decimal TrendMultiplier
	{
		get => _trendMultiplier.Value;
		set => _trendMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the initial stop.
	/// </summary>
	public decimal ReverseMultiplier
	{
		get => _reverseMultiplier.Value;
		set => _reverseMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public PercentXTrendFollowerStrategy()
	{
		_bandType = Param(nameof(BandType), BandTypes.Keltner)
			.SetDisplay("Band Type", "Band used to normalize price", "Indicators");

		_maLength = Param(nameof(MaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Period of the band middle line and width", "Indicators");

		_loopbackPeriod = Param(nameof(LoopbackPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Loopback Period", "Bars for the oscillator extremes", "Indicators");

		_outerLoopback = Param(nameof(OuterLoopback), 80)
			.SetGreaterThanZero()
			.SetDisplay("Outer Loopback", "Bars for the dynamic range", "Indicators");

		_useInitialStop = Param(nameof(UseInitialStop), true)
			.SetDisplay("Use Initial Stop", "Use the initial ATR stop", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for the stop", "Risk");

		_trendMultiplier = Param(nameof(TrendMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trend Multiplier", "Band width multiplier", "Indicators");

		_reverseMultiplier = Param(nameof(ReverseMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Reverse Multiplier", "ATR multiplier of the initial stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevOsc = null;
		_prevUpper = null;
		_prevLower = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		IIndicator middle;
		IIndicator width;

		if (BandType == BandTypes.Keltner)
		{
			middle = new ExponentialMovingAverage { Length = MaLength };
			width = new AverageTrueRange { Length = MaLength };
		}
		else
		{
			middle = new SimpleMovingAverage { Length = MaLength };
			width = new StandardDeviation { Length = MaLength };
		}

		var atr = new AverageTrueRange { Length = AtrLength };

		_oscHighest = new Highest { Length = LoopbackPeriod };
		_oscLowest = new Lowest { Length = LoopbackPeriod };
		_upperRange = new Lowest { Length = OuterLoopback };
		_lowerRange = new Highest { Length = OuterLoopback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(middle, width, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, middle);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal width, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var halfWidth = width * TrendMultiplier;
		if (halfWidth <= 0)
			return;

		var close = candle.ClosePrice;
		var osc = (close - middle) / halfWidth * 100m;

		var highestValue = _oscHighest.Process(new DecimalIndicatorValue(_oscHighest, osc, candle.OpenTime) { IsFinal = true });
		var lowestValue = _oscLowest.Process(new DecimalIndicatorValue(_oscLowest, osc, candle.OpenTime) { IsFinal = true });

		if (!_oscHighest.IsFormed || !_oscLowest.IsFormed)
			return;

		var upperValue = _upperRange.Process(new DecimalIndicatorValue(_upperRange, highestValue.ToDecimal(), candle.OpenTime) { IsFinal = true });
		var lowerValue = _lowerRange.Process(new DecimalIndicatorValue(_lowerRange, lowestValue.ToDecimal(), candle.OpenTime) { IsFinal = true });

		if (!_upperRange.IsFormed || !_lowerRange.IsFormed)
			return;

		var upper = upperValue.ToDecimal();
		var lower = lowerValue.ToDecimal();

		var prevOsc = _prevOsc;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevOsc = osc;
		_prevUpper = upper;
		_prevLower = lower;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_stopPrice is decimal stop)
		{
			if (Position > 0 && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = null;
				return;
			}

			if (Position < 0 && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				return;
			}
		}

		if (prevOsc is not decimal po || prevUpper is not decimal pu || prevLower is not decimal pl)
			return;

		if (po <= pu && osc > upper && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = UseInitialStop ? close - atr * ReverseMultiplier : null;
		}
		else if (po >= pl && osc < lower && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = UseInitialStop ? close + atr * ReverseMultiplier : null;
		}
	}
}
