namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving average crossover strategy converted from the MT4 expert "wajdyss MA expert v3".
/// Reproduces money management, session closing filters, and protective order handling.
/// </summary>
public class WajdyssMaExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<MovingAverageMethod> _fastMethod;
	private readonly StrategyParam<PriceSource> _fastPrice;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<MovingAverageMethod> _slowMethod;
	private readonly StrategyParam<PriceSource> _slowPrice;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _autoCloseOpposite;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _balanceReference;
	private readonly StrategyParam<int> _dailyCloseHour;
	private readonly StrategyParam<int> _dailyCloseMinute;
	private readonly StrategyParam<int> _fridayCloseHour;
	private readonly StrategyParam<int> _fridayCloseMinute;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _fastMa;
	private LengthIndicator<decimal> _slowMa;
	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();
	private decimal _pipSize;
	private TimeSpan? _timeFrame;
	private DateTimeOffset? _lastBuyTime;
	private DateTimeOffset? _lastSellTime;
	private MovingAverageMethod _currentFastMethod;
	private MovingAverageMethod _currentSlowMethod;

	/// <summary>
	/// Initializes a new instance of the <see cref="WajdyssMaExpertStrategy"/> class.
	/// </summary>
	public WajdyssMaExpertStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Length of the fast moving average.", "Indicators");

		_fastShift = Param(nameof(FastShift), 0)
		.SetNotNegative()
		.SetDisplay("Fast MA Shift", "Shift applied to the fast moving average.", "Indicators");

		_fastMethod = Param(nameof(FastMethod), MovingAverageMethod.Ema)
		.SetDisplay("Fast MA Method", "Smoothing method used for the fast moving average.", "Indicators");

		_fastPrice = Param(nameof(FastPriceType), PriceSource.Close)
		.SetDisplay("Fast Price", "Price source fed into the fast moving average.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Length of the slow moving average.", "Indicators");

		_slowShift = Param(nameof(SlowShift), 0)
		.SetNotNegative()
		.SetDisplay("Slow MA Shift", "Shift applied to the slow moving average.", "Indicators");

		_slowMethod = Param(nameof(SlowMethod), MovingAverageMethod.Ema)
		.SetDisplay("Slow MA Method", "Smoothing method used for the slow moving average.", "Indicators");

		_slowPrice = Param(nameof(SlowPriceType), PriceSource.Close)
		.SetDisplay("Slow Price", "Price source fed into the slow moving average.", "Indicators");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Distance to the profit target in pips (0 disables).", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Distance to the protective stop in pips (0 disables).", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips (0 disables).", "Risk");

		_autoCloseOpposite = Param(nameof(AutoCloseOpposite), true)
		.SetDisplay("Auto Close Opposite", "Close opposite positions before reversing.", "Trading");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Base order volume before money management.", "Trading");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Use Money Management", "Enable balance-proportional position sizing.", "Risk");

		_balanceReference = Param(nameof(BalanceReference), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Balance Reference", "Account balance divisor used when sizing trades.", "Risk");

		_dailyCloseHour = Param(nameof(DailyCloseHour), 23)
		.SetRange(0, 23)
		.SetDisplay("Daily Close Hour", "Hour after which positions are closed each day.", "Session");

		_dailyCloseMinute = Param(nameof(DailyCloseMinute), 45)
		.SetRange(0, 59)
		.SetDisplay("Daily Close Minute", "Minute of the daily close filter.", "Session");

		_fridayCloseHour = Param(nameof(FridayCloseHour), 22)
		.SetRange(0, 23)
		.SetDisplay("Friday Close Hour", "Hour after which Friday trades are closed.", "Session");

		_fridayCloseMinute = Param(nameof(FridayCloseMinute), 45)
		.SetRange(0, 59)
		.SetDisplay("Friday Close Minute", "Minute component of the Friday close filter.", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for the moving averages.", "General");
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average shift applied before evaluating signals.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Moving average method used for the fast line.
	/// </summary>
	public MovingAverageMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	/// <summary>
	/// Candle price fed into the fast moving average.
	/// </summary>
	public PriceSource FastPriceType
	{
		get => _fastPrice.Value;
		set => _fastPrice.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average shift applied before evaluating signals.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Moving average method used for the slow line.
	/// </summary>
	public MovingAverageMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	/// <summary>
	/// Candle price fed into the slow moving average.
	/// </summary>
	public PriceSource SlowPriceType
	{
		get => _slowPrice.Value;
		set => _slowPrice.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables closing opposite exposure before reversing.
	/// </summary>
	public bool AutoCloseOpposite
	{
		get => _autoCloseOpposite.Value;
		set => _autoCloseOpposite.Value = value;
	}

	/// <summary>
	/// Base trading volume before risk-based scaling.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Enables proportional money management when true.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Balance divisor used to scale the base trading volume.
	/// </summary>
	public decimal BalanceReference
	{
		get => _balanceReference.Value;
		set => _balanceReference.Value = value;
	}

	/// <summary>
	/// Hour after which daily trading is closed.
	/// </summary>
	public int DailyCloseHour
	{
		get => _dailyCloseHour.Value;
		set => _dailyCloseHour.Value = value;
	}

	/// <summary>
	/// Minute component of the daily close filter.
	/// </summary>
	public int DailyCloseMinute
	{
		get => _dailyCloseMinute.Value;
		set => _dailyCloseMinute.Value = value;
	}

	/// <summary>
	/// Hour after which Friday positions are closed.
	/// </summary>
	public int FridayCloseHour
	{
		get => _fridayCloseHour.Value;
		set => _fridayCloseHour.Value = value;
	}

	/// <summary>
	/// Minute component of the Friday close filter.
	/// </summary>
	public int FridayCloseMinute
	{
		get => _fridayCloseMinute.Value;
		set => _fridayCloseMinute.Value = value;
	}

	/// <summary>
	/// Candle type requested for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastHistory.Clear();
		_slowHistory.Clear();
		_fastMa = null;
		_slowMa = null;
		_pipSize = 0m;
		_timeFrame = null;
		_lastBuyTime = null;
		_lastSellTime = null;
		_currentFastMethod = FastMethod;
		_currentSlowMethod = SlowMethod;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = InitialVolume;
		_pipSize = CalculatePipSize();
		_timeFrame = GetTimeFrame();

		_currentFastMethod = FastMethod;
		_currentSlowMethod = SlowMethod;
		_fastMa = CreateMovingAverage(FastMethod, FastPeriod);
		_slowMa = CreateMovingAverage(SlowMethod, SlowPeriod);
		_fastHistory.Clear();
		_slowHistory.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_fastMa != null)
			{
				DrawIndicator(area, _fastMa);
			}

			if (_slowMa != null)
			{
				DrawIndicator(area, _slowMa);
			}

			DrawOwnTrades(area);
		}

		var takeProfit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;
		var stopLoss = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		var trailing = TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null;

		StartProtection(
		takeProfit: takeProfit,
		stopLoss: stopLoss,
		trailingStop: trailing,
		trailingStep: trailing,
		useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (_fastMa == null || _slowMa == null)
		return;

		if (FastMethod != _currentFastMethod)
		{
			_fastMa = CreateMovingAverage(FastMethod, FastPeriod);
			_fastHistory.Clear();
			_currentFastMethod = FastMethod;
		}

		if (SlowMethod != _currentSlowMethod)
		{
			_slowMa = CreateMovingAverage(SlowMethod, SlowPeriod);
			_slowHistory.Clear();
			_currentSlowMethod = SlowMethod;
		}

		if (_fastMa.Length != FastPeriod)
		_fastMa.Length = FastPeriod;

		if (_slowMa.Length != SlowPeriod)
		_slowMa.Length = SlowPeriod;

		var isFinal = candle.State == CandleStates.Finished;
		var fastValue = _fastMa.Process(GetPrice(candle, FastPriceType), candle.OpenTime, isFinal);
		var slowValue = _slowMa.Process(GetPrice(candle, SlowPriceType), candle.OpenTime, isFinal);

		if (!isFinal)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		_fastHistory.Add(fast);
		_slowHistory.Add(slow);
		TrimHistory(_fastHistory);
		TrimHistory(_slowHistory);

		var candleTime = candle.CloseTime;
		if (HandleSessionClose(candleTime))
		return;

		var signal = GetSignal();
		if (signal == 0)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var price = candle.ClosePrice;
		if (price <= 0m)
		return;

		var position = Position;
		if (signal > 0)
		{
			if (position > 0m)
			return;

			if (!CanOpenPosition(candleTime, true))
			return;

			var volume = GetOrderVolume(price);

			if (position < 0m)
			{
				if (!AutoCloseOpposite)
				return;

				volume += Math.Abs(position);
			}

			if (volume <= 0m)
			return;

			CancelActiveOrders();

			BuyMarket(volume);
			_lastBuyTime = candleTime;
		}
		else if (signal < 0)
		{
			if (position < 0m)
			return;

			if (!CanOpenPosition(candleTime, false))
			return;

			var volume = GetOrderVolume(price);

			if (position > 0m)
			{
				if (!AutoCloseOpposite)
				return;

				volume += position;
			}

			if (volume <= 0m)
			return;

			CancelActiveOrders();

			SellMarket(volume);
			_lastSellTime = candleTime;
		}
	}

	private int GetSignal()
	{
		var fastPrevious = GetHistoryValue(_fastHistory, FastShift, 1);
		var fastPrevPrev = GetHistoryValue(_fastHistory, FastShift, 2);
		var slowPrevious = GetHistoryValue(_slowHistory, SlowShift, 1);
		var slowPrevPrev = GetHistoryValue(_slowHistory, SlowShift, 2);

		if (fastPrevious is null || fastPrevPrev is null || slowPrevious is null || slowPrevPrev is null)
		return 0;

		var bullish = fastPrevious > slowPrevious && fastPrevPrev < slowPrevPrev;
		var bearish = fastPrevious < slowPrevious && fastPrevPrev > slowPrevPrev;

		if (bullish)
		return 1;

		if (bearish)
		return -1;

		return 0;
	}

	private bool HandleSessionClose(DateTimeOffset time)
	{
		var shouldClose = IsFridayClose(time) || IsDailyClose(time);
		if (!shouldClose)
		return false;

		CancelActiveOrders();
		CloseOpenPosition();
		return true;
	}

	private bool CanOpenPosition(DateTimeOffset time, bool isBuy)
	{
		var frame = _timeFrame;
		if (frame is null || frame.Value <= TimeSpan.Zero)
		return true;

		var lastTime = isBuy ? _lastBuyTime : _lastSellTime;
		if (lastTime is null)
		return true;

		return time - lastTime.Value >= frame.Value;
	}

	private void CloseOpenPosition()
	{
		var position = Position;
		if (position > 0m)
		{
			SellMarket(position);
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}
	}

	private decimal GetOrderVolume(decimal price)
	{
		var volume = InitialVolume;

		if (UseMoneyManagement && price > 0m)
		{
			var portfolio = Portfolio;
			var balance = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
			if (balance > 0m && BalanceReference > 0m)
			{
				volume = balance / BalanceReference * InitialVolume;
			}
		}

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var rounded = step * Math.Floor(volume / step);
			if (rounded <= 0m)
			{
				rounded = step;
			}

			volume = rounded;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
		volume = maxVolume.Value;

		return volume;
	}

	private bool IsDailyClose(DateTimeOffset time)
	{
		if (DailyCloseHour < 0)
		return false;

		var limit = new TimeSpan(DailyCloseHour, DailyCloseMinute, 0);
		return time.TimeOfDay >= limit;
	}

	private bool IsFridayClose(DateTimeOffset time)
	{
		if (time.DayOfWeek != DayOfWeek.Friday)
		return false;

		var limit = new TimeSpan(FridayCloseHour, FridayCloseMinute, 0);
		return time.TimeOfDay >= limit;
	}

	private void TrimHistory(List<decimal> history)
	{
		var capacity = Math.Max(FastShift, SlowShift) + 10;
		if (history.Count <= capacity)
		return;

		history.RemoveRange(0, history.Count - capacity);
	}

	private static decimal? GetHistoryValue(List<decimal> history, int maShift, int barShift)
	{
		var index = history.Count - 1 - (maShift + barShift);
		if (index < 0 || index >= history.Count)
		return null;

		return history[index];
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		priceStep = 0.0001m;

		var decimals = security.Decimals ?? 0;
		var multiplier = decimals is 5 or 3 ? 10m : 1m;
		return priceStep * multiplier;
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan span ? span : null;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Sma => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Ema => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smma => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, PriceSource priceType)
	{
		return priceType switch
		{
			PriceSource.Open => candle.OpenPrice,
			PriceSource.High => candle.HighPrice,
			PriceSource.Low => candle.LowPrice,
			PriceSource.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PriceSource.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceSource.Weighted => (candle.HighPrice + candle.LowPrice + (candle.ClosePrice * 2m)) / 4m,
			_ => candle.ClosePrice,
		};
	}
}
