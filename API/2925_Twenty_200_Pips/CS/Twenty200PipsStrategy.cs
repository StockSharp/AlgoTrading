using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy derived from the "20/200 pips" MQL5 expert.
/// </summary>
public class Twenty200PipsStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _firstOffset;
	private readonly StrategyParam<int> _secondOffset;
	private readonly StrategyParam<int> _deltaPoints;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private Shift _shiftFirst;
	private Shift _shiftSecond;
	private decimal _pointValue;
	private bool _canTrade = true;

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Hour of the day (0-23) when the strategy evaluates entries.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	/// <summary>
	/// Index of the older open price used for the comparison.
	/// </summary>
	public int FirstOffset
	{
		get => _firstOffset.Value;
		set => _firstOffset.Value = value;
	}

	/// <summary>
	/// Index of the newer open price used for the comparison.
	/// </summary>
	public int SecondOffset
	{
		get => _secondOffset.Value;
		set => _secondOffset.Value = value;
	}

	/// <summary>
	/// Minimum distance in points required between the two open prices.
	/// </summary>
	public int DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	/// <summary>
	/// Order volume submitted with market orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used for the hourly comparison.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Twenty200PipsStrategy"/>.
	/// </summary>
	public Twenty200PipsStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 200)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Take profit distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_stopLoss = Param(nameof(StopLoss), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Stop loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(200, 4000, 100);

		_tradeHour = Param(nameof(TradeHour), 18)
			.SetDisplay("Trade Hour", "Hour to evaluate entries", "Timing")
			.SetRange(0, 23);

		_firstOffset = Param(nameof(FirstOffset), 7)
			.SetGreaterThanZero()
			.SetDisplay("First Offset", "Older bar index (Open[t1])", "Signal")
			.SetCanOptimize(true)
			.SetOptimize(1, 12, 1);

		_secondOffset = Param(nameof(SecondOffset), 2)
			.SetGreaterThanZero()
			.SetDisplay("Second Offset", "Newer bar index (Open[t2])", "Signal")
			.SetCanOptimize(true)
			.SetOptimize(1, 6, 1);

		_deltaPoints = Param(nameof(DeltaPoints), 70)
			.SetGreaterThanZero()
			.SetDisplay("Delta (points)", "Minimum difference between opens", "Signal")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for the hourly comparison", "General");
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

		_shiftFirst = null;
		_shiftSecond = null;
		_pointValue = 0m;
		_canTrade = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		_shiftFirst = new Shift { Length = FirstOffset };
		_shiftSecond = new Shift { Length = SecondOffset };

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

		StartProtection(
			takeProfit: new Unit(TakeProfit * _pointValue, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * _pointValue, UnitTypes.Absolute),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var isFinal = candle.State == CandleStates.Finished;

		// Feed open prices into shift indicators to access historical opens.
		var firstValue = _shiftFirst.Process(candle.OpenPrice, candle.OpenTime, isFinal);
		var secondValue = _shiftSecond.Process(candle.OpenPrice, candle.OpenTime, isFinal);

		// Synchronize indicator lengths with parameter values if the user changes them live.
		if (_shiftFirst.Length != FirstOffset)
			_shiftFirst.Length = FirstOffset;
		if (_shiftSecond.Length != SecondOffset)
			_shiftSecond.Length = SecondOffset;

		var hour = candle.OpenTime.Hour;
		if (hour > TradeHour)
		{
			// Allow a new trade after the configured hour has passed for the day.
			_canTrade = true;
		}

		if (!isFinal)
			return;

		if (!_shiftFirst.IsFormed || !_shiftSecond.IsFormed)
			return;

		if (hour != TradeHour || !_canTrade || Position != 0)
			return;

		var openFirst = firstValue.ToDecimal();
		var openSecond = secondValue.ToDecimal();
		var threshold = DeltaPoints * _pointValue;

		if (openFirst > openSecond + threshold)
		{
			// Current open is higher than the reference by delta: open a short position.
			SellMarket(Volume);
			_canTrade = false;
		}
		else if (openFirst + threshold < openSecond)
		{
			// Current open is lower than the reference by delta: open a long position.
			BuyMarket(Volume);
			_canTrade = false;
		}
	}
}
