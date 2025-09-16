using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blau Tick Volume Index strategy with optional trading window and stop protections.
/// Generates reversal entries when Blau TVI changes slope and optionally enforces timed trading sessions.
/// </summary>
public class BlauTviTimedReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<BlauTviMaType> _maType;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _length3;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;
	private readonly StrategyParam<bool> _enableTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private LengthIndicator<decimal> _upTicksMa1 = null!;
	private LengthIndicator<decimal> _downTicksMa1 = null!;
	private LengthIndicator<decimal> _upTicksMa2 = null!;
	private LengthIndicator<decimal> _downTicksMa2 = null!;
	private LengthIndicator<decimal> _tviMa = null!;

	private readonly List<decimal> _tviHistory = new();

	/// <summary>
	/// Trade volume in contracts or lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used for Blau TVI calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Smoothing method used in Blau TVI moving averages.
	/// </summary>
	public BlauTviMaType MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Length of the first smoothing stage.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	/// <summary>
	/// Length of the second smoothing stage.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	/// <summary>
	/// Length of the final smoothing stage.
	/// </summary>
	public int Length3
	{
		get => _length3.Value;
		set => _length3.Value = value;
	}

	/// <summary>
	/// Signal bar offset (1 = previous completed candle).
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	/// <summary>
	/// Enable closing long positions on opposite signals.
	/// </summary>
	public bool EnableBuyClose
	{
		get => _enableBuyClose.Value;
		set => _enableBuyClose.Value = value;
	}

	/// <summary>
	/// Enable closing short positions on opposite signals.
	/// </summary>
	public bool EnableSellClose
	{
		get => _enableSellClose.Value;
		set => _enableSellClose.Value = value;
	}

	/// <summary>
	/// Enable intraday trading window filtering.
	/// </summary>
	public bool EnableTimeFilter
	{
		get => _enableTimeFilter.Value;
		set => _enableTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading window start hour (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window start minute (0-59).
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Trading window end hour (0-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Trading window end minute (0-59).
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="BlauTviTimedReversalStrategy"/>.
	/// </summary>
	public BlauTviTimedReversalStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for Blau TVI", "General");

		_maType = Param(nameof(MaType), BlauTviMaType.Exponential)
		.SetDisplay("MA Type", "Smoothing method for Blau TVI", "Indicators");

		_length1 = Param(nameof(Length1), 12)
		.SetGreaterThanZero()
		.SetDisplay("Stage 1 Length", "First smoothing stage length", "Indicators")
		.SetCanOptimize(true);

		_length2 = Param(nameof(Length2), 12)
		.SetGreaterThanZero()
		.SetDisplay("Stage 2 Length", "Second smoothing stage length", "Indicators")
		.SetCanOptimize(true);

		_length3 = Param(nameof(Length3), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stage 3 Length", "Final smoothing stage length", "Indicators")
		.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Offset from the current candle", "Signals");

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
		.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
		.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
		.SetDisplay("Close Long", "Allow closing long positions on bearish slope", "Trading");

		_enableSellClose = Param(nameof(EnableSellClose), true)
		.SetDisplay("Close Short", "Allow closing short positions on bullish slope", "Trading");

		_enableTimeFilter = Param(nameof(EnableTimeFilter), true)
		.SetDisplay("Time Filter", "Restrict trading to a session", "Timing");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Trading window start hour", "Timing");

		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Trading window start minute", "Timing");

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Trading window end hour", "Timing");

		_endMinute = Param(nameof(EndMinute), 59)
		.SetDisplay("End Minute", "Trading window end minute", "Timing");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss Points", "Protective stop in price points", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit Points", "Profit target in price points", "Protection");
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
		_tviHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_upTicksMa1 = CreateMovingAverage(MaType, Length1);
		_downTicksMa1 = CreateMovingAverage(MaType, Length1);
		_upTicksMa2 = CreateMovingAverage(MaType, Length2);
		_downTicksMa2 = CreateMovingAverage(MaType, Length2);
		_tviMa = CreateMovingAverage(MaType, Length3);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep > 0m)
		{
			StartProtection(
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute) : null,
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * priceStep, UnitTypes.Absolute) : null);
		}
		else
		{
			StartProtection();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep == 0m)
		priceStep = 1m;

		var volume = candle.TotalVolume;
		if (volume <= 0m)
		{
			UpdateHistory(0m);
			return;
		}

		var priceDelta = candle.ClosePrice - candle.OpenPrice;
		var upTicksRaw = (volume + priceDelta / priceStep) / 2m;
		var downTicksRaw = volume - upTicksRaw;

		var up1Value = _upTicksMa1.Process(upTicksRaw);
		var down1Value = _downTicksMa1.Process(downTicksRaw);

		if (!TryGetFinalValue(up1Value, out var up1) || !TryGetFinalValue(down1Value, out var down1))
		return;

		var up2Value = _upTicksMa2.Process(up1);
		var down2Value = _downTicksMa2.Process(down1);

		if (!TryGetFinalValue(up2Value, out var up2) || !TryGetFinalValue(down2Value, out var down2))
		return;

		var denominator = up2 + down2;
		if (denominator == 0m)
		return;

		var tviRaw = 100m * (up2 - down2) / denominator;
		var tviValue = _tviMa.Process(tviRaw);

		if (!TryGetFinalValue(tviValue, out var tvi))
		return;

		UpdateHistory(tvi);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var required = SignalBar + 3;
		if (_tviHistory.Count < required)
		return;

		var value0 = _tviHistory[^ (SignalBar + 1)];
		var value1 = _tviHistory[^ (SignalBar + 2)];
		var value2 = _tviHistory[^ (SignalBar + 3)];

		var slopeUp = value1 < value2;
		var slopeDown = value1 > value2;
		var turningUp = slopeUp && value0 > value1;
		var turningDown = slopeDown && value0 < value1;

		if (EnableTimeFilter && !IsWithinTradingWindow(candle.OpenTime) && Position != 0)
		CloseAllPositions();

		if (EnableSellClose && slopeUp && Position < 0)
		CloseShort();

		if (EnableBuyClose && slopeDown && Position > 0)
		CloseLong();

		var inWindow = !EnableTimeFilter || IsWithinTradingWindow(candle.OpenTime);

		if (inWindow)
		{
			if (EnableBuyOpen && turningUp && Position <= 0)
			{
				var volumeToBuy = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
				if (volumeToBuy > 0m)
				BuyMarket(volumeToBuy);
			}
			else if (EnableSellOpen && turningDown && Position >= 0)
			{
				var volumeToSell = Volume + (Position > 0 ? Position : 0m);
				if (volumeToSell > 0m)
				SellMarket(volumeToSell);
			}
		}
	}

	private static bool TryGetFinalValue(IIndicatorValue value, out decimal result)
	{
		if (!value.IsFinal || !value.TryGetValue(out result))
		{
			result = 0m;
			return false;
		}

		return true;
	}

	private void UpdateHistory(decimal value)
	{
		_tviHistory.Add(value);

		var maxSize = Math.Max(8, SignalBar + 5);
		if (_tviHistory.Count > maxSize)
		_tviHistory.RemoveRange(0, _tviHistory.Count - maxSize);
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!EnableTimeFilter)
		return true;

		var start = new TimeSpan(StartHour.Clamp(0, 23), StartMinute.Clamp(0, 59), 0);
		var end = new TimeSpan(EndHour.Clamp(0, 23), EndMinute.Clamp(0, 59), 0);
		var current = time.TimeOfDay;

		if (start == end)
		return current >= start && current <= end;

		if (start < end)
		return current >= start && current <= end;

		return current >= start || current <= end;
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void CloseLong()
	{
		if (Position > 0)
		SellMarket(Position);
	}

	private void CloseShort()
	{
		if (Position < 0)
		BuyMarket(Math.Abs(Position));
	}

	private static LengthIndicator<decimal> CreateMovingAverage(BlauTviMaType type, int length)
	{
		return type switch
		{
			BlauTviMaType.Simple => new SimpleMovingAverage { Length = length },
			BlauTviMaType.Smoothed => new SmoothedMovingAverage { Length = length },
			BlauTviMaType.Weighted => new WeightedMovingAverage { Length = length },
			BlauTviMaType.Jurik => new JurikMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported moving average types for Blau TVI smoothing.
	/// </summary>
	public enum BlauTviMaType
	{
		/// <summary>
		/// Exponential moving average (EMA).
		/// </summary>
		Exponential,

		/// <summary>
		/// Simple moving average (SMA).
		/// </summary>
		Simple,

		/// <summary>
		/// Smoothed moving average (SMMA/RMA).
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average (WMA).
		/// </summary>
		Weighted,

		/// <summary>
		/// Jurik moving average (JMA).
		/// </summary>
		Jurik
	}
}

internal static class TimeExtensions
{
	public static int Clamp(this int value, int min, int max)
	{
		if (value < min)
		return min;

		return value > max ? max : value;
	}
}
