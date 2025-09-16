using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JS Chaos strategy converted from the original MQL5 expert advisor.
/// </summary>
public class JSChaosStrategy : Strategy
{
	private const int FractalLookback = 10;
	private const int JawShift = 8;
	private const int TeethShift = 5;
	private const int LipsShift = 3;

	private readonly StrategyParam<bool> _useTime;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _indentPips;
	private readonly StrategyParam<decimal> _fibo1;
	private readonly StrategyParam<decimal> _fibo2;
	private readonly StrategyParam<bool> _useClosePositions;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<bool> _useBreakeven;
	private readonly StrategyParam<int> _breakevenPlusPips;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;
	private SmoothedMovingAverage _ma21;
	private AwesomeOscillator _ao;
	private SimpleMovingAverage _aoSma;
	private StandardDeviation _stdDev;

	private readonly Queue<decimal> _jawQueue = new();
	private readonly Queue<decimal> _teethQueue = new();
	private readonly Queue<decimal> _lipsQueue = new();

	private decimal? _jawValue;
	private decimal? _teethValue;
	private decimal? _lipsValue;
	private decimal? _ma21Value;

	private decimal? _aoCurrent;
	private decimal? _aoPrev;
	private decimal? _acCurrent;
	private decimal? _acPrev;
	private decimal? _stdDevCurrent;
	private decimal? _stdDevPrev;

	private readonly decimal?[] _highs = new decimal?[5];
	private readonly decimal?[] _lows = new decimal?[5];
	private int _bufferCount;

	private readonly List<FractalLevel> _upFractals = new();
	private readonly List<FractalLevel> _downFractals = new();

	private readonly List<PendingOrder> _pendingOrders = new();
	private readonly List<ActiveTrade> _activeTrades = new();

	private decimal _pipSize;
	private decimal _indentValue;
	private decimal _breakevenPlusValue;
	private bool _priceSettingsReady;

	private decimal? _prevOpen;

	/// <summary>
	/// Use time window filter.
	/// </summary>
	public bool UseTime
	{
		get => _useTime.Value;
		set => _useTime.Value = value;
	}

	/// <summary>
	/// Trading session start hour.
	/// </summary>
	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Base volume for staged entries.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fractal indentation in pips.
	/// </summary>
	public int IndentingPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Primary take-profit multiplier.
	/// </summary>
	public decimal Fibo1
	{
		get => _fibo1.Value;
		set => _fibo1.Value = value;
	}

	/// <summary>
	/// Secondary take-profit multiplier.
	/// </summary>
	public decimal Fibo2
	{
		get => _fibo2.Value;
		set => _fibo2.Value = value;
	}

	/// <summary>
	/// Close positions when lips cross the previous open.
	/// </summary>
	public bool UseClosePositions
	{
		get => _useClosePositions.Value;
		set => _useClosePositions.Value = value;
	}

	/// <summary>
	/// Enable MA-based trailing stop.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Enable breakeven management for the secondary order.
	/// </summary>
	public bool UseBreakeven
	{
		get => _useBreakeven.Value;
		set => _useBreakeven.Value = value;
	}

