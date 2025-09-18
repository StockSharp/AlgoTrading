using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual moving averages calculated on top of RSI values.
/// </summary>
public class RsiMaOnRsiDualStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastRsiPeriod;
	private readonly StrategyParam<int> _slowRsiPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<decimal> _neutralLevel;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;

	private RelativeStrengthIndex? _fastRsi;
	private RelativeStrengthIndex? _slowRsi;
	private SimpleMovingAverage? _fastMa;
	private SimpleMovingAverage? _slowMa;

	private decimal? _previousFastMa;
	private decimal? _previousSlowMa;

	private bool _pendingBuy;
	private bool _pendingSell;
	private bool _orderInFlight;

	private DateTimeOffset? _lastLongSignalTime;
	private DateTimeOffset? _lastShortSignalTime;

	public RsiMaOnRsiDualStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candles processed by the strategy.", "General");

		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI period", "Length of the fast RSI smoothing window.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI period", "Length of the slow RSI smoothing window.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_maPeriod = Param(nameof(MaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Number of RSI values averaged by the smoothing moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Close)
			.SetDisplay("Applied price", "Price source used to build RSI values.", "Indicators");

		_neutralLevel = Param(nameof(NeutralLevel), 50m)
			.SetDisplay("Neutral level", "Neutral RSI level used to filter entries.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40m, 60m, 1m);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow long", "If disabled, long entries are ignored.", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow short", "If disabled, short entries are ignored.", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse signals", "If enabled, swap long and short signals.", "Trading");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close opposite", "Close the opposite position before opening a new one.", "Trading");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only one position", "Restrict the strategy to a single open position.", "Trading");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use time filter", "Restrict entries to a specified intraday interval.", "Timing");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(10, 0, 0))
			.SetDisplay("Session start", "Start time of the trading window (exchange timezone).", "Timing");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
			.SetDisplay("Session end", "End time of the trading window (exchange timezone).", "Timing");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastRsiPeriod
	{
		get => _fastRsiPeriod.Value;
		set => _fastRsiPeriod.Value = value;
	}

	public int SlowRsiPeriod
	{
		get => _slowRsiPeriod.Value;
		set => _slowRsiPeriod.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public decimal NeutralLevel
	{
		get => _neutralLevel.Value;
		set => _neutralLevel.Value = value;
	}

	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_fastRsi = null;
		_slowRsi = null;
		_fastMa = null;
		_slowMa = null;
		_previousFastMa = null;
		_previousSlowMa = null;
		_pendingBuy = false;
		_pendingSell = false;
		_orderInFlight = false;
		_lastLongSignalTime = null;
		_lastShortSignalTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastRsi = new RelativeStrengthIndex { Length = FastRsiPeriod };
		_slowRsi = new RelativeStrengthIndex { Length = SlowRsiPeriod };
		_fastMa = new SimpleMovingAverage { Length = MaPeriod };
		_slowMa = new SimpleMovingAverage { Length = MaPeriod };

		_previousFastMa = null;
		_previousSlowMa = null;

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			if (_fastMa != null)
				DrawIndicator(priceArea, _fastMa);
			if (_slowMa != null)
				DrawIndicator(priceArea, _slowMa);
		}

		if (_fastRsi != null && _slowRsi != null)
		{
			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, _fastRsi);
				DrawIndicator(rsiArea, _slowRsi);
			}
		}
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		_orderInFlight = false;

		if (_pendingBuy || _pendingSell)
			ProcessPendingOrders();
	}

	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		_orderInFlight = false;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinSession(candle.OpenTime))
			return;

		if (_fastRsi == null || _slowRsi == null || _fastMa == null || _slowMa == null)
			return;

		var price = GetAppliedPrice(candle, AppliedPrice);
		var time = candle.OpenTime;

		var fastRsiValue = _fastRsi.Process(price, time, true).ToDecimal();
		var slowRsiValue = _slowRsi.Process(price, time, true).ToDecimal();

		if (!_fastRsi.IsFormed || !_slowRsi.IsFormed)
			return;

		var fastMaValue = _fastMa.Process(fastRsiValue, time, true).ToDecimal();
		var slowMaValue = _slowMa.Process(slowRsiValue, time, true).ToDecimal();

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		var buySignal = false;
		var sellSignal = false;

		if (_previousFastMa.HasValue && _previousSlowMa.HasValue)
		{
			var prevFast = _previousFastMa.Value;
			var prevSlow = _previousSlowMa.Value;
			var neutral = NeutralLevel;

			// Detect bullish crossover below the neutral RSI level.
			buySignal = prevFast < prevSlow && fastMaValue > slowMaValue && fastMaValue < neutral && slowMaValue < neutral;

			// Detect bearish crossover above the neutral RSI level.
			sellSignal = prevFast > prevSlow && fastMaValue < slowMaValue && fastMaValue > neutral && slowMaValue > neutral;
		}

		_previousFastMa = fastMaValue;
		_previousSlowMa = slowMaValue;

		if (ReverseSignals)
		{
			var tmp = buySignal;
			buySignal = sellSignal;
			sellSignal = tmp;
		}

		if (buySignal && AllowLong && _lastLongSignalTime != time)
		{
			_pendingBuy = true;
			_pendingSell = false;
			_lastLongSignalTime = time;
		}
		else if (sellSignal && AllowShort && _lastShortSignalTime != time)
		{
			_pendingSell = true;
			_pendingBuy = false;
			_lastShortSignalTime = time;
		}

		ProcessPendingOrders();
	}

	private void ProcessPendingOrders()
	{
		if (!_pendingBuy && !_pendingSell)
			return;

		if (_orderInFlight)
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		if (_pendingBuy)
		{
			if (!AllowLong)
			{
				_pendingBuy = false;
			}
			else if (_closeOpposite && Position < 0m)
			{
				ClosePosition();
				_orderInFlight = true;
				return;
			}
			else if (_onlyOnePosition && Position != 0m)
			{
				_pendingBuy = false;
			}
			else if (Position <= 0m)
			{
				if (Position == 0m)
				{
					BuyMarket(volume);
					_orderInFlight = true;
				}
				_pendingBuy = false;
			}
			else
			{
				_pendingBuy = false;
			}
		}

		if (_pendingSell)
		{
			if (!AllowShort)
			{
				_pendingSell = false;
			}
			else if (_closeOpposite && Position > 0m)
			{
				ClosePosition();
				_orderInFlight = true;
				return;
			}
			else if (_onlyOnePosition && Position != 0m)
			{
				_pendingSell = false;
			}
			else if (Position >= 0m)
			{
				if (Position == 0m)
				{
					SellMarket(volume);
					_orderInFlight = true;
				}
				_pendingSell = false;
			}
			else
			{
				_pendingSell = false;
			}
		}
	}

	private bool IsWithinSession(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var start = SessionStart;
		var end = SessionEnd;
		var t = time.TimeOfDay;

		if (start == end)
			return true;

		if (start < end)
			return t >= start && t < end;

		return t >= start || t < end;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			AppliedPriceType.Average => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Average,
	}
}
