
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum ExpSslNrtrSmoothingMethod
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
	Ama,
}

public enum MarginMode
{
	FreeMargin,
	Balance,
	LossFreeMargin,
	LossBalance,
	Lot,
}

public class ExpSslNrtrTmPlusStrategy : Strategy
{

	// Store only a limited number of indicator states for signal comparisons.
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MarginMode> _marginMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _timeExitMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ExpSslNrtrSmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _historyCapacity;

	private ISslSmoother _highSmoother = null!;
	private ISslSmoother _lowSmoother = null!;

	private readonly List<int> _colorHistory = new();
	private readonly List<DateTimeOffset> _timeHistory = new();

	// Internal timers used to throttle repeated entries per direction.
	private DateTimeOffset? _nextBuyTime;
	private DateTimeOffset? _nextSellTime;
	private decimal? _entryPrice;
	private DateTimeOffset? _positionOpened;
	private decimal? _pendingEntryPrice;
	private DateTimeOffset? _pendingEntryTime;
	private int _pendingDirection;

	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	public MarginMode MarginMode
	{
		get => _marginMode.Value;
		set => _marginMode.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	public int TimeExitMinutes
	{
		get => _timeExitMinutes.Value;
		set => _timeExitMinutes.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpSslNrtrSmoothingMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}
	/// <summary>
	/// Maximum number of historical states tracked for signals.
	/// </summary>

	public int HistoryCapacity
	{
		get => _historyCapacity.Value;
		set => _historyCapacity.Value = value;
	}

	public ExpSslNrtrTmPlusStrategy()
	{
		// Initialize all tunable parameters exposed in the UI.
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
			.SetDisplay("Money Management", "Fraction of capital or direct lots", "Trading");

		_marginMode = Param(nameof(MarginMode), MarginMode.Lot)
			.SetDisplay("Margin Mode", "Mode used to convert money management into volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");

		_slippagePoints = Param(nameof(SlippagePoints), 10m)
			.SetNotNegative()
			.SetDisplay("Slippage", "Expected slippage in points (informational)", "Risk");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long Entries", "Enable long position openings", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short Entries", "Enable short position openings", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Allow Long Exits", "Allow closing long positions on signals", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Allow Short Exits", "Allow closing short positions on signals", "Trading");

		_useTimeExit = Param(nameof(UseTimeExit), true)
			.SetDisplay("Use Time Exit", "Enable position exit after a holding period", "Risk");

		_timeExitMinutes = Param(nameof(TimeExitMinutes), 1920)
			.SetNotNegative()
			.SetDisplay("Exit Minutes", "Minutes to hold a trade before a time exit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "Data");

		_smoothingMethod = Param(nameof(SmoothingMethod), ExpSslNrtrSmoothingMethod.T3)
			.SetDisplay("Smoothing Method", "Type of moving average used inside SSL", "Indicator");

		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Smoothing period", "Indicator");

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Auxiliary parameter used by adaptive averages", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Number of closed bars to look back for signals", "Indicator");
		_historyCapacity = Param(nameof(HistoryCapacity), 1024)
			.SetRange(10, 5000)
			.SetDisplay("History Capacity", "Maximum number of stored signal states", "Signals");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_highSmoother?.Reset();
		_lowSmoother?.Reset();
		_colorHistory.Clear();
		_timeHistory.Clear();
		_nextBuyTime = null;
		_nextSellTime = null;
		_entryPrice = null;
		_positionOpened = null;
		_pendingEntryPrice = null;
		_pendingEntryTime = null;
		_pendingDirection = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highSmoother = CreateSmoother();
		_lowSmoother = CreateSmoother();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			_positionOpened = null;
			_pendingEntryPrice = null;
			_pendingEntryTime = null;
			_pendingDirection = 0;
			return;
		}

		if (Position > 0m && _pendingDirection > 0 && _pendingEntryPrice.HasValue)
		{
			_entryPrice = _pendingEntryPrice;
			_positionOpened = _pendingEntryTime;
			_pendingEntryPrice = null;
			_pendingEntryTime = null;
			_pendingDirection = 0;
		}
		else if (Position < 0m && _pendingDirection < 0 && _pendingEntryPrice.HasValue)
		{
			_entryPrice = _pendingEntryPrice;
			_positionOpened = _pendingEntryTime;
			_pendingEntryPrice = null;
			_pendingEntryTime = null;
			_pendingDirection = 0;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles to avoid repainting issues.
		if (candle.State != CandleStates.Finished)
			return;

		var high = _highSmoother.Process(candle.HighPrice);
		var low = _lowSmoother.Process(candle.LowPrice);

		if (!_highSmoother.IsFormed || !_lowSmoother.IsFormed)
			return;

		var color = DetermineColor(candle.ClosePrice, high, low);
		AddHistory(candle.OpenTime, color);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryCloseByStopOrTake(candle);
		TryCloseByTime(candle);
		ProcessSignals(candle);
	}

