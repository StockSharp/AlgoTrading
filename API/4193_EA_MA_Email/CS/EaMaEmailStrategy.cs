using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the EMA cross email alerts from the MQL expert.
/// It logs a message that simulates the original email notification whenever
/// the selected EMA pairs cross based on candle open prices.
/// </summary>
public class EaMaEmailStrategy : Strategy
{
	private static readonly Dictionary<TimeSpan, string> _periodNames = new()
	{
		[TimeSpan.FromMinutes(1)] = "PERIOD_M1",
		[TimeSpan.FromMinutes(5)] = "PERIOD_M5",
		[TimeSpan.FromMinutes(15)] = "PERIOD_M15",
		[TimeSpan.FromMinutes(30)] = "PERIOD_M30",
		[TimeSpan.FromHours(1)] = "PERIOD_H1",
		[TimeSpan.FromHours(4)] = "PERIOD_H4",
		[TimeSpan.FromDays(1)] = "PERIOD_D1",
		[TimeSpan.FromDays(7)] = "PERIOD_W1",
		[TimeSpan.FromDays(30)] = "PERIOD_MN1",
	};

	private readonly StrategyParam<bool> _sendEmailAlert;
	private readonly StrategyParam<bool> _monitor20Over50;
	private readonly StrategyParam<bool> _monitor20Over100;
	private readonly StrategyParam<bool> _monitor20Over200;
	private readonly StrategyParam<bool> _monitor50Over100;
	private readonly StrategyParam<bool> _monitor50Over200;
	private readonly StrategyParam<bool> _monitor100Over200;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema20 = null!;
	private ExponentialMovingAverage _ema50 = null!;
	private ExponentialMovingAverage _ema100 = null!;
	private ExponentialMovingAverage _ema200 = null!;

	private decimal? _prevEma20;
	private decimal? _prevEma50;
	private decimal? _prevEma100;
	private decimal? _prevEma200;
	private string _periodDescription = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="EaMaEmailStrategy"/>.
	/// </summary>
	public EaMaEmailStrategy()
	{
		_sendEmailAlert = Param(nameof(SendEmailAlert), true)
			.SetDisplay("Send Email Alert", "Log messages that emulate the email notification", "Notifications")
			.SetCanOptimize(true);

		_monitor20Over50 = Param(nameof(Monitor20Over50), false)
			.SetDisplay("Monitor EMA 20 vs 50", "Track crossovers between EMA 20 and EMA 50", "Pairs")
			.SetCanOptimize(true);

		_monitor20Over100 = Param(nameof(Monitor20Over100), false)
			.SetDisplay("Monitor EMA 20 vs 100", "Track crossovers between EMA 20 and EMA 100", "Pairs")
			.SetCanOptimize(true);

		_monitor20Over200 = Param(nameof(Monitor20Over200), false)
			.SetDisplay("Monitor EMA 20 vs 200", "Track crossovers between EMA 20 and EMA 200", "Pairs")
			.SetCanOptimize(true);

		_monitor50Over100 = Param(nameof(Monitor50Over100), false)
			.SetDisplay("Monitor EMA 50 vs 100", "Track crossovers between EMA 50 and EMA 100", "Pairs")
			.SetCanOptimize(true);

		_monitor50Over200 = Param(nameof(Monitor50Over200), true)
			.SetDisplay("Monitor EMA 50 vs 200", "Track crossovers between EMA 50 and EMA 200", "Pairs")
			.SetCanOptimize(true);

		_monitor100Over200 = Param(nameof(Monitor100Over200), false)
			.SetDisplay("Monitor EMA 100 vs 200", "Track crossovers between EMA 100 and EMA 200", "Pairs")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for EMA calculations", "General");
	}

	/// <summary>
	/// Enables or disables simulated email alerts.
	/// </summary>
	public bool SendEmailAlert
	{
		get => _sendEmailAlert.Value;
		set => _sendEmailAlert.Value = value;
	}

	/// <summary>
	/// Tracks crossovers between EMA 20 and EMA 50.
	/// </summary>
	public bool Monitor20Over50
	{
		get => _monitor20Over50.Value;
		set => _monitor20Over50.Value = value;
	}

	/// <summary>
	/// Tracks crossovers between EMA 20 and EMA 100.
	/// </summary>
	public bool Monitor20Over100
	{
		get => _monitor20Over100.Value;
		set => _monitor20Over100.Value = value;
	}

	/// <summary>
	/// Tracks crossovers between EMA 20 and EMA 200.
	/// </summary>
	public bool Monitor20Over200
	{
		get => _monitor20Over200.Value;
		set => _monitor20Over200.Value = value;
	}

	/// <summary>
	/// Tracks crossovers between EMA 50 and EMA 100.
	/// </summary>
	public bool Monitor50Over100
	{
		get => _monitor50Over100.Value;
		set => _monitor50Over100.Value = value;
	}

