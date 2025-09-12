using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average Rainbow (Stormer) strategy.
/// Implements a set of twelve moving averages forming a rainbow.
/// Enters long or short based on trend confirmation and price touching the averages.
/// Stop loss is set to the previously touched average with profit target as a multiplier of the risk.
/// </summary>
public class MovingAverageRainbowStormerStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _maLengthFirst;
	private readonly StrategyParam<int> _maLengthSecond;
	private readonly StrategyParam<int> _maLengthThird;
	private readonly StrategyParam<int> _maLengthFourth;
	private readonly StrategyParam<int> _maLengthFifth;
	private readonly StrategyParam<int> _maLengthSixth;
	private readonly StrategyParam<int> _maLengthSeventh;
	private readonly StrategyParam<int> _maLengthEighth;
	private readonly StrategyParam<int> _maLengthNinth;
	private readonly StrategyParam<int> _maLengthTenth;
	private readonly StrategyParam<int> _maLengthEleventh;
	private readonly StrategyParam<int> _maLengthTwelfth;
	private readonly StrategyParam<decimal> _targetFactor;
	private readonly StrategyParam<bool> _verifyTurnoverTrend;
	private readonly StrategyParam<bool> _verifyTurnoverSignal;
	private readonly StrategyParam<bool> _verifyTurnoverSignalPriceExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;
	private decimal? _prevTouchPriceUptrend;
	private decimal? _prevTouchPriceDowntrend;
	private decimal _entryPrice;
	private bool _positionIsLong;
	private decimal _stopLoss;
	private decimal _target;
	private decimal _longPositionHighestHigh;
	private decimal _shortPositionLowestLow;
	private bool _isTurnoverTrendLongTrigger;
	private bool _isTurnoverTrendShortTrigger;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Length of MA #1.
	/// </summary>
	public int MaLengthFirst
	{
		get => _maLengthFirst.Value;
		set => _maLengthFirst.Value = value;
	}

	/// <summary>
	/// Length of MA #2.
	/// </summary>
	public int MaLengthSecond
	{
		get => _maLengthSecond.Value;
		set => _maLengthSecond.Value = value;
	}

	/// <summary>
	/// Length of MA #3.
	/// </summary>
	public int MaLengthThird
	{
		get => _maLengthThird.Value;
		set => _maLengthThird.Value = value;
	}

	/// <summary>
	/// Length of MA #4.
	/// </summary>
	public int MaLengthFourth
	{
		get => _maLengthFourth.Value;
		set => _maLengthFourth.Value = value;
	}

	/// <summary>
	/// Length of MA #5.
	/// </summary>
	public int MaLengthFifth
	{
		get => _maLengthFifth.Value;
		set => _maLengthFifth.Value = value;
	}

	/// <summary>
	/// Length of MA #6.
	/// </summary>
	public int MaLengthSixth
	{
		get => _maLengthSixth.Value;
		set => _maLengthSixth.Value = value;
	}

	/// <summary>
	/// Length of MA #7.
	/// </summary>
	public int MaLengthSeventh
	{
		get => _maLengthSeventh.Value;
		set => _maLengthSeventh.Value = value;
	}

	/// <summary>
	/// Length of MA #8.
	/// </summary>
	public int MaLengthEighth
	{
		get => _maLengthEighth.Value;
		set => _maLengthEighth.Value = value;
	}

	/// <summary>
	/// Length of MA #9.
	/// </summary>
	public int MaLengthNinth
	{
		get => _maLengthNinth.Value;
		set => _maLengthNinth.Value = value;
	}

	/// <summary>
	/// Length of MA #10.
	/// </summary>
	public int MaLengthTenth
	{
		get => _maLengthTenth.Value;
		set => _maLengthTenth.Value = value;
	}

	/// <summary>
	/// Length of MA #11.
	/// </summary>
	public int MaLengthEleventh
	{
		get => _maLengthEleventh.Value;
		set => _maLengthEleventh.Value = value;
	}

	/// <summary>
	/// Length of MA #12.
	/// </summary>
	public int MaLengthTwelfth
	{
		get => _maLengthTwelfth.Value;
		set => _maLengthTwelfth.Value = value;
	}

	/// <summary>
	/// Take profit factor relative to risk.
	/// </summary>
	public decimal TargetFactor
	{
		get => _targetFactor.Value;
		set => _targetFactor.Value = value;
	}

	/// <summary>
	/// Enable turnover trend exit.
	/// </summary>
	public bool VerifyTurnoverTrend
	{
		get => _verifyTurnoverTrend.Value;
		set => _verifyTurnoverTrend.Value = value;
	}

	/// <summary>
	/// Enable turnover signal reversal.
	/// </summary>
	public bool VerifyTurnoverSignal
	{
		get => _verifyTurnoverSignal.Value;
		set => _verifyTurnoverSignal.Value = value;
	}

	/// <summary>
	/// Require profitable price for turnover signal.
	/// </summary>
	public bool VerifyTurnoverSignalPriceExit
	{
		get => _verifyTurnoverSignalPriceExit.Value;
		set => _verifyTurnoverSignalPriceExit.Value = value;
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
	/// Initialize strategy.
	/// </summary>
	public MovingAverageRainbowStormerStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Exponential)
		.SetDisplay("MA Type", string.Empty, "Moving Averages");

		_maLengthFirst = Param(nameof(MaLengthFirst), 3)
		.SetGreaterThanZero()
		.SetDisplay("MA #1", string.Empty, "Moving Averages");
		_maLengthSecond = Param(nameof(MaLengthSecond), 5)
		.SetGreaterThanZero()
		.SetDisplay("MA #2", string.Empty, "Moving Averages");
		_maLengthThird = Param(nameof(MaLengthThird), 8)
		.SetGreaterThanZero()
		.SetDisplay("MA #3", string.Empty, "Moving Averages");
		_maLengthFourth = Param(nameof(MaLengthFourth), 13)
		.SetGreaterThanZero()
		.SetDisplay("MA #4", string.Empty, "Moving Averages");
		_maLengthFifth = Param(nameof(MaLengthFifth), 20)
		.SetGreaterThanZero()
		.SetDisplay("MA #5", string.Empty, "Moving Averages");
		_maLengthSixth = Param(nameof(MaLengthSixth), 25)
		.SetGreaterThanZero()
		.SetDisplay("MA #6", string.Empty, "Moving Averages");
		_maLengthSeventh = Param(nameof(MaLengthSeventh), 30)
		.SetGreaterThanZero()
		.SetDisplay("MA #7", string.Empty, "Moving Averages");
		_maLengthEighth = Param(nameof(MaLengthEighth), 35)
		.SetGreaterThanZero()
		.SetDisplay("MA #8", string.Empty, "Moving Averages");
		_maLengthNinth = Param(nameof(MaLengthNinth), 40)
		.SetGreaterThanZero()
		.SetDisplay("MA #9", string.Empty, "Moving Averages");
		_maLengthTenth = Param(nameof(MaLengthTenth), 45)
		.SetGreaterThanZero()
		.SetDisplay("MA #10", string.Empty, "Moving Averages");
		_maLengthEleventh = Param(nameof(MaLengthEleventh), 50)
		.SetGreaterThanZero()
		.SetDisplay("MA #11", string.Empty, "Moving Averages");
		_maLengthTwelfth = Param(nameof(MaLengthTwelfth), 55)
		.SetGreaterThanZero()
		.SetDisplay("MA #12", string.Empty, "Moving Averages");

		_targetFactor = Param(nameof(TargetFactor), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("Target Factor", "Take profit multiplier", "Risk Management");

		_verifyTurnoverTrend = Param(nameof(VerifyTurnoverTrend), true)
		.SetDisplay("Verify Turnover Trend", string.Empty, "Turnover");
		_verifyTurnoverSignal = Param(nameof(VerifyTurnoverSignal), false)
		.SetDisplay("Verify Turnover Signal", string.Empty, "Turnover");
		_verifyTurnoverSignalPriceExit = Param(nameof(VerifyTurnoverSignalPriceExit), false)
		.SetDisplay("Verify Turnover Signal Price Exit", string.Empty, "Turnover");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", string.Empty, "General");
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

		_prevHigh = 0m;
		_prevLow = 0m;
		_hasPrev = false;
		_prevTouchPriceUptrend = null;
		_prevTouchPriceDowntrend = null;
		_entryPrice = 0m;
		_positionIsLong = false;
		_stopLoss = 0m;
		_target = 0m;
		_longPositionHighestHigh = 0m;
		_shortPositionLowestLow = 0m;
		_isTurnoverTrendLongTrigger = false;
		_isTurnoverTrendShortTrigger = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma1 = CreateMa(MaType, MaLengthFirst);
		var ma2 = CreateMa(MaType, MaLengthSecond);
		var ma3 = CreateMa(MaType, MaLengthThird);
		var ma4 = CreateMa(MaType, MaLengthFourth);
		var ma5 = CreateMa(MaType, MaLengthFifth);
		var ma6 = CreateMa(MaType, MaLengthSixth);
		var ma7 = CreateMa(MaType, MaLengthSeventh);
		var ma8 = CreateMa(MaType, MaLengthEighth);
		var ma9 = CreateMa(MaType, MaLengthNinth);
		var ma10 = CreateMa(MaType, MaLengthTenth);
		var ma11 = CreateMa(MaType, MaLengthEleventh);
		var ma12 = CreateMa(MaType, MaLengthTwelfth);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ma1, ma2, ma3, ma4, ma5, ma6, ma7, ma8, ma9, ma10, ma11, ma12, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle,
	decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5, decimal ma6,
	decimal ma7, decimal ma8, decimal ma9, decimal ma10, decimal ma11, decimal ma12)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var maMean = (ma1 + ma2 + ma3 + ma4 + ma5 + ma6 + ma7 + ma8 + ma9 + ma10 + ma11 + ma12) / 12m;

		var isMa1To4Above = ma1 > ma2 && ma2 > ma3 && ma3 > ma4;
		var isMa1To4Below = ma1 < ma2 && ma2 < ma3 && ma3 < ma4;
		var isMa5To8Above = ma5 > ma6 && ma6 > ma7 && ma7 > ma8;
		var isMa5To8Below = ma5 < ma6 && ma6 < ma7 && ma7 < ma8;
		var isCloseGreaterMaMean = candle.ClosePrice > maMean;
		var isCloseLesserMaMean = candle.ClosePrice < maMean;
		var isCurHighGreaterPrevHigh = _hasPrev && candle.HighPrice > _prevHigh;
		var isCurLowLesserPrevLow = _hasPrev && candle.LowPrice < _prevLow;
		var isMaUptrend = isCloseGreaterMaMean && isMa5To8Above;
		var isMaDowntrend = isCloseLesserMaMean && isMa5To8Below;

		var curTouchPriceUptrend = isMaUptrend ? GetTouchPriceUptrend(candle.LowPrice, ma1, ma2, ma3, ma4, ma5, ma6, ma7, ma8, ma9, ma10, ma11, ma12) : null;
		var curTouchPriceDowntrend = isMaDowntrend ? GetTouchPriceDowntrend(candle.HighPrice, ma1, ma2, ma3, ma4, ma5, ma6, ma7, ma8, ma9, ma10, ma11, ma12) : null;

		var prevTouchPriceUptrend = _prevTouchPriceUptrend;
		var prevTouchPriceDowntrend = _prevTouchPriceDowntrend;
		_prevTouchPriceUptrend = curTouchPriceUptrend;
		_prevTouchPriceDowntrend = curTouchPriceDowntrend;

		var isPrevTouchedPriceUptrend = prevTouchPriceUptrend.HasValue && isMaUptrend;
		var isPrevTouchedPriceDowntrend = prevTouchPriceDowntrend.HasValue && isMaDowntrend;

		var isLongCondition = isMaUptrend && isCurHighGreaterPrevHigh && isPrevTouchedPriceUptrend;
		var isShortCondition = isMaDowntrend && isCurLowLesserPrevLow && isPrevTouchedPriceDowntrend;

		if (Position == 0)
		{
			_entryPrice = 0m;
			_positionIsLong = false;
			_stopLoss = 0m;
			_target = 0m;
			_longPositionHighestHigh = 0m;
			_shortPositionLowestLow = 0m;
			_isTurnoverTrendLongTrigger = false;
			_isTurnoverTrendShortTrigger = false;
		}

		if (VerifyTurnoverSignal && isLongCondition && Position < 0)
		{
			if (!VerifyTurnoverSignalPriceExit || candle.ClosePrice < _entryPrice)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_positionIsLong = true;
				_stopLoss = prevTouchPriceUptrend ?? candle.ClosePrice;
				if (isCurLowLesserPrevLow)
				{
					var curLowTouchedPrice = curTouchPriceUptrend ?? candle.LowPrice;
					_stopLoss = curTouchPriceUptrend.HasValue ? curLowTouchedPrice : (_stopLoss + curLowTouchedPrice) / 2m;
				}
				_target = _entryPrice + Math.Abs(_entryPrice - _stopLoss) * TargetFactor;
			}
		}
		else if (VerifyTurnoverSignal && isShortCondition && Position > 0)
		{
			if (!VerifyTurnoverSignalPriceExit || candle.ClosePrice > _entryPrice)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_positionIsLong = false;
				_stopLoss = prevTouchPriceDowntrend ?? candle.ClosePrice;
				if (isCurHighGreaterPrevHigh)
				{
					var curHighTouchedPrice = curTouchPriceDowntrend ?? candle.HighPrice;
					_stopLoss = curTouchPriceDowntrend.HasValue ? curHighTouchedPrice : (_stopLoss + curHighTouchedPrice) / 2m;
				}
				_target = _entryPrice - Math.Abs(_entryPrice - _stopLoss) * TargetFactor;
			}
		}
		else if (isLongCondition && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_positionIsLong = true;
			_stopLoss = prevTouchPriceUptrend ?? candle.ClosePrice;
			if (isCurLowLesserPrevLow)
			{
				var curLowTouchedPrice = curTouchPriceUptrend ?? candle.LowPrice;
				_stopLoss = curTouchPriceUptrend.HasValue ? curLowTouchedPrice : (_stopLoss + curLowTouchedPrice) / 2m;
			}
			_target = _entryPrice + Math.Abs(_entryPrice - _stopLoss) * TargetFactor;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isShortCondition && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_positionIsLong = false;
			_stopLoss = prevTouchPriceDowntrend ?? candle.ClosePrice;
			if (isCurHighGreaterPrevHigh)
			{
				var curHighTouchedPrice = curTouchPriceDowntrend ?? candle.HighPrice;
				_stopLoss = curTouchPriceDowntrend.HasValue ? curHighTouchedPrice : (_stopLoss + curHighTouchedPrice) / 2m;
			}
			_target = _entryPrice - Math.Abs(_entryPrice - _stopLoss) * TargetFactor;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			_longPositionHighestHigh = Math.Max(_longPositionHighestHigh, candle.HighPrice);
			if (VerifyTurnoverTrend && isMa1To4Below && isCloseLesserMaMean && _longPositionHighestHigh > _entryPrice)
			{
				_isTurnoverTrendLongTrigger = true;
				SellMarket(Math.Abs(Position));
			}
			else if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _target)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			_shortPositionLowestLow = _shortPositionLowestLow == 0m ? candle.LowPrice : Math.Min(_shortPositionLowestLow, candle.LowPrice);
			if (VerifyTurnoverTrend && isMa1To4Above && isCloseGreaterMaMean && _shortPositionLowestLow < _entryPrice)
			{
				_isTurnoverTrendShortTrigger = true;
				BuyMarket(Math.Abs(Position));
			}
			else if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _target)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPrev = true;
	}

	private static decimal? GetTouchPriceUptrend(decimal low, decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5, decimal ma6, decimal ma7, decimal ma8, decimal ma9, decimal ma10, decimal ma11, decimal ma12)
	{
		if (low <= ma1 && low >= ma2) return ma2;
		else if (low <= ma2 && low >= ma3) return ma3;
		else if (low <= ma3 && low >= ma4) return ma4;
		else if (low <= ma4 && low >= ma5) return ma5;
		else if (low <= ma5 && low >= ma6) return ma6;
		else if (low <= ma6 && low >= ma7) return ma7;
		else if (low <= ma7 && low >= ma8) return ma8;
		else if (low <= ma8 && low >= ma9) return ma9;
		else if (low <= ma9 && low >= ma10) return ma10;
		else if (low <= ma10 && low >= ma11) return ma11;
		else if (low <= ma11 && low >= ma12) return ma12;
		else return null;
	}

	private static decimal? GetTouchPriceDowntrend(decimal high, decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5, decimal ma6, decimal ma7, decimal ma8, decimal ma9, decimal ma10, decimal ma11, decimal ma12)
	{
		if (high >= ma1 && high <= ma2) return ma2;
		else if (high >= ma2 && high <= ma3) return ma3;
		else if (high >= ma3 && high <= ma4) return ma4;
		else if (high >= ma4 && high <= ma5) return ma5;
		else if (high >= ma5 && high <= ma6) return ma6;
		else if (high >= ma6 && high <= ma7) return ma7;
		else if (high >= ma7 && high <= ma8) return ma8;
		else if (high >= ma8 && high <= ma9) return ma9;
		else if (high >= ma9 && high <= ma10) return ma10;
		else if (high >= ma10 && high <= ma11) return ma11;
		else if (high >= ma11 && high <= ma12) return ma12;
		else return null;
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.Hull => new HullMovingAverage { Length = length },
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Available moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Weighted moving average.</summary>
		Weighted,
		/// <summary>Hull moving average.</summary>
		Hull,
		/// <summary>Volume weighted moving average.</summary>
		VolumeWeighted
	}
}