	private void ProcessSignals(ICandleMessage candle)
	{
		// Evaluate the SSL color transitions to detect entries and exits.
		if (_colorHistory.Count <= SignalBar + 1)
			return;

		var currentIndex = _colorHistory.Count - 1 - SignalBar;
		if (currentIndex <= 0 || currentIndex >= _colorHistory.Count)
			return;

		var previousIndex = currentIndex - 1;
		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[previousIndex];
		var signalTime = _timeHistory[currentIndex];
		var timeFrame = CandleType.TimeFrame ?? TimeSpan.FromMinutes(1);

		if (currentColor == 0)
		{
			if (SellClose && Position < 0m)
				CloseShortPosition();

			if (BuyOpen && previousColor != 0)
				TryOpenLong(candle, signalTime, timeFrame);
		}
		else if (currentColor == 2)
		{
			if (BuyClose && Position > 0m)
				CloseLongPosition();

			if (SellOpen && previousColor != 2)
				TryOpenShort(candle, signalTime, timeFrame);
		}
	}

	private void TryOpenLong(ICandleMessage candle, DateTimeOffset signalTime, TimeSpan timeFrame)
	{
		// Open a long position when allowed and the throttle window has passed.
		if (Position > 0m)
			return;

		if (_nextBuyTime.HasValue && candle.CloseTime < _nextBuyTime.Value)
			return;

		var price = candle.ClosePrice;
		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_nextBuyTime = signalTime + timeFrame;
		_pendingEntryPrice = price;
		_pendingEntryTime = candle.CloseTime;
		_pendingDirection = 1;
	}

	private void TryOpenShort(ICandleMessage candle, DateTimeOffset signalTime, TimeSpan timeFrame)
	{
		// Symmetric short entry with the same gating logic as longs.
		if (Position < 0m)
			return;

		if (_nextSellTime.HasValue && candle.CloseTime < _nextSellTime.Value)
			return;

		var price = candle.ClosePrice;
		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_nextSellTime = signalTime + timeFrame;
		_pendingEntryPrice = price;
		_pendingEntryTime = candle.CloseTime;
		_pendingDirection = -1;
	}

	private void CloseLongPosition()
	{
		if (Position <= 0m)
			return;

		SellMarket(Position);
	}

	private void CloseShortPosition()
	{
		if (Position >= 0m)
			return;

		BuyMarket(Math.Abs(Position));
	}

