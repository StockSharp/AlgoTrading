using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "MARE5.1".
/// The system compares two simple moving averages at multiple historical offsets to detect reversals.
/// It limits trading to a configurable daytime window and attaches MetaTrader-style stops to every order.
/// </summary>
public class Mare51Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _movingAverageShift;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _timeOpenHour;
	private readonly StrategyParam<int> _timeCloseHour;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private Shift? _fastShift0;
	private Shift? _fastShift2;
	private Shift? _fastShift5;
	private Shift? _slowShift0;
	private Shift? _slowShift2;
	private Shift? _slowShift5;

	private ICandleMessage? _previousCandle;
	private decimal _pointSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="Mare51Strategy"/> class.
	/// </summary>
	public Mare51Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 7.8m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Lot size used for market entries.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 10m, 0.1m);

		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetRange(1, 200)
			.SetDisplay("Fast MA Period", "Period of the fast simple moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 55)
			.SetRange(1, 400)
			.SetDisplay("Slow MA Period", "Period of the slow simple moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 5);

		_movingAverageShift = Param(nameof(MovingAverageShift), 2)
			.SetRange(0, 20)
			.SetDisplay("MA Shift", "Forward shift applied to both moving averages.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_stopLossPoints = Param(nameof(StopLossPoints), 80m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 110m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Take-profit distance expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 250m, 10m);

		_timeOpenHour = Param(nameof(TimeOpenHour), 8)
			.SetRange(0, 23)
			.SetDisplay("Trading Start Hour", "Hour of the day when the strategy is allowed to trade.", "Timing");

		_timeCloseHour = Param(nameof(TimeCloseHour), 14)
			.SetRange(0, 23)
			.SetDisplay("Trading End Hour", "Hour of the day when trading stops.", "Timing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for moving average calculations.", "Data");

		Volume = _tradeVolume.Value;
		_pointSize = 0m;
	}

	/// <summary>
	/// Lot size used for new market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Period of the fast simple moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow simple moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to both moving averages.
	/// </summary>
	public int MovingAverageShift
	{
		get => _movingAverageShift.Value;
		set => _movingAverageShift.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Opening hour of the allowed trading window.
	/// </summary>
	public int TimeOpenHour
	{
		get => _timeOpenHour.Value;
		set => _timeOpenHour.Value = value;
	}

	/// <summary>
	/// Closing hour of the allowed trading window.
	/// </summary>
	public int TimeCloseHour
	{
		get => _timeCloseHour.Value;
		set => _timeCloseHour.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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

		_fastShift0 = null;
		_fastShift2 = null;
		_fastShift5 = null;
		_slowShift0 = null;
		_slowShift2 = null;
		_slowShift5 = null;
		_previousCandle = null;
		_pointSize = 0m;
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = _tradeVolume.Value;
		_pointSize = GetPointSize();

		_fastMa = new SimpleMovingAverage { Length = FastPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowPeriod };

		_fastShift0 = CreateShift(MovingAverageShift);
		_fastShift2 = CreateShift(MovingAverageShift + 2);
		_fastShift5 = CreateShift(MovingAverageShift + 5);
		_slowShift0 = CreateShift(MovingAverageShift);
		_slowShift2 = CreateShift(MovingAverageShift + 2);
		_slowShift5 = CreateShift(MovingAverageShift + 5);

		var stopLoss = CreateProtectionUnit(StopLossPoints, _pointSize);
		var takeProfit = CreateProtectionUnit(TakeProfitPoints, _pointSize);

		if (stopLoss != null || takeProfit != null)
		{
			// Attach MetaTrader-style stop-loss and take-profit distances to upcoming orders.
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}

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
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		// Work strictly with completed candles to stay close to the original tick-based script.
		if (candle.State != CandleStates.Finished)
		return;

		var previousCandle = _previousCandle;
		_previousCandle = candle;

		var fastCurrent = GetShiftedValue(_fastShift0, fastValue, candle);
		var fastBack2 = GetShiftedValue(_fastShift2, fastValue, candle);
		var fastBack5 = GetShiftedValue(_fastShift5, fastValue, candle);

		var slowCurrent = GetShiftedValue(_slowShift0, slowValue, candle);
		var slowBack2 = GetShiftedValue(_slowShift2, slowValue, candle);
		var slowBack5 = GetShiftedValue(_slowShift5, slowValue, candle);

		if (fastCurrent is null || fastBack2 is null || fastBack5 is null ||
			slowCurrent is null || slowBack2 is null || slowBack5 is null)
		{
			// Indicators still need additional history to cover the requested shifts.
			return;
		}

		if (previousCandle is null)
		return;

		if (!IsWithinTradingWindow(candle.CloseTime))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m)
		return; // Only one position at a time, mirroring OrdersTotal() < 1 in MetaTrader.

		var minDifference = _pointSize;
		var bearishPrevious = previousCandle.ClosePrice < previousCandle.OpenPrice;
		var bullishPrevious = previousCandle.ClosePrice > previousCandle.OpenPrice;

		var sellSignal = bearishPrevious &&
		IsDifferenceSatisfied(slowCurrent.Value, fastCurrent.Value, minDifference) &&
		IsDifferenceSatisfied(fastBack2.Value, slowBack2.Value, minDifference) &&
		IsDifferenceSatisfied(fastBack5.Value, slowBack5.Value, minDifference);

		var buySignal = bullishPrevious &&
		IsDifferenceSatisfied(fastCurrent.Value, slowCurrent.Value, minDifference) &&
		IsDifferenceSatisfied(slowBack2.Value, fastBack2.Value, minDifference) &&
		IsDifferenceSatisfied(slowBack5.Value, fastBack5.Value, minDifference);

		if (!sellSignal && !buySignal)
		return;

		var volume = Volume;
		if (volume <= 0m)
		return;

		if (buySignal)
		{
			// Fast MA now leads while the two older offsets still show a bearish regime -> buy reversal.
			BuyMarket(volume);
		}
		else if (sellSignal)
		{
			// Slow MA now leads while older offsets remain bullish -> sell reversal.
			SellMarket(volume);
		}
	}

	private decimal? GetShiftedValue(Shift? shift, decimal baseValue, ICandleMessage candle)
	{
		if (shift == null)
		return baseValue;

		var value = shift.Process(baseValue, candle.OpenTime, true);
		return value.IsFinal ? value.ToDecimal() : null;
	}

	private static Shift? CreateShift(int totalShift)
	{
	return totalShift > 0 ? new Shift { Length = totalShift } : null;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.LocalDateTime.Hour;
		var start = TimeOpenHour;
		var end = TimeCloseHour;

		if (start <= end)
		return hour >= start && hour <= end;

		// Support overnight windows when the end hour is less than the start hour.
		return hour >= start || hour <= end;
	}

	private bool IsDifferenceSatisfied(decimal left, decimal right, decimal minDifference)
	{
		var diff = left - right;
		return minDifference > 0m ? diff >= minDifference : diff > 0m;
	}

	private decimal GetPointSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		return step > 0m ? step : 0m;
	}

	private static Unit? CreateProtectionUnit(decimal points, decimal pointSize)
	{
		if (points <= 0m || pointSize <= 0m)
		return null;

		var offset = points * pointSize;
		return new Unit(offset, UnitTypes.Absolute);
	}
}
