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
/// Candlestick confirmation strategy combined with a stochastic oscillator filter.
/// The system looks for bullish or bearish reversal patterns and confirms them with the %D line of a stochastic indicator.
/// </summary>
public class CandlestickStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowingPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _closeLowerLevel;
	private readonly StrategyParam<decimal> _closeUpperLevel;

	private StochasticOscillator _stochastic = null!;
	private List<decimal> _bodyValues = new();
	private List<decimal> _closeValues = new();

	private CandleInfo? _prev1;
	private CandleInfo? _prev2;
	private CandleInfo? _prev3;

	private decimal? _stochPrev1;
	private decimal? _stochPrev2;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlestickStochasticStrategy"/> class.
	/// </summary>
	public CandlestickStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for candlestick analysis", "General");

		_maPeriod = Param(nameof(MaPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Body Average Period", "Number of candles used for average body calculations", "Pattern Detection")
		.SetCanOptimize(true)
		.SetOptimize(6, 24, 2);

		_kPeriod = Param(nameof(KPeriod), 33)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "%K lookback period", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_dPeriod = Param(nameof(DPeriod), 37)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Smoothing period for %D", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_slowingPeriod = Param(nameof(SlowingPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Smoothing", "Additional smoothing period applied to %K", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
		.SetDisplay("Oversold Threshold", "Value of %D considered oversold", "Trading Rules")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
		.SetDisplay("Overbought Threshold", "Value of %D considered overbought", "Trading Rules")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_closeLowerLevel = Param(nameof(CloseLowerLevel), 20m)
		.SetDisplay("Lower Exit Level", "Lower crossover level used to exit short positions", "Trading Rules")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_closeUpperLevel = Param(nameof(CloseUpperLevel), 80m)
		.SetDisplay("Upper Exit Level", "Upper crossover level used to exit long positions", "Trading Rules")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);
	}

	/// <summary>
	/// Target candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used for average body size.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// %K lookback period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to %K.
	/// </summary>
	public int SlowingPeriod
	{
		get => _slowingPeriod.Value;
		set => _slowingPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level for entries.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level for entries.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Lower crossover level used to close shorts.
	/// </summary>
	public decimal CloseLowerLevel
	{
		get => _closeLowerLevel.Value;
		set => _closeLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper crossover level used to close longs.
	/// </summary>
	public decimal CloseUpperLevel
	{
		get => _closeUpperLevel.Value;
		set => _closeUpperLevel.Value = value;
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

		_stochastic = null!;
		_bodyValues = new();
		_closeValues = new();
		_prev1 = null;
		_prev2 = null;
		_prev3 = null;
		_stochPrev1 = null;
		_stochPrev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Smooth = SlowingPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.D is not decimal currentSignal)
		return;

		var previousSignal = _stochPrev1;
		var previousSignal2 = _stochPrev2;

		var bullishPattern = CheckBullishPattern();
		var bearishPattern = CheckBearishPattern();

		if (Position < 0m)
		{
			var exitShort = bearishPattern;

			if (!exitShort && previousSignal is decimal s1 && previousSignal2 is decimal s2)
			{
				if ((s1 > CloseLowerLevel && s2 < CloseLowerLevel) || (s1 > CloseUpperLevel && s2 < CloseUpperLevel))
				{
					exitShort = true;
				}
			}

			if (exitShort)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		if (Position > 0m)
		{
			var exitLong = bullishPattern;

			if (!exitLong && previousSignal is decimal s1 && previousSignal2 is decimal s2)
			{
				if ((s1 < CloseUpperLevel && s2 > CloseUpperLevel) || (s1 < CloseLowerLevel && s2 > CloseLowerLevel))
				{
					exitLong = true;
				}
			}

			if (exitLong)
			{
				SellMarket(Math.Abs(Position));
			}
		}

		if (IsFormedAndOnlineAndAllowTrading() && previousSignal is decimal signal1)
		{
			if (bullishPattern && signal1 < OversoldLevel && Position <= 0m)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (bearishPattern && signal1 > OverboughtLevel && Position >= 0m)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_stochPrev2 = _stochPrev1;
		_stochPrev1 = currentSignal;

		ShiftCandles(candle);
	}

	private void ShiftCandles(ICandleMessage candle)
	{
		if (_prev1 is CandleInfo latest)
		{
			_bodyValues.Insert(0, latest.Body);
			_closeValues.Insert(0, latest.Close);
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = new CandleInfo(candle);

		TrimHistory(_bodyValues);
		TrimHistory(_closeValues);
	}

	private void TrimHistory(List<decimal> values)
	{
		var maxLength = MaPeriod + 3;
		if (values.Count > maxLength)
		{
			values.RemoveRange(maxLength, values.Count - maxLength);
		}
	}

	private bool CheckBullishPattern()
	{
		return CheckThreeWhiteSoldiers()
		|| CheckPiercingLine()
		|| CheckMorningDoji()
		|| CheckBullishEngulfing()
		|| CheckBullishHarami()
		|| CheckMorningStar()
		|| CheckBullishMeetingLines();
	}

	private bool CheckBearishPattern()
	{
		return CheckThreeBlackCrows()
		|| CheckDarkCloudCover()
		|| CheckEveningDoji()
		|| CheckBearishEngulfing()
		|| CheckBearishHarami()
		|| CheckEveningStar()
		|| CheckBearishMeetingLines();
	}

	private decimal? AvgBody(int shift)
	{
		var index = shift - 1;
		var required = index + MaPeriod;
		if (index < 0 || required > _bodyValues.Count)
		return null;

		decimal sum = 0m;
		for (var i = index; i < required; i++)
		sum += _bodyValues[i];

		return sum / MaPeriod;
	}

	private decimal? CloseAvg(int shift)
	{
		var index = shift - 1;
		var required = index + MaPeriod;
		if (index < 0 || required > _closeValues.Count)
		return null;

		decimal sum = 0m;
		for (var i = index; i < required; i++)
		sum += _closeValues[i];

		return sum / MaPeriod;
	}

	private static decimal MidPoint(CandleInfo candle)
	{
		return (candle.High + candle.Low) / 2m;
	}

	private static decimal MidOpenClose(CandleInfo candle)
	{
		return (candle.Open + candle.Close) / 2m;
	}

	private bool CheckThreeBlackCrows()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2 || _prev3 is not CandleInfo c3)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c3.Open - c3.Close > avgBody)
		&& (c2.Open - c2.Close > avgBody)
		&& (c1.Open - c1.Close > avgBody)
		&& MidPoint(c2) < MidPoint(c3)
		&& MidPoint(c1) < MidPoint(c2);
	}

	private bool CheckThreeWhiteSoldiers()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2 || _prev3 is not CandleInfo c3)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c3.Close - c3.Open > avgBody)
		&& (c2.Close - c2.Open > avgBody)
		&& (c1.Close - c1.Open > avgBody)
		&& MidPoint(c2) > MidPoint(c3)
		&& MidPoint(c1) > MidPoint(c2);
	}

	private bool CheckDarkCloudCover()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c2.Close - c2.Open > avgBody)
		&& (c1.Close < c2.Close)
		&& (c1.Close > c2.Open)
		&& CloseAvg(2) is decimal trend && MidOpenClose(c2) > trend
		&& c1.Open > c2.High;
	}

	private bool CheckPiercingLine()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c1.Close - c1.Open > avgBody)
		&& (c2.Open - c2.Close > avgBody)
		&& (c1.Close > c2.Close)
		&& (c1.Close < c2.Open)
		&& CloseAvg(2) is decimal trend && MidOpenClose(c2) < trend
		&& c1.Open < c2.Low;
	}

	private bool CheckMorningDoji()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2 || _prev3 is not CandleInfo c3)
		return false;

		if (AvgBody(1) is not decimal avgBody || AvgBody(2) is not decimal avgBody2)
		return false;

		return (c3.Open - c3.Close > avgBody)
		&& (avgBody2 < avgBody * 0.1m)
		&& (c2.Close < c3.Close)
		&& (c2.Open < c3.Open)
		&& (c1.Open > c2.Close)
		&& (c1.Close > c2.Close);
	}

	private bool CheckEveningDoji()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2 || _prev3 is not CandleInfo c3)
		return false;

		if (AvgBody(1) is not decimal avgBody || AvgBody(2) is not decimal avgBody2)
		return false;

		return (c3.Close - c3.Open > avgBody)
		&& (avgBody2 < avgBody * 0.1m)
		&& (c2.Close > c3.Close)
		&& (c2.Open > c3.Open)
		&& (c1.Open < c2.Close)
		&& (c1.Close < c2.Close);
	}

	private bool CheckBearishEngulfing()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c2.Open < c2.Close)
		&& (c1.Open - c1.Close > avgBody)
		&& (c1.Close < c2.Open)
		&& CloseAvg(2) is decimal trend && MidOpenClose(c2) > trend
		&& (c1.Open > c2.Close);
	}

	private bool CheckBullishEngulfing()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c2.Open > c2.Close)
		&& (c1.Close - c1.Open > avgBody)
		&& (c1.Close > c2.Open)
		&& CloseAvg(2) is decimal trend && MidOpenClose(c2) < trend
		&& (c1.Open < c2.Close);
	}

	private bool CheckEveningStar()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2 || _prev3 is not CandleInfo c3)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c3.Close - c3.Open > avgBody)
		&& (Math.Abs(c2.Close - c2.Open) < avgBody * 0.5m)
		&& (c2.Close > c3.Close)
		&& (c2.Open > c3.Open)
		&& (c1.Close < MidOpenClose(c3));
	}

	private bool CheckMorningStar()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2 || _prev3 is not CandleInfo c3)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c3.Open - c3.Close > avgBody)
		&& (Math.Abs(c2.Close - c2.Open) < avgBody * 0.5m)
		&& (c2.Close < c3.Close)
		&& (c2.Open < c3.Open)
		&& (c1.Close > MidOpenClose(c3));
	}

	private bool CheckBearishHarami()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c1.Close < c1.Open)
		&& (c2.Close - c2.Open > avgBody)
		&& (c1.Close > c2.Open)
		&& (c1.Open < c2.Close)
		&& CloseAvg(2) is decimal trend && MidPoint(c2) > trend;
	}

	private bool CheckBullishHarami()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c1.Close > c1.Open)
		&& (c2.Open - c2.Close > avgBody)
		&& (c1.Close < c2.Open)
		&& (c1.Open > c2.Close)
		&& CloseAvg(2) is decimal trend && MidPoint(c2) < trend;
	}

	private bool CheckBearishMeetingLines()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c2.Close - c2.Open > avgBody)
		&& (c1.Open - c1.Close > avgBody)
		&& Math.Abs(c1.Close - c2.Close) < 0.1m * avgBody;
	}

	private bool CheckBullishMeetingLines()
	{
		if (_prev1 is not CandleInfo c1 || _prev2 is not CandleInfo c2)
		return false;

		if (AvgBody(1) is not decimal avgBody)
		return false;

		return (c2.Open - c2.Close > avgBody)
		&& (c1.Close - c1.Open > avgBody)
		&& Math.Abs(c1.Close - c2.Close) < 0.1m * avgBody;
	}

	private readonly struct CandleInfo
	{
		public CandleInfo(ICandleMessage candle)
		{
			Open = candle.OpenPrice;
			High = candle.HighPrice;
			Low = candle.LowPrice;
			Close = candle.ClosePrice;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
		public decimal Body => Math.Abs(Close - Open);
	}
}

