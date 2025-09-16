using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian strategy based on the 2pb Ideal Moving Average filters.
/// Opens trades on crossings between the single and triple smoothed filters
/// and re-enters the trend when price advances by a configured number of ticks.
/// </summary>
public class TwoPbIdealMaReOpenStrategy : Strategy
{
	private readonly StrategyParam<decimal> _positionVolume;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _priceStepTicks;
	private readonly StrategyParam<int> _maxReEntries;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<int> _signalBarShift;
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<int> _periodX1;
	private readonly StrategyParam<int> _periodX2;
	private readonly StrategyParam<int> _periodY1;
	private readonly StrategyParam<int> _periodY2;
	private readonly StrategyParam<int> _periodZ1;
	private readonly StrategyParam<int> _periodZ2;
	private readonly StrategyParam<DataType> _candleType;

	private IdealMovingAverage _fastMa = null!;
	private TripleIdealMovingAverage _slowMa = null!;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private int _buyReEntries;
	private int _sellReEntries;

	/// <summary>
	/// Initializes a new instance of <see cref="TwoPbIdealMaReOpenStrategy"/>.
	/// </summary>
	public TwoPbIdealMaReOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_positionVolume = Param(nameof(PositionVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Position Volume", "Base order volume", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetNonNegative()
			.SetDisplay("Stop Loss (ticks)", "Protective stop distance in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
			.SetNonNegative()
			.SetDisplay("Take Profit (ticks)", "Protective profit distance in ticks", "Risk");

		_priceStepTicks = Param(nameof(PriceStepTicks), 300)
			.SetNonNegative()
			.SetDisplay("Re-entry Step (ticks)", "Price advance required for re-entry", "Entries");

		_maxReEntries = Param(nameof(MaxReEntries), 10)
			.SetNonNegative()
			.SetDisplay("Max Re-entries", "Maximum number of add-on trades", "Entries");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Entries");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Entries");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Close Long Positions", "Close longs on opposite signal", "Risk");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Close Short Positions", "Close shorts on opposite signal", "Risk");

		_signalBarShift = Param(nameof(SignalBarShift), 1)
			.SetNonNegative()
			.SetDisplay("Signal Shift", "Bars back used for crossover detection", "Logic");

		_period1 = Param(nameof(Period1), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Filter Period 1", "First weight of single filter", "Indicators");

		_period2 = Param(nameof(Period2), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Filter Period 2", "Second weight of single filter", "Indicators");

		_periodX1 = Param(nameof(PeriodX1), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stage X Period 1", "First weight of stage X", "Indicators");

		_periodX2 = Param(nameof(PeriodX2), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stage X Period 2", "Second weight of stage X", "Indicators");

		_periodY1 = Param(nameof(PeriodY1), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stage Y Period 1", "First weight of stage Y", "Indicators");

		_periodY2 = Param(nameof(PeriodY2), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stage Y Period 2", "Second weight of stage Y", "Indicators");

		_periodZ1 = Param(nameof(PeriodZ1), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stage Z Period 1", "First weight of stage Z", "Indicators");

		_periodZ2 = Param(nameof(PeriodZ2), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stage Z Period 2", "Second weight of stage Z", "Indicators");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order size used for entries and re-entries.
	/// </summary>
	public decimal PositionVolume
	{
		get => _positionVolume.Value;
		set => _positionVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Number of price ticks required between re-entry orders.
	/// </summary>
	public int PriceStepTicks
	{
		get => _priceStepTicks.Value;
		set => _priceStepTicks.Value = value;
	}

	/// <summary>
	/// Maximum number of additional entries allowed per direction.
	/// </summary>
	public int MaxReEntries
	{
		get => _maxReEntries.Value;
		set => _maxReEntries.Value = value;
	}

	/// <summary>
	/// Controls whether the strategy can open new long positions.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Controls whether the strategy can open new short positions.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Controls whether long trades are closed when the signal reverses.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Controls whether short trades are closed when the signal reverses.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Number of bars back that participates in crossover detection.
	/// </summary>
	public int SignalBarShift
	{
		get => _signalBarShift.Value;
		set => _signalBarShift.Value = value;
	}

	/// <summary>
	/// First weight for the single stage ideal moving average.
	/// </summary>
	public int Period1
	{
		get => _period1.Value;
		set => _period1.Value = value;
	}

	/// <summary>
	/// Second weight for the single stage ideal moving average.
	/// </summary>
	public int Period2
	{
		get => _period2.Value;
		set => _period2.Value = value;
	}

	/// <summary>
	/// First weight for the X stage of the triple filter.
	/// </summary>
	public int PeriodX1
	{
		get => _periodX1.Value;
		set => _periodX1.Value = value;
	}

	/// <summary>
	/// Second weight for the X stage of the triple filter.
	/// </summary>
	public int PeriodX2
	{
		get => _periodX2.Value;
		set => _periodX2.Value = value;
	}

	/// <summary>
	/// First weight for the Y stage of the triple filter.
	/// </summary>
	public int PeriodY1
	{
		get => _periodY1.Value;
		set => _periodY1.Value = value;
	}

	/// <summary>
	/// Second weight for the Y stage of the triple filter.
	/// </summary>
	public int PeriodY2
	{
		get => _periodY2.Value;
		set => _periodY2.Value = value;
	}

	/// <summary>
	/// First weight for the Z stage of the triple filter.
	/// </summary>
	public int PeriodZ1
	{
		get => _periodZ1.Value;
		set => _periodZ1.Value = value;
	}

	/// <summary>
	/// Second weight for the Z stage of the triple filter.
	/// </summary>
	public int PeriodZ2
	{
		get => _periodZ2.Value;
		set => _periodZ2.Value = value;
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

		_fastHistory.Clear();
		_slowHistory.Clear();
		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
		_buyReEntries = 0;
		_sellReEntries = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new IdealMovingAverage
		{
			Period1 = Period1,
			Period2 = Period2
		};

		_slowMa = new TripleIdealMovingAverage
		{
			PeriodX1 = PeriodX1,
			PeriodX2 = PeriodX2,
			PeriodY1 = PeriodY1,
			PeriodY2 = PeriodY2,
			PeriodZ1 = PeriodZ1,
			PeriodZ2 = PeriodZ2
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		Unit? stop = StopLossTicks > 0 ? new Unit(StopLossTicks * step, UnitTypes.Point) : null;
		Unit? take = TakeProfitTicks > 0 ? new Unit(TakeProfitTicks * step, UnitTypes.Point) : null;
		StartProtection(stopLoss: stop, takeProfit: take);

		if (PositionVolume > 0m)
			Volume = PositionVolume;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastHistory.Add(fast);
		_slowHistory.Add(slow);

		TrimHistory(_fastHistory);
		TrimHistory(_slowHistory);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shift = SignalBarShift;
		if (_fastHistory.Count < shift + 2 || _slowHistory.Count < shift + 2)
			return;

		var currentIndex = _fastHistory.Count - 1 - shift;
		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var fastCurrent = _fastHistory[currentIndex];
		var fastPrevious = _fastHistory[previousIndex];
		var slowCurrent = _slowHistory[currentIndex];
		var slowPrevious = _slowHistory[previousIndex];

		var bearishCross = fastPrevious > slowPrevious && fastCurrent < slowCurrent;
		var bullishCross = fastPrevious < slowPrevious && fastCurrent > slowCurrent;

		if (EnableBuyExits && bullishCross && Position > 0)
		{
			SellMarket(Position);
			ResetLongState();
		}

		if (EnableSellExits && bearishCross && Position < 0)
		{
			BuyMarket(-Position);
			ResetShortState();
		}

		var step = Security.PriceStep ?? 1m;
		var reEntryDistance = PriceStepTicks * step;

		if (PriceStepTicks > 0 && reEntryDistance > 0m)
		{
			if (Position > 0 && _buyReEntries < MaxReEntries)
			{
				var advance = candle.ClosePrice - _lastBuyPrice;
				if (advance >= reEntryDistance)
				{
					BuyMarket(PositionVolume);
					_buyReEntries++;
					_lastBuyPrice = candle.ClosePrice;
				}
			}
			else if (Position < 0 && _sellReEntries < MaxReEntries)
			{
				var advance = _lastSellPrice - candle.ClosePrice;
				if (advance >= reEntryDistance)
				{
					SellMarket(PositionVolume);
					_sellReEntries++;
					_lastSellPrice = candle.ClosePrice;
				}
			}
		}

		if (bearishCross && EnableBuyEntries && Position == 0)
		{
			BuyMarket(PositionVolume);
			_lastBuyPrice = candle.ClosePrice;
			_buyReEntries = 0;
			_sellReEntries = 0;
		}
		else if (bullishCross && EnableSellEntries && Position == 0)
		{
			SellMarket(PositionVolume);
			_lastSellPrice = candle.ClosePrice;
			_sellReEntries = 0;
			_buyReEntries = 0;
		}
	}

	private void TrimHistory(List<decimal> history)
	{
		var maxCount = Math.Max(SignalBarShift + 2, 3);
		while (history.Count > maxCount)
			history.RemoveAt(0);
	}

	private void ResetLongState()
	{
		_lastBuyPrice = 0m;
		_buyReEntries = 0;
	}

	private void ResetShortState()
	{
		_lastSellPrice = 0m;
		_sellReEntries = 0;
	}

	private sealed class IdealMovingAverage : Indicator<decimal>
	{
		public int Period1 { get; set; } = 10;
		public int Period2 { get; set; } = 10;

		private bool _initialized;
		private decimal _previousPrice;
		private decimal _previousValue;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();

			if (!_initialized)
			{
				_initialized = true;
				_previousPrice = price;
				_previousValue = price;
				IsFormed = false;
				return new DecimalIndicatorValue(this, price, input.Time);
			}

			var weight1 = 1m / Math.Max(Period1, 1);
			var weight2 = 1m / Math.Max(Period2, 1);

			var diff = price - _previousPrice;
			var diffSquaredMinusOne = diff * diff - 1m;
			var denominator = 1m + weight2 * diffSquaredMinusOne;

			decimal result;
			if (denominator == 0m)
			{
				result = price;
			}
			else
			{
				var numerator = weight1 * (price - _previousValue) + _previousValue + weight2 * _previousValue * diffSquaredMinusOne;
				result = numerator / denominator;
			}

			_previousPrice = price;
			_previousValue = result;
			IsFormed = true;

			return new DecimalIndicatorValue(this, result, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_initialized = false;
			_previousPrice = 0m;
			_previousValue = 0m;
		}
	}

	private sealed class TripleIdealMovingAverage : Indicator<decimal>
	{
		public int PeriodX1 { get; set; } = 10;
		public int PeriodX2 { get; set; } = 10;
		public int PeriodY1 { get; set; } = 10;
		public int PeriodY2 { get; set; } = 10;
		public int PeriodZ1 { get; set; } = 10;
		public int PeriodZ2 { get; set; } = 10;

		private bool _initialized;
		private decimal _previousPrice;
		private decimal _previousX;
		private decimal _previousY;
		private decimal _previousZ;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();

			if (!_initialized)
			{
				_initialized = true;
				_previousPrice = price;
				_previousX = price;
				_previousY = price;
				_previousZ = price;
				IsFormed = false;
				return new DecimalIndicatorValue(this, price, input.Time);
			}

			var weightX1 = 1m / Math.Max(PeriodX1, 1);
			var weightX2 = 1m / Math.Max(PeriodX2, 1);
			var weightY1 = 1m / Math.Max(PeriodY1, 1);
			var weightY2 = 1m / Math.Max(PeriodY2, 1);
			var weightZ1 = 1m / Math.Max(PeriodZ1, 1);
			var weightZ2 = 1m / Math.Max(PeriodZ2, 1);

			var x = Calculate(weightX1, weightX2, _previousPrice, price, _previousX);
			var y = Calculate(weightY1, weightY2, _previousX, x, _previousY);
			var z = Calculate(weightZ1, weightZ2, _previousY, y, _previousZ);

			_previousPrice = price;
			_previousX = x;
			_previousY = y;
			_previousZ = z;
			IsFormed = true;

			return new DecimalIndicatorValue(this, z, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_initialized = false;
			_previousPrice = 0m;
			_previousX = 0m;
			_previousY = 0m;
			_previousZ = 0m;
		}

		private static decimal Calculate(decimal weight1, decimal weight2, decimal previousInput, decimal currentInput, decimal previousValue)
		{
			var diff = currentInput - previousInput;
			var diffSquaredMinusOne = diff * diff - 1m;
			var denominator = 1m + weight2 * diffSquaredMinusOne;

			if (denominator == 0m)
				return currentInput;

			var numerator = weight1 * (currentInput - previousValue) + previousValue + weight2 * previousValue * diffSquaredMinusOne;
			return numerator / denominator;
		}
	}
}