	/// <summary>
	/// Extra pips for breakeven stop placement.
	/// </summary>
	public int BreakevenPlusPips
	{
		get => _breakevenPlusPips.Value;
		set => _breakevenPlusPips.Value = value;
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
	/// Initialize <see cref="JSChaosStrategy"/>.
	/// </summary>
	public JSChaosStrategy()
	{
		_useTime = Param(nameof(UseTime), true)
			.SetDisplay("Use Time", "Enable trading window", "General");

		_openHour = Param(nameof(OpenHour), 7)
			.SetRange(0, 23)
			.SetDisplay("Open Hour", "Hour to start trading", "General");

		_closeHour = Param(nameof(CloseHour), 18)
			.SetRange(0, 23)
			.SetDisplay("Close Hour", "Hour to stop trading", "General");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Base volume for staged entries", "Trading");

		_indentPips = Param(nameof(IndentingPips), 0)
			.SetRange(0, 1000)
			.SetDisplay("Indenting (pips)", "Offset from fractal level", "Trading");

		_fibo1 = Param(nameof(Fibo1), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Fibo 1", "Primary take-profit multiplier", "Targets");

		_fibo2 = Param(nameof(Fibo2), 4.618m)
			.SetGreaterThanZero()
			.SetDisplay("Fibo 2", "Secondary take-profit multiplier", "Targets");

		_useClosePositions = Param(nameof(UseClosePositions), true)
			.SetDisplay("Close Positions", "Exit when lips cross previous open", "Risk");

		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable MA trailing stop", "Risk");

		_useBreakeven = Param(nameof(UseBreakeven), true)
			.SetDisplay("Use Breakeven", "Move secondary trade to breakeven", "Risk");

		_breakevenPlusPips = Param(nameof(BreakevenPlusPips), 1)
			.SetRange(0, 1000)
			.SetDisplay("Breakeven Plus", "Additional pips for breakeven", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		_jawQueue.Clear();
		_teethQueue.Clear();
		_lipsQueue.Clear();
		Array.Clear(_highs);
		Array.Clear(_lows);
		_bufferCount = 0;
		_upFractals.Clear();
		_downFractals.Clear();
		_pendingOrders.Clear();
		_activeTrades.Clear();
		_jawValue = null;
		_teethValue = null;
		_lipsValue = null;
		_ma21Value = null;
		_aoCurrent = null;
		_aoPrev = null;
		_acCurrent = null;
		_acPrev = null;
		_stdDevCurrent = null;
		_stdDevPrev = null;
		_pipSize = 0m;
		_indentValue = 0m;
		_breakevenPlusValue = 0m;
		_priceSettingsReady = false;
		_prevOpen = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePriceSettings();

		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };
		_ma21 = new SmoothedMovingAverage { Length = 21 };
		_ao = new AwesomeOscillator { ShortPeriod = 5, LongPeriod = 34 };
		_aoSma = new SimpleMovingAverage { Length = 5 };
		_stdDev = new StandardDeviation { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_priceSettingsReady)
			UpdatePriceSettings();

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		UpdateAlligator(median, candle);

		var maValue = _ma21.Process(new DecimalIndicatorValue(_ma21, candle.ClosePrice, candle.ServerTime));
		if (maValue.IsFormed)
			_ma21Value = maValue.ToDecimal();

		var aoValue = _ao.Process(new DecimalIndicatorValue(_ao, median, candle.ServerTime));
		if (!aoValue.IsFinal)
			return;

		var ao = aoValue.ToDecimal();
		var aoSmaValue = _aoSma.Process(new DecimalIndicatorValue(_aoSma, ao, candle.ServerTime));
		if (!aoSmaValue.IsFinal)
			return;

		var aoSma = aoSmaValue.ToDecimal();
		var ac = ao - aoSma;

		var stdValue = _stdDev.Process(new DecimalIndicatorValue(_stdDev, candle.ClosePrice, candle.ServerTime));
		if (!stdValue.IsFinal)
			return;

		var stdDev = stdValue.ToDecimal();

		if (_jawValue is null || _teethValue is null || _lipsValue is null || _ma21Value is null)
			return;

		UpdateHistory(ref _aoCurrent, ref _aoPrev, ao);
		UpdateHistory(ref _acCurrent, ref _acPrev, ac);
		UpdateStdDev(stdDev);
		UpdateFractals(candle);

		if (UseTrailing)
			UpdateTrailing(candle.ClosePrice);

		UpdateBreakeven(candle.ClosePrice);
		HandleStopsAndTargets(candle);
		UpdateBreakeven(candle.ClosePrice);

		if (UseClosePositions)
		{
			ApplyLipsExit();
			UpdateBreakeven(candle.ClosePrice);
		}

		var signal = GetSignal();
		var canTrade = IsTradingTime(candle.OpenTime) && IsFormedAndOnlineAndAllowTrading();

		if (canTrade)
			TryPlaceOrders(signal, candle.ClosePrice);

		TriggerPendingOrders(candle);

		if (signal == 2)
			_pendingOrders.RemoveAll(o => o.Side == Sides.Buy);
		else if (signal == 1)
			_pendingOrders.RemoveAll(o => o.Side == Sides.Sell);

		_prevOpen = candle.OpenPrice;
	}

	private void UpdateAlligator(decimal median, ICandleMessage candle)
	{
		var jawValue = _jaw.Process(new DecimalIndicatorValue(_jaw, median, candle.ServerTime));
		if (jawValue.IsFormed)
		{
			_jawQueue.Enqueue(jawValue.ToDecimal());
			if (_jawQueue.Count > JawShift)
				_jawValue = _jawQueue.Dequeue();
		}

		var teethValue = _teeth.Process(new DecimalIndicatorValue(_teeth, median, candle.ServerTime));
		if (teethValue.IsFormed)
		{
			_teethQueue.Enqueue(teethValue.ToDecimal());
			if (_teethQueue.Count > TeethShift)
				_teethValue = _teethQueue.Dequeue();
		}

		var lipsValue = _lips.Process(new DecimalIndicatorValue(_lips, median, candle.ServerTime));
		if (lipsValue.IsFormed)
		{
			_lipsQueue.Enqueue(lipsValue.ToDecimal());
			if (_lipsQueue.Count > LipsShift)
				_lipsValue = _lipsQueue.Dequeue();
		}
	}

	private void UpdateHistory(ref decimal? current, ref decimal? previous, decimal value)
	{
		previous = current;
		current = value;
	}

	private void UpdateStdDev(decimal value)
	{
		_stdDevPrev = _stdDevCurrent;
		_stdDevCurrent = value;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		for (var i = 0; i < 4; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}

		_highs[4] = candle.HighPrice;
		_lows[4] = candle.LowPrice;

		if (_bufferCount < 5)
			_bufferCount++;

		IncrementFractalAges(_upFractals);
		IncrementFractalAges(_downFractals);

		if (_bufferCount < 5)
		{
			TrimFractals(_upFractals);
			TrimFractals(_downFractals);
			return;
		}

		decimal? upFractal = null;
		decimal? downFractal = null;

		var h0 = _highs[0];
		var h1 = _highs[1];
		var h2 = _highs[2];
		var h3 = _highs[3];
		var h4 = _highs[4];

		if (h2.HasValue && h0.HasValue && h1.HasValue && h3.HasValue && h4.HasValue &&
			h2.Value > h0.Value && h2.Value > h1.Value && h2.Value > h3.Value && h2.Value > h4.Value)
			upFractal = h2.Value;

		var l0 = _lows[0];
		var l1 = _lows[1];
		var l2 = _lows[2];
		var l3 = _lows[3];
		var l4 = _lows[4];

		if (l2.HasValue && l0.HasValue && l1.HasValue && l3.HasValue && l4.HasValue &&
			l2.Value < l0.Value && l2.Value < l1.Value && l2.Value < l3.Value && l2.Value < l4.Value)
			downFractal = l2.Value;

		if (upFractal.HasValue)
		{
			var price = NormalizePrice(upFractal.Value + _indentValue);
			_upFractals.Insert(0, new FractalLevel(price));
		}

		if (downFractal.HasValue)
		{
			var price = NormalizePrice(downFractal.Value - _indentValue);
			_downFractals.Insert(0, new FractalLevel(price));
		}

		TrimFractals(_upFractals);
		TrimFractals(_downFractals);
	}

	private void IncrementFractalAges(List<FractalLevel> levels)
	{
		foreach (var level in levels)
			level.Age++;
	}

	private void TrimFractals(List<FractalLevel> levels)
	{
		for (var i = levels.Count - 1; i >= 0; i--)
		{
			if (levels[i].Age >= FractalLookback)
				levels.RemoveAt(i);
		}
	}

	private int GetSignal()
	{
		if (_aoCurrent is not decimal ao0 || _aoPrev is not decimal ao1 ||
			_lipsValue is not decimal lips || _teethValue is not decimal teeth || _jawValue is not decimal jaw)
			return 0;

		if (ao0 > ao1 && ao1 > 0m && lips > teeth && teeth > jaw)
			return 1;

		if (ao0 < ao1 && ao1 < 0m && lips < teeth && teeth < jaw)
			return 2;

		return 0;
	}

	private void TryPlaceOrders(int signal, decimal closePrice)
	{
		if (signal == 1)
		{
			var upFractal = GetLatestFractal(_upFractals);
			if (upFractal.HasValue)
				TryCreateBuyOrders(upFractal.Value, _lipsValue!.Value, closePrice);
		}
		else if (signal == 2)
		{
			var downFractal = GetLatestFractal(_downFractals);
			if (downFractal.HasValue)
				TryCreateSellOrders(downFractal.Value, _lipsValue!.Value, closePrice);
		}
	}

	private decimal? GetLatestFractal(List<FractalLevel> levels)
	{
		return levels.Count > 0 ? levels[0].Price : null;
	}

	private void TryCreateBuyOrders(decimal upFractal, decimal lips, decimal closePrice)
	{
		if (upFractal <= lips)
			return;

		if (_activeTrades.Exists(t => t.Side == Sides.Buy))
			return;

		var hasPrimary = _pendingOrders.Exists(o => o.Side == Sides.Buy && o.IsPrimary);
		var hasSecondary = _pendingOrders.Exists(o => o.Side == Sides.Buy && !o.IsPrimary);

		if (!hasPrimary)
		{
			var distance = upFractal - lips;
			if (_pipSize > 0m)
			{
				if (distance <= _pipSize)
					return;

				if (closePrice + _pipSize >= upFractal)
					return;
			}

			var tp = lips + distance * Fibo1;
			if (tp <= 0m)
				return;

			if (_pipSize > 0m && tp - upFractal <= _pipSize)
				return;

			var order = new PendingOrder
			{
				Side = Sides.Buy,
				Price = NormalizePrice(upFractal),
				StopLoss = NormalizePrice(lips),
				TakeProfit = NormalizePrice(tp),
				Volume = BaseVolume * 2m,
				IsPrimary = true
			};

			if (order.Volume > 0m)
				_pendingOrders.Add(order);
		}

		hasPrimary = _pendingOrders.Exists(o => o.Side == Sides.Buy && o.IsPrimary);
		if (!hasPrimary || hasSecondary)
			return;

		var distanceSecondary = upFractal - lips;
		if (_pipSize > 0m)
		{
			if (distanceSecondary <= _pipSize)
				return;

			if (closePrice + _pipSize >= upFractal)
				return;
		}

		var tp2 = lips + distanceSecondary * Fibo2;
		if (tp2 <= 0m)
			return;

		if (_pipSize > 0m && tp2 - upFractal <= _pipSize)
			return;

		var secondary = new PendingOrder
		{
			Side = Sides.Buy,
			Price = NormalizePrice(upFractal),
			StopLoss = NormalizePrice(lips),
			TakeProfit = NormalizePrice(tp2),
			Volume = BaseVolume,
			IsPrimary = false
		};

		if (secondary.Volume > 0m)
			_pendingOrders.Add(secondary);
	}

	private void TryCreateSellOrders(decimal downFractal, decimal lips, decimal closePrice)
	{
		if (downFractal >= lips)
			return;

		if (_activeTrades.Exists(t => t.Side == Sides.Sell))
			return;

		var hasPrimary = _pendingOrders.Exists(o => o.Side == Sides.Sell && o.IsPrimary);
		var hasSecondary = _pendingOrders.Exists(o => o.Side == Sides.Sell && !o.IsPrimary);

		if (!hasPrimary)
		{
			var distance = lips - downFractal;
			if (_pipSize > 0m)
			{
				if (distance <= _pipSize)
					return;

				if (closePrice - _pipSize <= downFractal)
					return;
			}

			var tp = lips - distance * Fibo1;
			if (tp <= 0m)
				return;

			if (_pipSize > 0m && downFractal - tp <= _pipSize)
				return;

			var order = new PendingOrder
			{
				Side = Sides.Sell,
				Price = NormalizePrice(downFractal),
				StopLoss = NormalizePrice(lips),
				TakeProfit = NormalizePrice(tp),
				Volume = BaseVolume * 2m,
				IsPrimary = true
			};

			if (order.Volume > 0m)
				_pendingOrders.Add(order);
		}

		hasPrimary = _pendingOrders.Exists(o => o.Side == Sides.Sell && o.IsPrimary);
		if (!hasPrimary || hasSecondary)
			return;

		var distanceSecondary = lips - downFractal;
		if (_pipSize > 0m)
		{
			if (distanceSecondary <= _pipSize)
				return;

			if (closePrice - _pipSize <= downFractal)
				return;
		}

		var tp2 = lips - distanceSecondary * Fibo2;
		if (tp2 <= 0m)
			return;

		if (_pipSize > 0m && downFractal - tp2 <= _pipSize)
			return;

		var secondary = new PendingOrder
		{
			Side = Sides.Sell,
			Price = NormalizePrice(downFractal),
			StopLoss = NormalizePrice(lips),
			TakeProfit = NormalizePrice(tp2),
			Volume = BaseVolume,
			IsPrimary = false
		};

		if (secondary.Volume > 0m)
			_pendingOrders.Add(secondary);
	}

	private void TriggerPendingOrders(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		for (var i = _pendingOrders.Count - 1; i >= 0; i--)
		{
			var pending = _pendingOrders[i];
			var triggered = pending.Side == Sides.Buy
				? candle.HighPrice >= pending.Price
				: candle.LowPrice <= pending.Price;

			if (!triggered)
				continue;

			ExecuteTrade(pending);
			_pendingOrders.RemoveAt(i);
		}
	}

	private void ExecuteTrade(PendingOrder order)
	{
		if (order.Volume <= 0m)
			return;

		if (order.Side == Sides.Buy)
			BuyMarket(order.Volume);
		else
			SellMarket(order.Volume);

		_activeTrades.Add(new ActiveTrade
		{
			Side = order.Side,
			Volume = order.Volume,
			EntryPrice = order.Price,
			StopLoss = order.StopLoss,
			TakeProfit = order.TakeProfit,
			IsPrimary = order.IsPrimary
		});
	}

	private void UpdateTrailing(decimal closePrice)
	{
		if (_ma21Value is not decimal ma21 ||
			_stdDevCurrent is not decimal stdDev0 || _stdDevPrev is not decimal stdDev1 ||
			_aoCurrent is not decimal ao0 || _aoPrev is not decimal ao1 ||
			_acCurrent is not decimal ac0 || _acPrev is not decimal ac1)
			return;

		foreach (var trade in _activeTrades)
		{
			if (trade.Side == Sides.Buy)
			{
				if ((trade.StopLoss <= 0m || (trade.StopLoss != ma21 && trade.StopLoss < ma21)) &&
					stdDev0 > stdDev1 && ao0 > ao1 && ac0 > ac1)
				{
					if (_pipSize <= 0m || ma21 + _pipSize <= closePrice)
						trade.StopLoss = NormalizePrice(ma21);
				}
			}
			else
			{
				if ((trade.StopLoss <= 0m || (trade.StopLoss != ma21 && trade.StopLoss > ma21)) &&
					stdDev0 > stdDev1 && ao0 < ao1 && ac0 < ac1)
				{
					if (_pipSize <= 0m || ma21 - _pipSize >= closePrice)
						trade.StopLoss = NormalizePrice(ma21);
				}
			}
		}
	}

	private void UpdateBreakeven(decimal closePrice)
	{
		if (!UseBreakeven || _breakevenPlusValue <= 0m)
			return;

		foreach (var trade in _activeTrades)
		{
			if (!trade.IsSecondary || trade.MovedToBreakeven)
				continue;

			var primaryExists = _activeTrades.Exists(t => t.Side == trade.Side && t.IsPrimary);
			if (primaryExists)
				continue;

			if (trade.Side == Sides.Buy)
			{
				if (closePrice >= trade.EntryPrice + _breakevenPlusValue && trade.StopLoss < trade.EntryPrice)
				{
					trade.StopLoss = NormalizePrice(trade.EntryPrice + _breakevenPlusValue);
					trade.MovedToBreakeven = true;
				}
			}
			else
			{
				if (closePrice <= trade.EntryPrice - _breakevenPlusValue && trade.StopLoss > trade.EntryPrice)
				{
					trade.StopLoss = NormalizePrice(trade.EntryPrice - _breakevenPlusValue);
					trade.MovedToBreakeven = true;
				}
			}
		}
	}

	private void HandleStopsAndTargets(ICandleMessage candle)
	{
		for (var i = _activeTrades.Count - 1; i >= 0; i--)
		{
			var trade = _activeTrades[i];
			var close = false;

			if (trade.Side == Sides.Buy)
			{
				if (trade.TakeProfit > 0m && candle.HighPrice >= trade.TakeProfit)
					close = true;
				else if (trade.StopLoss > 0m && candle.LowPrice <= trade.StopLoss)
					close = true;
			}
			else
			{
				if (trade.TakeProfit > 0m && candle.LowPrice <= trade.TakeProfit)
					close = true;
				else if (trade.StopLoss > 0m && candle.HighPrice >= trade.StopLoss)
					close = true;
			}

			if (!close)
				continue;

			CloseTrade(trade);
			_activeTrades.RemoveAt(i);
		}
	}

	private void ApplyLipsExit()
	{
		if (_prevOpen is null || _lipsValue is null)
			return;

		var prevOpen = _prevOpen.Value;
		var lips = _lipsValue.Value;

		if (lips > prevOpen)
			CloseTrades(Sides.Buy);

		if (lips < prevOpen)
			CloseTrades(Sides.Sell);
	}

	private void CloseTrades(Sides side)
	{
		for (var i = _activeTrades.Count - 1; i >= 0; i--)
		{
			var trade = _activeTrades[i];
			if (trade.Side != side)
				continue;

			CloseTrade(trade);
			_activeTrades.RemoveAt(i);
		}
	}

	private void CloseTrade(ActiveTrade trade)
	{
		if (trade.Side == Sides.Buy)
			SellMarket(trade.Volume);
		else
			BuyMarket(trade.Volume);
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (!UseTime)
			return true;

		var hour = time.Hour;
		var trading = false;

		if (OpenHour > CloseHour)
			trading = hour <= CloseHour || hour >= OpenHour;
		else if (OpenHour < CloseHour)
			trading = hour >= OpenHour && hour <= CloseHour;
		else
			trading = hour == OpenHour;

		var dayOfWeek = (int)time.DayOfWeek;

		if (dayOfWeek == 1 && hour < 3)
			trading = false;

		if (dayOfWeek >= 5 && hour > 18)
			trading = false;

		if (time.Month == 1 && time.Day < 10)
			trading = false;

		if (time.Month == 12 && time.Day > 20)
			trading = false;

		return trading;
	}

	private void UpdatePriceSettings()
	{
		if (Security is null)
			return;

		var step = Security.PriceStep ?? Security.MinPriceStep;
		if (step is not decimal priceStep || priceStep <= 0m)
			return;

		var decimals = CountDecimals(priceStep);
		var pip = priceStep;

		if (decimals == 3 || decimals == 5)
			pip = priceStep * 10m;

		_pipSize = pip;
		_indentValue = pip * IndentingPips;
		_breakevenPlusValue = pip * BreakevenPlusPips;
		_priceSettingsReady = true;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? Security?.MinPriceStep;
		if (step is decimal priceStep && priceStep > 0m)
			return Math.Round(price / priceStep, MidpointRounding.AwayFromZero) * priceStep;

		return price;
	}

	private static int CountDecimals(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private sealed class FractalLevel
	{
		public FractalLevel(decimal price)
		{
			Price = price;
		}

		public decimal Price { get; }
		public int Age { get; set; }
	}

	private sealed class PendingOrder
	{
		public Sides Side { get; init; }
		public decimal Price { get; init; }
		public decimal StopLoss { get; init; }
		public decimal TakeProfit { get; init; }
		public decimal Volume { get; init; }
		public bool IsPrimary { get; init; }
	}

	private sealed class ActiveTrade
	{
		public Sides Side { get; init; }
		public decimal Volume { get; init; }
		public decimal EntryPrice { get; init; }
		public decimal StopLoss { get; set; }
		public decimal TakeProfit { get; init; }
		public bool IsPrimary { get; init; }
		public bool MovedToBreakeven { get; set; }
	}
}
