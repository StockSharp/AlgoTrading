namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Exit modes for Compass Line strategy.
/// </summary>
public enum CompassLineExitMode
{
	/// <summary>
	/// Do not close positions on indicator signals.
	/// </summary>
	None = 0,

	/// <summary>
	/// Close when both Compass and Follow Line signals reverse.
	/// </summary>
	BothIndicators = 1,

	/// <summary>
	/// Close only when Follow Line reverses.
	/// </summary>
	FollowLineOnly = 2,

	/// <summary>
	/// Close only when Compass turns.
	/// </summary>
	CompassOnly = 3,
}

/// <summary>
/// Compass Line strategy combines Follow Line and Compass trend filters with optional time window and protective stops.
/// </summary>
public class CompassLineStrategy : Strategy
{
	private readonly StrategyParam<int> _followBbPeriod;
	private readonly StrategyParam<decimal> _followBbDeviation;
	private readonly StrategyParam<int> _followAtrPeriod;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<int> _compassPeriod;
	private readonly StrategyParam<int> _closeMode;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<string> _session;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _followLine;
	private decimal? _prevFollowLine;
	private int _followTrend;
	private int _prevFollowTrend;
	private int _followBias;

	private decimal? _compassLastValue;
	private decimal? _compassPrevBuffer;
	private int _compassTrend;
	private int _prevCompassTrend;

	private SimpleMovingAverage _compassAverage = null!;
	private Highest _compassHigh = null!;
	private Lowest _compassLow = null!;
	private SimpleMovingAverage _compassSmooth1 = null!;
	private SimpleMovingAverage _compassSmooth2 = null!;
	private int _smoothingLength;

	/// <summary>
	/// Follow Line Bollinger period.
	/// </summary>
	public int FollowBbPeriod
	{
		get => _followBbPeriod.Value;
		set => _followBbPeriod.Value = value;
	}

	/// <summary>
	/// Follow Line Bollinger deviation.
	/// </summary>
	public decimal FollowBbDeviation
	{
		get => _followBbDeviation.Value;
		set => _followBbDeviation.Value = value;
	}

	/// <summary>
	/// ATR period for Follow Line offset.
	/// </summary>
	public int FollowAtrPeriod
	{
		get => _followAtrPeriod.Value;
		set => _followAtrPeriod.Value = value;
	}

	/// <summary>
	/// Enable ATR displacement for Follow Line.
	/// </summary>
	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	/// <summary>
	/// Compass moving average period.
	/// </summary>
	public int CompassPeriod
	{
		get => _compassPeriod.Value;
		set => _compassPeriod.Value = value;
	}

	/// <summary>
	/// Close logic selection.
	/// </summary>
	public CompassLineExitMode CloseMode
	{
		get => (CompassLineExitMode)_closeMode.Value;
		set => _closeMode.Value = (int)value;
	}

	/// <summary>
	/// Enable trading session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading session in HHmm-HHmm format.
	/// </summary>
	public string Session
	{
		get => _session.Value;
		set => _session.Value = value;
	}