	private void TryCloseByStopOrTake(ICandleMessage candle)
	{
		// Simulate MT5 protective orders by monitoring stop-loss and take-profit levels.
		if (Position == 0m || !_entryPrice.HasValue)
			return;

		var step = Security?.PriceStep ?? 1m;
		var entry = _entryPrice.Value;

		if (Position > 0m)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = entry - StopLossPoints * step;
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = entry + TakeProfitPoints * step;
				if (candle.HighPrice >= takePrice)
				{
					SellMarket(Position);
				}
			}
		}
		else
		{
			var volume = Math.Abs(Position);

			if (StopLossPoints > 0m)
			{
				var stopPrice = entry + StopLossPoints * step;
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(volume);
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = entry - TakeProfitPoints * step;
				if (candle.LowPrice <= takePrice)
				{
					BuyMarket(volume);
				}
			}
		}
	}

	private void TryCloseByTime(ICandleMessage candle)
	{
		// Optional time-based exit replicating the original EA timer.
		if (!UseTimeExit || Position == 0m || !_positionOpened.HasValue)
			return;

		var hold = candle.CloseTime - _positionOpened.Value;
		if (hold < TimeSpan.FromMinutes(TimeExitMinutes))
			return;

		if (Position > 0m)
			SellMarket(Position);
		else
			BuyMarket(Math.Abs(Position));
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		// Translate the money-management setting into a tradable volume.
		var mm = MoneyManagement;
		if (mm == 0m)
			return NormalizeVolume(Volume);

		if (mm < 0m)
			return NormalizeVolume(Math.Abs(mm));

		if (Portfolio == null || price <= 0m)
			return NormalizeVolume(Volume);

		var capital = Portfolio.CurrentValue ?? 0m;
		if (capital <= 0m)
			return NormalizeVolume(Volume);

		decimal volume;

		switch (MarginMode)
		{
			case MarginMode.FreeMargin:
			case MarginMode.Balance:
			{
				var amount = capital * mm;
				volume = amount / price;
				break;
			}
			case MarginMode.LossFreeMargin:
			case MarginMode.LossBalance:
			{
				var step = Security?.PriceStep ?? 1m;
				var risk = StopLossPoints * step;
				if (risk <= 0m)
					return NormalizeVolume(Volume);

				var lossAmount = capital * mm;
				volume = lossAmount / risk;
				break;
			}
			case MarginMode.Lot:
			default:
				volume = mm;
				break;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		// Align the calculated size with the instrument constraints.
		if (Security == null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private void AddHistory(DateTimeOffset time, int color)
	{
		_colorHistory.Add(color);
		_timeHistory.Add(time);

		if (_colorHistory.Count > HistoryCapacity)
		{
			_colorHistory.RemoveAt(0);
			_timeHistory.RemoveAt(0);
		}
	}

	private ISslSmoother CreateSmoother()
	{
		// Build the smoothing engine requested by the user.
		return SmoothingMethod switch
		{
			ExpSslNrtrSmoothingMethod.Sma => new IndicatorSmoother(new SimpleMovingAverage { Length = Length }),
			ExpSslNrtrSmoothingMethod.Ema => new IndicatorSmoother(new ExponentialMovingAverage { Length = Length }),
			ExpSslNrtrSmoothingMethod.Smma => new IndicatorSmoother(new SmoothedMovingAverage { Length = Length }),
			ExpSslNrtrSmoothingMethod.Lwma => new IndicatorSmoother(new WeightedMovingAverage { Length = Length }),
			ExpSslNrtrSmoothingMethod.Jjma => new IndicatorSmoother(new JurikMovingAverage { Length = Length }),
			ExpSslNrtrSmoothingMethod.Jurx => new IndicatorSmoother(new JurikMovingAverage { Length = Length }), // Fallback to Jurik MA.
			ExpSslNrtrSmoothingMethod.Parma => new IndicatorSmoother(new ExponentialMovingAverage { Length = Length }), // Approximated with EMA.
			ExpSslNrtrSmoothingMethod.T3 => new TillsonT3Smoother(Length, Phase / 100m),
			ExpSslNrtrSmoothingMethod.Vidya => new VidyaSmoother(Length, Math.Max(1, Phase)),
			ExpSslNrtrSmoothingMethod.Ama => new AmaSmoother(Length, Math.Max(1, Phase)),
			_ => new IndicatorSmoother(new ExponentialMovingAverage { Length = Length }),
		};
	}

	private static int DetermineColor(decimal close, decimal highMa, decimal lowMa)
	{
		if (close < lowMa)
			return 2;

		if (close > highMa)
			return 0;

		return 1;
	}

	private interface ISslSmoother
	{
		// Minimal abstraction used to handle several moving-average variants.
		bool IsFormed { get; }

		decimal Process(decimal value);

		void Reset();
	}

	private sealed class IndicatorSmoother : ISslSmoother
	{
		// Wrap StockSharp built-in indicators into the smoother interface.
		private readonly IIndicator _indicator;

		public IndicatorSmoother(IIndicator indicator)
		{
			_indicator = indicator;
		}

		public bool IsFormed => _indicator.IsFormed;

		public decimal Process(decimal value)
		{
			var indicatorValue = _indicator.Process(new DecimalIndicatorValue(_indicator, value));
			return indicatorValue.ToDecimal();
		}

		public void Reset()
		{
			_indicator.Reset();
		}
	}

	private sealed class TillsonT3Smoother : ISslSmoother
	{
		// Custom implementation of the Tillson T3 moving average.
		private readonly int _length;
		private readonly decimal _volumeFactor;
		private decimal? _e1;
		private decimal? _e2;
		private decimal? _e3;
		private decimal? _e4;
		private decimal? _e5;
		private decimal? _e6;
		private int _count;

		public TillsonT3Smoother(int length, decimal volumeFactor)
		{
			_length = Math.Max(1, length);
			_volumeFactor = volumeFactor;
		}

		public bool IsFormed => _count >= _length;

		public decimal Process(decimal value)
		{
			var alpha = 2m / (_length + 1m);

			_e1 = _e1.HasValue ? alpha * value + (1m - alpha) * _e1.Value : value;
			_e2 = _e2.HasValue ? alpha * _e1.Value + (1m - alpha) * _e2.Value : _e1;
			_e3 = _e3.HasValue ? alpha * _e2.Value + (1m - alpha) * _e3.Value : _e2;
			_e4 = _e4.HasValue ? alpha * _e3.Value + (1m - alpha) * _e4.Value : _e3;
			_e5 = _e5.HasValue ? alpha * _e4.Value + (1m - alpha) * _e5.Value : _e4;
			_e6 = _e6.HasValue ? alpha * _e5.Value + (1m - alpha) * _e6.Value : _e5;

			var v = _volumeFactor;
			var c1 = -v * v * v;
			var c2 = 3m * v * v + 3m * v * v * v;
			var c3 = -6m * v * v - 3m * v - 3m * v * v * v;
			var c4 = 1m + 3m * v + v * v * v + 3m * v * v;

			_count++;
			return c1 * _e6!.Value + c2 * _e5!.Value + c3 * _e4!.Value + c4 * _e3!.Value;
		}

		public void Reset()
		{
			_e1 = _e2 = _e3 = _e4 = _e5 = _e6 = null;
			_count = 0;
		}
	}

	private sealed class VidyaSmoother : ISslSmoother
	{
		// VIDYA adapts EMA smoothing based on the current CMO reading.
		private readonly int _length;
		private readonly int _cmoLength;
		private readonly Queue<decimal> _changes = new();
		private decimal _vidya;
		private decimal _previousPrice;
		private bool _initialized;
		private int _count;

		public VidyaSmoother(int length, int cmoLength)
		{
			_length = Math.Max(1, length);
			_cmoLength = Math.Max(1, cmoLength);
		}

		public bool IsFormed => _count >= Math.Max(_length, _cmoLength);

		public decimal Process(decimal value)
		{
			if (!_initialized)
			{
				_vidya = value;
				_previousPrice = value;
				_initialized = true;
				_count++;
				return _vidya;
			}

			var change = value - _previousPrice;
			_previousPrice = value;

			_changes.Enqueue(change);
			if (_changes.Count > _cmoLength)
				_changes.Dequeue();

			var upSum = 0m;
			var downSum = 0m;

			foreach (var ch in _changes)
			{
				if (ch > 0m)
					upSum += ch;
				else
					downSum -= ch;
			}

			var denominator = upSum + downSum;
			var cmo = denominator > 0m ? Math.Abs((upSum - downSum) / denominator) : 0m;
			var alpha = 2m / (_length + 1m);
			var factor = cmo * alpha;

			_vidya = factor * value + (1m - factor) * _vidya;
			_count++;
			return _vidya;
		}

		public void Reset()
		{
			_changes.Clear();
			_vidya = 0m;
			_previousPrice = 0m;
			_initialized = false;
			_count = 0;
		}
	}

	private sealed class AmaSmoother : ISslSmoother
	{
		// Kaufman adaptive moving average with configurable slow period.
		private readonly int _length;
		private readonly int _slowLength;
		private readonly int _fastLength;
		private readonly decimal _rate;
		private readonly Queue<decimal> _prices = new();
		private readonly Queue<decimal> _changes = new();
		private decimal _previousPrice;
		private decimal _ama;
		private bool _initialized;
		private int _count;

		public AmaSmoother(int length, int slowLength)
		{
			_length = Math.Max(1, length);
			_slowLength = Math.Max(1, slowLength);
			_fastLength = 2;
			_rate = 2m;
		}

		public bool IsFormed => _count >= _length;

		public decimal Process(decimal value)
		{
			if (!_initialized)
			{
				_previousPrice = value;
				_ama = value;
				_prices.Enqueue(value);
				_initialized = true;
				_count++;
				return _ama;
			}

			var change = value - _previousPrice;
			_previousPrice = value;

			_prices.Enqueue(value);
			_changes.Enqueue(Math.Abs(change));

			if (_prices.Count > _length + 1)
				_prices.Dequeue();

			if (_changes.Count > _length)
				_changes.Dequeue();

			if (_prices.Count <= _length || _changes.Count < _length)
			{
				_count++;
				return _ama;
			}

			var signal = Math.Abs(value - _prices.Peek());
			var noise = 0m;

			foreach (var absChange in _changes)
				noise += absChange;

			var er = noise > 0m ? signal / noise : 0m;
			var slow = 2m / (_slowLength + 1m);
			var fast = 2m / (_fastLength + 1m);
			var sc = er * (fast - slow) + slow;
			var smoothing = (decimal)Math.Pow((double)sc, (double)_rate);

			_ama = _ama + smoothing * (value - _ama);
			_count++;
			return _ama;
		}

		public void Reset()
		{
			_prices.Clear();
			_changes.Clear();
			_previousPrice = 0m;
			_ama = 0m;
			_initialized = false;
			_count = 0;
		}
	}
}