	/// <summary>
	/// Tracks crossovers between EMA 50 and EMA 200.
	/// </summary>
	public bool Monitor50Over200
	{
		get => _monitor50Over200.Value;
		set => _monitor50Over200.Value = value;
	}

	/// <summary>
	/// Tracks crossovers between EMA 100 and EMA 200.
	/// </summary>
	public bool Monitor100Over200
	{
		get => _monitor100Over200.Value;
		set => _monitor100Over200.Value = value;
	}

	/// <summary>
	/// Candle type used for EMA calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_prevEma20 = null;
		_prevEma50 = null;
		_prevEma100 = null;
		_prevEma200 = null;
		_periodDescription = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_periodDescription = DescribeCandlePeriod();

		_ema20 = new ExponentialMovingAverage
		{
			Length = 20,
			CandlePrice = CandlePrice.Open,
		};

		_ema50 = new ExponentialMovingAverage
		{
			Length = 50,
			CandlePrice = CandlePrice.Open,
		};

		_ema100 = new ExponentialMovingAverage
		{
			Length = 100,
			CandlePrice = CandlePrice.Open,
		};

		_ema200 = new ExponentialMovingAverage
		{
			Length = 200,
			CandlePrice = CandlePrice.Open,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema20, _ema50, _ema100, _ema200, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema20);
			DrawIndicator(area, _ema50);
			DrawIndicator(area, _ema100);
			DrawIndicator(area, _ema200);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema20Value, decimal ema50Value, decimal ema100Value, decimal ema200Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema20.IsFormed || !_ema50.IsFormed || !_ema100.IsFormed || !_ema200.IsFormed)
		{
			_prevEma20 = ema20Value;
			_prevEma50 = ema50Value;
			_prevEma100 = ema100Value;
			_prevEma200 = ema200Value;
			return;
		}

		if (_prevEma20 is null || _prevEma50 is null || _prevEma100 is null || _prevEma200 is null)
		{
			_prevEma20 = ema20Value;
			_prevEma50 = ema50Value;
			_prevEma100 = ema100Value;
			_prevEma200 = ema200Value;
			return;
		}

		DetectCross(Monitor20Over50, _prevEma20.Value, ema20Value, _prevEma50.Value, ema50Value, "20", "50", candle);
		DetectCross(Monitor20Over100, _prevEma20.Value, ema20Value, _prevEma100.Value, ema100Value, "20", "100", candle);
		DetectCross(Monitor20Over200, _prevEma20.Value, ema20Value, _prevEma200.Value, ema200Value, "20", "200", candle);
		DetectCross(Monitor50Over100, _prevEma50.Value, ema50Value, _prevEma100.Value, ema100Value, "50", "100", candle);
		DetectCross(Monitor50Over200, _prevEma50.Value, ema50Value, _prevEma200.Value, ema200Value, "50", "200", candle);
		DetectCross(Monitor100Over200, _prevEma100.Value, ema100Value, _prevEma200.Value, ema200Value, "100", "200", candle);

		_prevEma20 = ema20Value;
		_prevEma50 = ema50Value;
		_prevEma100 = ema100Value;
		_prevEma200 = ema200Value;
	}

	private void DetectCross(bool isEnabled, decimal previousFast, decimal currentFast, decimal previousSlow, decimal currentSlow, string fastLabel, string slowLabel, ICandleMessage candle)
	{
		if (!isEnabled || !SendEmailAlert)
			return;

		if (previousFast < previousSlow && currentFast > currentSlow)
		{
			LogAlert(fastLabel, slowLabel, ">", candle);
		}
		else if (previousFast > previousSlow && currentFast < currentSlow)
		{
			LogAlert(fastLabel, slowLabel, "<", candle);
		}
	}

	private void LogAlert(string fastLabel, string slowLabel, string direction, ICandleMessage candle)
	{
		var securityId = Security?.Id ?? "Unknown";
		var period = string.IsNullOrEmpty(_periodDescription) ? CandleType.ToString() : _periodDescription;
		var subject = $"{securityId} {fastLabel}{direction}{slowLabel} {period}";
		var body = $"Date and Time: {candle.CloseTime:yyyy-MM-dd HH:mm:ss}; Instrument: {securityId}; Close: {candle.ClosePrice:0.#####}";

		// Write to the log to emulate the email alert from the original expert advisor.
		AddInfoLog($"Email alert -> Subject: '{subject}'. Body: '{body}'.");
	}

	private string DescribeCandlePeriod()
	{
		if (CandleType.Arg is TimeSpan span && _periodNames.TryGetValue(span, out var name))
			return name;

		return CandleType.ToString();
	}
}
