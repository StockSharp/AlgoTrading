using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the Total Power Indicator expert advisor.
/// </summary>
public class TotalPowerIndicatorXStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _powerPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private TotalPowerIndicator _totalPower;
	private decimal? _previousDifference;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for EMA inside Total Power Indicator.
	/// </summary>
	public int PowerPeriod
	{
		get => _powerPeriod.Value;
		set => _powerPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period used for bull and bear strength counters.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions on opposite signals.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions on opposite signals.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Enable time filter for trading sessions.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Session end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TotalPowerIndicatorXStrategy"/>.
	/// </summary>
	public TotalPowerIndicatorXStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_powerPeriod = Param(nameof(PowerPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Power Period", "EMA length used by Total Power", "Indicator")
		.SetCanOptimize(true);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 45)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Samples counted for bull/bear strength", "Indicator")
		.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
		.SetDisplay("Enable Long Entry", "Allow buying when bulls dominate", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
		.SetDisplay("Enable Short Entry", "Allow selling when bears dominate", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
		.SetDisplay("Enable Long Exit", "Close longs on bearish crossover", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
		.SetDisplay("Enable Short Exit", "Close shorts on bullish crossover", "Trading");

		_useTradingHours = Param(nameof(UseTradingHours), true)
		.SetDisplay("Use Trading Hours", "Restrict trading to session window", "Schedule");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Session start hour", "Schedule");

		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Session start minute", "Schedule");

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Session end hour", "Schedule");

		_endMinute = Param(nameof(EndMinute), 59)
		.SetDisplay("End Minute", "Session end minute", "Schedule");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk");
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

		_totalPower?.Reset();
		_previousDifference = null;
		ResetLongTargets();
		ResetShortTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_totalPower = new TotalPowerIndicator
		{
			PowerPeriod = PowerPeriod,
			LookbackPeriod = LookbackPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_totalPower, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _totalPower);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (indicatorValue is not TotalPowerIndicatorValue powerValue)
		return;

		if (!_totalPower.IsFormed)
		{
			_previousDifference = powerValue.Bulls - powerValue.Bears;
			return;
		}

		var difference = powerValue.Bulls - powerValue.Bears;
		var previous = _previousDifference ?? difference;
		_previousDifference = difference;

		if (HandleStops(candle))
		return;

		var crossUp = difference > 0m && previous <= 0m;
		var crossDown = difference < 0m && previous >= 0m;

		var isTradingTime = !UseTradingHours || IsWithinTradingWindow(candle.OpenTime);

		if (UseTradingHours && !isTradingTime)
		{
			CloseAllPositions();
			return;
		}

		if (EnableLongExit && crossDown && Position > 0m)
		{
			SellMarket(Position);
			ResetLongTargets();
		}

		if (EnableShortExit && crossUp && Position < 0m)
		{
			BuyMarket(-Position);
			ResetShortTargets();
		}

		if (!isTradingTime)
		return;

		if (EnableLongEntry && crossUp && Position == 0m)
		{
			BuyMarket(Volume);
			SetupLongTargets(candle.ClosePrice);
		}
		else if (EnableShortEntry && crossDown && Position == 0m)
		{
			SellMarket(Volume);
			SetupShortTargets(candle.ClosePrice);
		}
	}

	private bool HandleStops(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return false;

		if (Position > 0m)
		{
			var volume = Position;

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(volume);
				ResetLongTargets();
				return true;
			}

			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(volume);
				ResetLongTargets();
				return true;
			}
		}
		else if (Position < 0m)
		{
			var volume = -Position;

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				ResetShortTargets();
				return true;
			}

			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(volume);
				ResetShortTargets();
				return true;
			}
		}

		return false;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var current = time.TimeOfDay;

		if (start == end)
		return current >= start && current < end;

		if (start < end)
		return current >= start && current < end;

		return current >= start || current < end;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			ResetLongTargets();
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
			ResetShortTargets();
		}
	}

	private void SetupLongTargets(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			ResetLongTargets();
			return;
		}

		_longStopPrice = StopLossPoints > 0 ? entryPrice - StopLossPoints * step : null;
		_longTakePrice = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * step : null;
		ResetShortTargets();
	}

	private void SetupShortTargets(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			ResetShortTargets();
			return;
		}

		_shortStopPrice = StopLossPoints > 0 ? entryPrice + StopLossPoints * step : null;
		_shortTakePrice = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * step : null;
		ResetLongTargets();
	}

	private void ResetLongTargets()
	{
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortTargets()
	{
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private sealed class TotalPowerIndicator : Indicator<ICandleMessage>
	{
		private readonly Queue<int> _bullHistory = new();
		private readonly Queue<int> _bearHistory = new();
		private readonly EMA _ema = new();
		private int _bullCount;
		private int _bearCount;
		private int _powerPeriod = 10;
		private int _lookbackPeriod = 45;

		public int PowerPeriod
		{
			get => _powerPeriod;
			set
			{
				_powerPeriod = Math.Max(1, value);
				_ema.Length = _powerPeriod;
			}
		}

		public int LookbackPeriod
		{
			get => _lookbackPeriod;
			set => _lookbackPeriod = Math.Max(1, value);
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var emaValue = _ema.Process(new DecimalIndicatorValue(_ema, candle.ClosePrice, input.Time));

			if (!emaValue.IsFinal)
			{
				IsFormed = false;
				return new TotalPowerIndicatorValue(this, input, 0m, 0m, 0m);
			}

			var ema = emaValue.ToDecimal();
			var bullContribution = candle.HighPrice > ema ? 1 : 0;
			var bearContribution = candle.LowPrice < ema ? 1 : 0;

			UpdateCounters(_bullHistory, ref _bullCount, bullContribution);
			UpdateCounters(_bearHistory, ref _bearCount, bearContribution);

			if (_bullHistory.Count < LookbackPeriod || _bearHistory.Count < LookbackPeriod)
			{
				IsFormed = false;
				return new TotalPowerIndicatorValue(this, input, 0m, 0m, 0m);
			}

			var bullPercent = (decimal)_bullCount * 100m / LookbackPeriod;
			var bearPercent = (decimal)_bearCount * 100m / LookbackPeriod;

			var bulls = Math.Clamp((bullPercent - 50m) * 2m, 0m, 100m);
			var bears = Math.Clamp((bearPercent - 50m) * 2m, 0m, 100m);
			var power = Math.Clamp(2m * Math.Abs(bullPercent - bearPercent), 0m, 100m);

			IsFormed = true;
			return new TotalPowerIndicatorValue(this, input, bulls, bears, power);
		}

		public override void Reset()
		{
			base.Reset();

			_bullHistory.Clear();
			_bearHistory.Clear();
			_bullCount = 0;
			_bearCount = 0;
			_ema.Reset();
		}

		private void UpdateCounters(Queue<int> queue, ref int count, int value)
		{
			queue.Enqueue(value);
			count += value;

			while (queue.Count > LookbackPeriod)
			{
				count -= queue.Dequeue();
			}
		}
	}

	private sealed class TotalPowerIndicatorValue : ComplexIndicatorValue
	{
		public TotalPowerIndicatorValue(IIndicator indicator, IIndicatorValue input, decimal bulls, decimal bears, decimal power)
		: base(indicator, input, (nameof(Bulls), bulls), (nameof(Bears), bears), (nameof(Power), power))
		{
		}

		public decimal Bulls => (decimal)GetValue(nameof(Bulls));

		public decimal Bears => (decimal)GetValue(nameof(Bears));

		public decimal Power => (decimal)GetValue(nameof(Power));
	}
}
