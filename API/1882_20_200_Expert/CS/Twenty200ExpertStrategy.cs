using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that compares the opening prices of two past bars.
/// Goes long if the second shifted open exceeds the first by a threshold
/// and short if the opposite condition is met. Trades only at a specified hour
/// and exits via fixed take profit, stop loss or timeout.
/// </summary>
public class Twenty200ExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _shift1;
	private readonly StrategyParam<int> _shift2;
	private readonly StrategyParam<int> _deltaLong;
	private readonly StrategyParam<int> _deltaShort;
	private readonly StrategyParam<int> _takeProfitLong;
	private readonly StrategyParam<int> _stopLossLong;
	private readonly StrategyParam<int> _takeProfitShort;
	private readonly StrategyParam<int> _stopLossShort;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _maxOpenTime;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _opens = new();
	private decimal _entryPrice;
	private DateTimeOffset _entryTime;

	/// <summary>
	/// First bar shift.
	/// </summary>
	public int Shift1 { get => _shift1.Value; set => _shift1.Value = value; }

	/// <summary>
	/// Second bar shift.
	/// </summary>
	public int Shift2 { get => _shift2.Value; set => _shift2.Value = value; }

	/// <summary>
	/// Difference for long entry in points.
	/// </summary>
	public int DeltaLong { get => _deltaLong.Value; set => _deltaLong.Value = value; }

	/// <summary>
	/// Difference for short entry in points.
	/// </summary>
	public int DeltaShort { get => _deltaShort.Value; set => _deltaShort.Value = value; }

	/// <summary>
	/// Take profit for long positions in points.
	/// </summary>
	public int TakeProfitLong { get => _takeProfitLong.Value; set => _takeProfitLong.Value = value; }

	/// <summary>
	/// Stop loss for long positions in points.
	/// </summary>
	public int StopLossLong { get => _stopLossLong.Value; set => _stopLossLong.Value = value; }

	/// <summary>
	/// Take profit for short positions in points.
	/// </summary>
	public int TakeProfitShort { get => _takeProfitShort.Value; set => _takeProfitShort.Value = value; }

	/// <summary>
	/// Stop loss for short positions in points.
	/// </summary>
	public int StopLossShort { get => _stopLossShort.Value; set => _stopLossShort.Value = value; }

	/// <summary>
	/// Hour when new positions can be opened.
	/// </summary>
	public int TradeHour { get => _tradeHour.Value; set => _tradeHour.Value = value; }

	/// <summary>
	/// Maximum time to hold a position in hours.
	/// </summary>
	public int MaxOpenTime { get => _maxOpenTime.Value; set => _maxOpenTime.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal OrderVolume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Twenty200ExpertStrategy"/>.
	/// </summary>
	public Twenty200ExpertStrategy()
	{
		_shift1 = Param(nameof(Shift1), 6)
			.SetGreaterThanZero()
			.SetDisplay("Shift 1", "Shift of first bar", "Signals")
			.SetCanOptimize(true);

		_shift2 = Param(nameof(Shift2), 2)
			.SetGreaterThanZero()
			.SetDisplay("Shift 2", "Shift of second bar", "Signals")
			.SetCanOptimize(true);

		_deltaLong = Param(nameof(DeltaLong), 6)
			.SetGreaterThanZero()
			.SetDisplay("Delta Long", "Price difference for long in points", "Signals")
			.SetCanOptimize(true);

		_deltaShort = Param(nameof(DeltaShort), 21)
			.SetGreaterThanZero()
			.SetDisplay("Delta Short", "Price difference for short in points", "Signals")
			.SetCanOptimize(true);

		_takeProfitLong = Param(nameof(TakeProfitLong), 390)
			.SetDisplay("Take Profit Long", "Take profit for long in points", "Risk")
			.SetCanOptimize(true);

		_stopLossLong = Param(nameof(StopLossLong), 1470)
			.SetDisplay("Stop Loss Long", "Stop loss for long in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitShort = Param(nameof(TakeProfitShort), 320)
			.SetDisplay("Take Profit Short", "Take profit for short in points", "Risk")
			.SetCanOptimize(true);

		_stopLossShort = Param(nameof(StopLossShort), 2670)
			.SetDisplay("Stop Loss Short", "Stop loss for short in points", "Risk")
			.SetCanOptimize(true);

		_tradeHour = Param(nameof(TradeHour), 14)
			.SetRange(0, 23)
			.SetDisplay("Trade Hour", "Hour to open positions", "Signals")
			.SetCanOptimize(true);

		_maxOpenTime = Param(nameof(MaxOpenTime), 504)
			.SetGreaterThanZero()
			.SetDisplay("Max Open Time", "Maximum position time in hours", "Risk")
			.SetCanOptimize(true);

		_volume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_opens.Clear();
		_entryPrice = 0m;
		_entryTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		_opens.Enqueue(candle.OpenPrice);
		var maxShift = Math.Max(Shift1, Shift2) + 1;
		if (_opens.Count > maxShift)
			_opens.Dequeue();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			var tp = _entryPrice + TakeProfitLong * priceStep;
			var sl = _entryPrice - StopLossLong * priceStep;
			var timedOut = MaxOpenTime > 0 && (candle.OpenTime - _entryTime).TotalHours >= MaxOpenTime;
			if (candle.HighPrice >= tp || candle.LowPrice <= sl || timedOut)
			{
				SellMarket(Position);
				_entryPrice = 0m;
				_entryTime = default;
			}
		}
		else if (Position < 0)
		{
			var tp = _entryPrice - TakeProfitShort * priceStep;
			var sl = _entryPrice + StopLossShort * priceStep;
			var timedOut = MaxOpenTime > 0 && (candle.OpenTime - _entryTime).TotalHours >= MaxOpenTime;
			if (candle.LowPrice <= tp || candle.HighPrice >= sl || timedOut)
			{
				BuyMarket(-Position);
				_entryPrice = 0m;
				_entryTime = default;
			}
		}

		if (_opens.Count < maxShift)
			return;

		if (Position != 0)
			return;

		var arr = _opens.ToArray();
		var openShift1 = arr[^1 - Shift1];
		var openShift2 = arr[^1 - Shift2];

		var diffLong = openShift2 - openShift1;
		var diffShort = openShift1 - openShift2;
		var thLong = DeltaLong * priceStep;
		var thShort = DeltaShort * priceStep;

		if (candle.OpenTime.Hour != TradeHour)
			return;

		if (diffLong > thLong && diffShort <= thShort)
		{
			BuyMarket(OrderVolume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
		}
		else if (diffShort > thShort && diffLong <= thLong)
		{
			SellMarket(OrderVolume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
		}
	}
}
