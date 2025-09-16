using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that replicates the Color XMUV expert advisor with a trading session filter.
/// </summary>
public class ColorXmuvTimeStrategy : Strategy
{
	private const int _maxColorHistory = 64;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<SmoothMethod> _xmaMethod;
	private readonly StrategyParam<int> _xLength;
	private readonly StrategyParam<int> _xPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private readonly List<TrendColor> _colorHistory = new();

	private IIndicator _xma = null!;
	private decimal? _previousXmuv;

	/// <summary>
	/// Type of candles for the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume for new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on bearish signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on bullish signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Enable restriction of trading by time window.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Start hour for the trading session (00-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Start minute for the trading session (00-59).
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// End hour for the trading session (00-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// End minute for the trading session (00-59).
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Smoothing method used by the Color XMUV line.
	/// </summary>
	public SmoothMethod XmaMethod
	{
		get => _xmaMethod.Value;
		set => _xmaMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing window.
	/// </summary>
	public int XLength
	{
		get => _xLength.Value;
		set => _xLength.Value = value;
	}

	/// <summary>
	/// Auxiliary phase parameter retained from the original expert advisor.
	/// </summary>
	public int XPhase
	{
		get => _xPhase.Value;
		set => _xPhase.Value = value;
	}

	/// <summary>
	/// Number of completed bars to delay signal confirmation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop loss size in points (converted to absolute price using the instrument price step).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit size in points (converted to absolute price using the instrument price step).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ColorXmuvTimeStrategy"/>.
	/// </summary>
	public ColorXmuvTimeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Source candles for the Color XMUV line", "General");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Size of market orders", "Trading");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
		.SetDisplay("Enable Long Entries", "Allow entering long positions", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
		.SetDisplay("Enable Short Entries", "Allow entering short positions", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
		.SetDisplay("Close Longs", "Close long positions on bearish flips", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
		.SetDisplay("Close Shorts", "Close short positions on bullish flips", "Trading");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time Filter", "Restrict trading to the specified session", "Time Filter");

		_startHour = Param(nameof(StartHour), 0)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Trading session start hour", "Time Filter");

		_startMinute = Param(nameof(StartMinute), 0)
		.SetRange(0, 59)
		.SetDisplay("Start Minute", "Trading session start minute", "Time Filter");

		_endHour = Param(nameof(EndHour), 23)
		.SetRange(0, 23)
		.SetDisplay("End Hour", "Trading session end hour", "Time Filter");

		_endMinute = Param(nameof(EndMinute), 59)
		.SetRange(0, 59)
		.SetDisplay("End Minute", "Trading session end minute", "Time Filter");

		_xmaMethod = Param(nameof(XmaMethod), SmoothMethod.Sma)
		.SetDisplay("Smoothing Method", "Algorithm for the Color XMUV line", "Indicator");

		_xLength = Param(nameof(XLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Smoothing length", "Indicator");

		_xPhase = Param(nameof(XPhase), 15)
		.SetDisplay("Phase", "Additional phase parameter for exotic smoothers", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(0, 10)
		.SetDisplay("Signal Bar", "Number of completed bars to delay signals", "Indicator");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk");
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

		_colorHistory.Clear();
		_previousXmuv = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_xma = CreateMovingAverage(XmaMethod, XLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(CreateTakeProfitUnit(), CreateStopLossUnit());
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var price = CalculateSignalPrice(candle);
		var indicatorValue = _xma.Process(price, candle.OpenTime, true);

		if (!_xma.IsFormed)
		{
			_previousXmuv = indicatorValue.ToDecimal();
			return;
		}

		var xmuv = indicatorValue.ToDecimal();
		var color = DetermineColor(xmuv);
		StoreColor(color);
		_previousXmuv = xmuv;

		if (!TryGetSignalColors(SignalBar, out var currentColor, out var previousColor))
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		var inSession = !UseTimeFilter || IsInsideSession(candle.CloseTime);

		if (!inSession)
		{
			ForceExitIfNeeded();
			return;
		}

		var bullishFlip = currentColor == TrendColor.Bullish && previousColor != TrendColor.Bullish;
		var bearishFlip = currentColor == TrendColor.Bearish && previousColor != TrendColor.Bearish;

		if (bullishFlip)
		{
			if (EnableSellExits && Position < 0)
			{
				BuyMarket(-Position);
			}

			if (EnableBuyEntries && Position <= 0)
			{
				var volume = OrderVolume + (Position < 0 ? -Position : 0m);
				if (volume > 0)
				{
					BuyMarket(volume);
				}
			}
		}
		else if (bearishFlip)
		{
			if (EnableBuyExits && Position > 0)
			{
				SellMarket(Position);
			}

			if (EnableSellEntries && Position >= 0)
			{
				var volume = OrderVolume + (Position > 0 ? Position : 0m);
				if (volume > 0)
				{
					SellMarket(volume);
				}
			}
		}
	}

	private decimal CalculateSignalPrice(ICandleMessage candle)
	{
		if (candle.ClosePrice < candle.OpenPrice)
		{
			return (candle.LowPrice + candle.ClosePrice) / 2m;
		}

		if (candle.ClosePrice > candle.OpenPrice)
		{
			return (candle.HighPrice + candle.ClosePrice) / 2m;
		}

		return candle.ClosePrice;
	}

	private TrendColor DetermineColor(decimal currentXmuv)
	{
		if (_previousXmuv is not decimal previous)
		{
			return TrendColor.Neutral;
		}

		if (currentXmuv > previous)
		{
			return TrendColor.Bullish;
		}

		if (currentXmuv < previous)
		{
			return TrendColor.Bearish;
		}

		return TrendColor.Neutral;
	}

	private void StoreColor(TrendColor color)
	{
		var maxSize = Math.Clamp(SignalBar + 2, 2, _maxColorHistory);
		_colorHistory.Add(color);

		if (_colorHistory.Count > maxSize)
		{
			_colorHistory.RemoveAt(0);
		}
	}

	private bool TryGetSignalColors(int offset, out TrendColor current, out TrendColor previous)
	{
		current = TrendColor.Neutral;
		previous = TrendColor.Neutral;

		var count = _colorHistory.Count;
		if (count <= offset)
		{
			return false;
		}

		var index = count - 1 - offset;
		if (index <= 0)
		{
			return false;
		}

		current = _colorHistory[index];
		previous = _colorHistory[index - 1];
		return true;
	}

	private bool IsInsideSession(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var moment = time.TimeOfDay;

		if (start == end)
		{
			return moment >= start && moment < end;
		}

		if (start < end)
		{
			return moment >= start && moment <= end;
		}

		return moment >= start || moment <= end;
	}

	private void ForceExitIfNeeded()
	{
		if (Position > 0 && EnableBuyExits)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && EnableSellExits)
		{
			BuyMarket(-Position);
		}
	}

	private Unit CreateStopLossUnit()
	{
		if (StopLossPoints <= 0 || Security?.PriceStep is not decimal step || step <= 0)
		{
			return default;
		}

		return new Unit(step * StopLossPoints, UnitTypes.Absolute);
	}

	private Unit CreateTakeProfitUnit()
	{
		if (TakeProfitPoints <= 0 || Security?.PriceStep is not decimal step || step <= 0)
		{
			return default;
		}

		return new Unit(step * TakeProfitPoints, UnitTypes.Absolute);
	}

	private IIndicator CreateMovingAverage(SmoothMethod method, int length)
	{
		return method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = length },
			SmoothMethod.Jjma => new JurikMovingAverage { Length = length },
			SmoothMethod.Jurx => new JurikMovingAverage { Length = length },
			SmoothMethod.Parma => new WeightedMovingAverage { Length = length },
			SmoothMethod.T3 => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Vidya => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private enum TrendColor
	{
		Bearish = 0,
		Neutral = 1,
		Bullish = 2
	}

	/// <summary>
	/// Smoothing methods supported by the Color XMUV indicator.
	/// </summary>
	public enum SmoothMethod
	{
		Sma,
		Ema,
		Smma,
		Lwma,
		Jjma,
		Jurx,
		Parma,
		T3,
		Vidya,
		Ama
	}
}