	/// <summary>
	/// Take profit distance in steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="CompassLineStrategy"/>.
	/// </summary>
	public CompassLineStrategy()
	{
		_followBbPeriod = Param(nameof(FollowBbPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Follow BB Period", "Bollinger length for Follow Line", "Follow Line");

		_followBbDeviation = Param(nameof(FollowBbDeviation), 1m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Follow BB Deviation", "Bollinger deviation for Follow Line", "Follow Line");

		_followAtrPeriod = Param(nameof(FollowAtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Follow ATR Period", "ATR length for Follow Line offset", "Follow Line");

		_useAtrFilter = Param(nameof(UseAtrFilter), false)
			.SetDisplay("Use ATR Filter", "Offset Follow Line with ATR", "Follow Line");

		_compassPeriod = Param(nameof(CompassPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Compass MA Period", "SMA period for Compass", "Compass");

		_closeMode = Param(nameof(CloseMode), (int)CompassLineExitMode.None)
			.SetDisplay("Close Mode", "Signal based exit mode", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Session", "Enable trading session filter", "Risk");

		_session = Param(nameof(Session), "0000-2400")
			.SetDisplay("Session", "Trading session", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0)
			.SetDisplay("Take Profit", "Target distance in steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_stopLoss = Param(nameof(StopLoss), 0)
			.SetDisplay("Stop Loss", "Protective distance in steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 400, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Analysis timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_followLine = null;
		_prevFollowLine = null;
		_followTrend = 0;
		_prevFollowTrend = 0;
		_followBias = 0;
		_compassLastValue = null;
		_compassPrevBuffer = null;
		_compassTrend = 0;
		_prevCompassTrend = 0;
		_compassAverage = null!;
		_compassHigh = null!;
		_compassLow = null!;
		_compassSmooth1 = null!;
		_compassSmooth2 = null!;
		_smoothingLength = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var followBb = new BollingerBands { Length = FollowBbPeriod, Width = FollowBbDeviation };
		var followAtr = new AverageTrueRange { Length = FollowAtrPeriod };

		_compassAverage = new SimpleMovingAverage { Length = CompassPeriod };
		_compassHigh = new Highest { Length = CompassPeriod, CandlePrice = CandlePrice.High };
		_compassLow = new Lowest { Length = CompassPeriod, CandlePrice = CandlePrice.Low };
		_smoothingLength = Math.Max(1, (int)Math.Round(CompassPeriod / 3m, MidpointRounding.AwayFromZero));
		_compassSmooth1 = new SimpleMovingAverage { Length = _smoothingLength };
		_compassSmooth2 = new SimpleMovingAverage { Length = _smoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(followBb, followAtr, _compassAverage, _compassHigh, _compassLow, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal atr, decimal compassMa, decimal highestHigh, decimal lowestLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Unused indicator outputs are intentionally ignored.
		_ = middle;
		_ = compassMa;

		UpdateFollowLine(candle, upper, lower, atr);
		UpdateCompass(candle, highestHigh, lowestLow);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StoreState();
			return;
		}

		if (UseTimeFilter && !InSession(candle.CloseTime))
		{
			StoreState();
			return;
		}

		if (!_compassSmooth2.IsFormed || (_followTrend == 0 && _prevFollowTrend == 0))
		{
			StoreState();
			return;
		}

		if (Position > 0 && ShouldCloseLong())
			SellMarket(Position);
		else if (Position < 0 && ShouldCloseShort())
			BuyMarket(Math.Abs(Position));

		var price = candle.ClosePrice;

		if (_followTrend > 0 && _compassTrend > 0 && Position <= 0)
		{
			var volume = Volume + Math.Max(0m, -Position);
			BuyMarket(volume);
			var resulting = Position + volume;
			if (TakeProfit > 0)
				SetTakeProfit(TakeProfit, price, resulting);
			if (StopLoss > 0)
				SetStopLoss(StopLoss, price, resulting);
		}
		else if (_followTrend < 0 && _compassTrend < 0 && Position >= 0)
		{
			var volume = Volume + Math.Max(0m, Position);
			SellMarket(volume);
			var resulting = Position - volume;
			if (TakeProfit > 0)
				SetTakeProfit(TakeProfit, price, resulting);
			if (StopLoss > 0)
				SetStopLoss(StopLoss, price, resulting);
		}

		StoreState();
	}

	private void UpdateFollowLine(ICandleMessage candle, decimal upper, decimal lower, decimal atr)
	{
		var bbSignal = 0;
		if (candle.ClosePrice > upper)
			bbSignal = 1;
		else if (candle.ClosePrice < lower)
			bbSignal = -1;

		if (bbSignal != 0)
		{
			var target = bbSignal == 1
				? (UseAtrFilter ? candle.LowPrice - atr : candle.LowPrice)
				: (UseAtrFilter ? candle.HighPrice + atr : candle.HighPrice);

			if (_followLine is null)
				_followLine = target;
			else if (bbSignal == 1)
				_followLine = Math.Max(target, _followLine.Value);
			else
				_followLine = Math.Min(target, _followLine.Value);

			_followBias = bbSignal;
		}

		_followTrend = 0;
		if (_followLine.HasValue && _prevFollowLine.HasValue)
		{
			if (_followLine > _prevFollowLine)
				_followTrend = 1;
			else if (_followLine < _prevFollowLine)
				_followTrend = -1;
			else
				_followTrend = _prevFollowTrend;
		}
		else if (_followLine.HasValue && _prevFollowLine is null && _followBias != 0)
		{
			_followTrend = _followBias;
		}
	}

	private void UpdateCompass(ICandleMessage candle, decimal highestHigh, decimal lowestLow)
	{
		if (!_compassAverage.IsFormed || !_compassHigh.IsFormed || !_compassLow.IsFormed)
			return;

		var range = highestHigh - lowestLow;
		if (range <= 0m)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var normalized = (median - lowestLow) / range - 0.5m;
		var baseValue = 0.66m * normalized + 0.67m * (_compassLastValue ?? 0m);
		baseValue = Math.Clamp(baseValue, -0.999m, 0.999m);
		_compassLastValue = baseValue;

		var ratio = (1m + baseValue) / (1m - baseValue);
		var logValue = (decimal)Math.Log((double)ratio);
		var buffer = 0.5m * logValue + 0.5m * (_compassPrevBuffer ?? 0m);
		_compassPrevBuffer = buffer;

		var rawSignal = buffer > 0m ? 10m : -10m;
		var smooth1 = _compassSmooth1.Process(rawSignal, candle.OpenTime, true).ToDecimal();
		var smooth2 = _compassSmooth2.Process(smooth1, candle.OpenTime, true).ToDecimal();

		if (_compassSmooth2.IsFormed)
		{
			if (smooth2 > 0m)
				_compassTrend = 1;
			else if (smooth2 < 0m)
				_compassTrend = -1;
			else
				_compassTrend = _prevCompassTrend;
		}
	}

	private bool ShouldCloseLong()
	{
		return CloseMode switch
		{
			CompassLineExitMode.BothIndicators => _followTrend < 0 && _compassTrend < 0,
			CompassLineExitMode.FollowLineOnly => _followTrend < 0,
			CompassLineExitMode.CompassOnly => _compassTrend < 0,
			_ => false,
		};
	}

	private bool ShouldCloseShort()
	{
		return CloseMode switch
		{
			CompassLineExitMode.BothIndicators => _followTrend > 0 && _compassTrend > 0,
			CompassLineExitMode.FollowLineOnly => _followTrend > 0,
			CompassLineExitMode.CompassOnly => _compassTrend > 0,
			_ => false,
		};
	}

	private void StoreState()
	{
		_prevFollowLine = _followLine;
		_prevFollowTrend = _followTrend;
		_prevCompassTrend = _compassTrend;
	}

	private bool InSession(DateTimeOffset time)
	{
		ParseSession(Session, out var start, out var end);
		var t = time.TimeOfDay;
		return start <= end ? t >= start && t <= end : t >= start || t <= end;
	}

	private static void ParseSession(string input, out TimeSpan start, out TimeSpan end)
	{
		start = TimeSpan.Zero;
		end = TimeSpan.FromHours(24);
		if (string.IsNullOrWhiteSpace(input))
			return;

		var parts = input.Split('-', ':');
		if (parts.Length < 2)
			return;

		TimeSpan.TryParseExact(parts[0], "hhmm", null, out start);
		TimeSpan.TryParseExact(parts[1], "hhmm", null, out end);
	}
}
