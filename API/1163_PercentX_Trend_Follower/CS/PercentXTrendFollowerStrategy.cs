using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PercentX Trend Follower strategy based on oscillator ranges.
/// </summary>
public class PercentXTrendFollowerStrategy : Strategy
{
	public enum BandMode
	{
		Keltner,
		Bollinger,
	}

	private readonly StrategyParam<BandMode> _bandType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _loopbackPeriod;
	private readonly StrategyParam<int> _outerLoopback;
	private readonly StrategyParam<bool> _useInitialStop;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trendMultiplier;
	private readonly StrategyParam<decimal> _reverseMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private BollingerBands _bollinger = null!;
	private KeltnerChannels _keltner = null!;
	private Highest _oscHighest = null!;
	private Lowest _oscLowest = null!;
	private Highest _overboughtHighest = null!;
	private Lowest _oversoldLowest = null!;

	private decimal? _prevOscillator;
	private decimal? _prevUpperRange;
	private decimal? _prevLowerRange;
	private decimal _stopPrice;

	/// <summary>
	/// Band type.
	/// </summary>
	public BandMode BandType { get => _bandType.Value; set => _bandType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Loopback period for oscillator range.
	/// </summary>
	public int LoopbackPeriod { get => _loopbackPeriod.Value; set => _loopbackPeriod.Value = value; }

	/// <summary>
	/// Loopback period for outer range.
	/// </summary>
	public int OuterLoopback { get => _outerLoopback.Value; set => _outerLoopback.Value = value; }

	/// <summary>
	/// Use ATR based initial stop.
	/// </summary>
	public bool UseInitialStop { get => _useInitialStop.Value; set => _useInitialStop.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Multiplier for trend direction.
	/// </summary>
	public decimal TrendMultiplier { get => _trendMultiplier.Value; set => _trendMultiplier.Value = value; }

	/// <summary>
	/// Multiplier for reverse stop.
	/// </summary>
	public decimal ReverseMultiplier { get => _reverseMultiplier.Value; set => _reverseMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="PercentXTrendFollowerStrategy"/>.
	/// </summary>
	public PercentXTrendFollowerStrategy()
	{
		_bandType = Param(nameof(BandType), BandMode.Keltner)
			.SetDisplay("Band Type", "Indicator for band calculation", "Parameters");

		_maLength = Param(nameof(MaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Parameters");

		_loopbackPeriod = Param(nameof(LoopbackPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Range Lookback", "Lookback for oscillator range", "Parameters");

		_outerLoopback = Param(nameof(OuterLoopback), 80)
			.SetGreaterThanZero()
			.SetDisplay("Outer Range Lookback", "Lookback for outer range", "Parameters");

		_useInitialStop = Param(nameof(UseInitialStop), true)
			.SetDisplay("Use Initial Stop", "Enable ATR based stop", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation length", "Risk");

		_trendMultiplier = Param(nameof(TrendMultiplier), 1m)
			.SetDisplay("Trend Mult", "Multiplier for entry distance", "Risk");

		_reverseMultiplier = Param(nameof(ReverseMultiplier), 3m)
			.SetDisplay("Reverse Mult", "Multiplier for stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		Volume = 1;
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
		_prevOscillator = null;
		_prevUpperRange = null;
		_prevLowerRange = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrLength };
		_bollinger = new BollingerBands { Length = MaLength, Width = 2m };
		_keltner = new KeltnerChannels { Length = MaLength, Multiplier = 2m };
		_oscHighest = new Highest { Length = LoopbackPeriod };
		_oscLowest = new Lowest { Length = LoopbackPeriod };
		_overboughtHighest = new Highest { Length = OuterLoopback };
		_oversoldLowest = new Lowest { Length = OuterLoopback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, _keltner, _atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue keltnerValue, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		decimal middle;
		decimal upper;
		decimal lower;

		if (BandType == BandMode.Bollinger)
		{
			var bb = (BollingerBandsValue)bollingerValue;
			if (bb.UpBand is not decimal up || bb.LowBand is not decimal low || bb.MovingAverage is not decimal mid)
				return;

			middle = mid;
			upper = up;
			lower = low;
		}
		else
		{
			var kc = (KeltnerChannelsValue)keltnerValue;
			if (kc.UpBand is not decimal up || kc.LowBand is not decimal low || kc.MovingAverage is not decimal mid)
				return;

			middle = mid;
			upper = up;
			lower = low;
		}

		var denom = middle - lower;
		if (denom == 0)
			return;

		var oscillator = (candle.ClosePrice - middle) / denom;

		var overbought = _oscHighest.Process(oscillator).ToDecimal();
		var oversold = _oscLowest.Process(oscillator).ToDecimal();
		var upperRange = _overboughtHighest.Process(overbought).ToDecimal();
		var lowerRange = _oversoldLowest.Process(oversold).ToDecimal();

		var longSignal = _prevOscillator is decimal po && _prevUpperRange is decimal pur && po <= pur && oscillator > upperRange;
		var shortSignal = _prevOscillator is decimal po2 && _prevLowerRange is decimal plr && po2 >= plr && oscillator < lowerRange;

		_prevOscillator = oscillator;
		_prevUpperRange = upperRange;
		_prevLowerRange = lowerRange;

		if (longSignal && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			if (UseInitialStop)
				_stopPrice = candle.LowPrice - atr * ReverseMultiplier;
		}
		else if (shortSignal && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
			if (UseInitialStop)
				_stopPrice = candle.HighPrice + atr * ReverseMultiplier;
		}

		if (UseInitialStop)
		{
			if (Position > 0 && candle.LowPrice <= _stopPrice)
				SellMarket(Position);
			else if (Position < 0 && candle.HighPrice >= _stopPrice)
				BuyMarket(-Position);
		}
	}
}
