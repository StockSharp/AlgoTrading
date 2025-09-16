using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD, EMA crossover, Bollinger Bands, Parabolic SAR and Bulls/Bears Power.
/// Trades during a predefined session and allows only one position at a time.
/// </summary>
public class MacdEmaSarBollingerBullBearStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _powerPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBearsPower;
	private decimal _prevBullsPower;
	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevUpper1;
	private decimal _prevUpper2;
	private int _candleCount;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Period for the fast EMA crossover.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow EMA crossover.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for Bulls and Bears Power indicators.
	/// </summary>
	public int PowerPeriod
	{
		get => _powerPeriod.Value;
		set => _powerPeriod.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum step.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MacdEmaSarBollingerBullBearStrategy"/>.
	/// </summary>
	public MacdEmaSarBollingerBullBearStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12).SetDisplay("MACD Fast", "MACD fast EMA period", "MACD");
		_macdSlow = Param(nameof(MacdSlow), 26).SetDisplay("MACD Slow", "MACD slow EMA period", "MACD");
		_macdSignal = Param(nameof(MacdSignal), 9).SetDisplay("MACD Signal", "MACD signal line period", "MACD");
		_fastMaPeriod = Param(nameof(FastMaPeriod), 3).SetDisplay("Fast EMA", "Fast EMA period", "MA");
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 34).SetDisplay("Slow EMA", "Slow EMA period", "MA");
		_powerPeriod = Param(nameof(PowerPeriod), 13).SetDisplay("Power Period", "Bulls/Bears Power period", "Power");
		_sarStep = Param(nameof(SarStep), 0.02m).SetDisplay("SAR Step", "Parabolic SAR step", "SAR");
		_sarMax = Param(nameof(SarMax), 0.2m).SetDisplay("SAR Max", "Parabolic SAR max", "SAR");
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetDisplay("BB Period", "Bollinger Bands period", "Bollinger");
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetDisplay("BB Deviation", "Bollinger Bands deviation", "Bollinger");
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(9)).SetDisplay("Session Start", "Trading session start", "Session");
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(17)).SetDisplay("Session End", "Trading session end", "Session");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var sar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = SarMax };
		var bears = new BearsPower { Length = PowerPeriod };
		var bulls = new BullsPower { Length = PowerPeriod };
		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, fastMa, slowMa, sar, bears, bulls, bollinger, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue macdValue,
	IIndicatorValue fastMaValue,
	IIndicatorValue slowMaValue,
	IIndicatorValue sarValue,
	IIndicatorValue bearsValue,
	IIndicatorValue bullsValue,
	IIndicatorValue bollingerValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var time = candle.OpenTime.LocalDateTime.TimeOfDay;
	var boll = (BollingerBandsValue)bollingerValue;
	var upperBand = boll.UpBand;
	var bears = bearsValue.GetValue<decimal>();
	var bulls = bullsValue.GetValue<decimal>();

	if (time < SessionStart || time >= SessionEnd || !IsFormedAndOnlineAndAllowTrading())
	{
	UpdateState(candle, upperBand, bears, bulls);
	return;
	}

	var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	var macdMain = macd.Macd;
	var macdSignal = macd.Signal;
	var emaFast = fastMaValue.GetValue<decimal>();
	var emaSlow = slowMaValue.GetValue<decimal>();
	var sar = sarValue.GetValue<decimal>();

	var sellSignal = Position == 0 &&
	macdMain > macdSignal &&
	emaFast < emaSlow &&
	sar > candle.HighPrice &&
	bears < 0m &&
	bears > _prevBearsPower;

	var buySignal = Position == 0 &&
	_candleCount >= 2 &&
	macdMain < macdSignal &&
	_prevHigh1 < _prevUpper1 &&
	_prevHigh2 < _prevUpper2 &&
	emaFast > emaSlow &&
	sar < candle.LowPrice &&
	bulls > 0m &&
	bulls < _prevBullsPower;

	var volume = Volume + Math.Abs(Position);

	if (sellSignal)
	SellMarket(volume);
	else if (buySignal)
	BuyMarket(volume);

	UpdateState(candle, upperBand, bears, bulls);
	}

	private void UpdateState(ICandleMessage candle, decimal upperBand, decimal bears, decimal bulls)
	{
	_prevBearsPower = bears;
	_prevBullsPower = bulls;

	_prevHigh2 = _prevHigh1;
	_prevUpper2 = _prevUpper1;
	_prevHigh1 = candle.HighPrice;
	_prevUpper1 = upperBand;

	_candleCount++;
	}
}
