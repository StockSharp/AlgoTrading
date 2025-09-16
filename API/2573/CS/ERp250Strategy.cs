using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reverse point breakout strategy converted from the e_RP_250 MQL script.
/// </summary>
public class ERp250Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _reversePoint;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _latestHighSignal;
	private decimal _latestLowSignal;
	private decimal _lastExecutedHigh;
	private decimal _lastExecutedLow;
	private DateTimeOffset? _lastSignalTime;
	private decimal? _bestLongPrice;
	private decimal? _bestShortPrice;
	private decimal _trailingDistance;

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public int ReversePoint
	{
		get => _reversePoint.Value;
		set => _reversePoint.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ERp250Strategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 15m)
			.SetDisplay("Take Profit Points", "Take profit distance in price points", "Risk")
			.SetCanOptimize();

		_stopLossPoints = Param(nameof(StopLossPoints), 999m)
			.SetDisplay("Stop Loss Points", "Stop loss distance in price points", "Risk")
			.SetCanOptimize();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetDisplay("Trailing Stop Points", "Trailing stop distance in price points", "Risk")
			.SetCanOptimize();

		_reversePoint = Param(nameof(ReversePoint), 250)
			.SetDisplay("Reverse Point Length", "Candles used to confirm reversal points", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyse", "General");
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

		_highest = null;
		_lowest = null;
		_latestHighSignal = 0m;
		_latestLowSignal = 0m;
		_lastExecutedHigh = 0m;
		_lastExecutedLow = 0m;
		_lastSignalTime = null;
		_bestLongPrice = null;
		_bestShortPrice = null;
		_trailingDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = ReversePoint };
		_lowest = new Lowest { Length = ReversePoint };

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var takeDistance = TakeProfitPoints > 0m ? step * TakeProfitPoints : 0m;
		var stopDistance = StopLossPoints > 0m ? step * StopLossPoints : 0m;
		_trailingDistance = TrailingStopPoints > 0m ? step * TrailingStopPoints : 0m;

		// Enable protective orders that match the original stop and take-profit distances.
		StartProtection(
			takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : default,
			stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : default
		);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var highValue = _highest.Process(candle.HighPrice, candle.OpenTime, true).ToNullableDecimal();
		var lowValue = _lowest.Process(candle.LowPrice, candle.OpenTime, true).ToNullableDecimal();

		if (highValue is null || lowValue is null)
			return;

		// Update the latest reversal levels detected by the rolling highest/lowest indicators.
		if (highValue.Value == candle.HighPrice)
			_latestHighSignal = candle.HighPrice;

		if (lowValue.Value == candle.LowPrice)
			_latestLowSignal = candle.LowPrice;

		// Manage an existing long position by trailing profits and reacting to opposite signals.
		if (Position > 0)
		{
			_bestLongPrice = (_bestLongPrice is null || candle.HighPrice > _bestLongPrice) ? candle.HighPrice : _bestLongPrice;

			if (_trailingDistance > 0m && _bestLongPrice is decimal bestLong && bestLong - candle.ClosePrice >= _trailingDistance && IsFormedAndOnlineAndAllowTrading())
			{
				SellMarket(Position);
				_bestLongPrice = null;
				return;
			}

			if (_latestHighSignal != 0m && _latestHighSignal != _lastExecutedHigh && IsFormedAndOnlineAndAllowTrading())
			{
				SellMarket(Position);
				_bestLongPrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			_bestShortPrice = (_bestShortPrice is null || candle.LowPrice < _bestShortPrice) ? candle.LowPrice : _bestShortPrice;

			if (_trailingDistance > 0m && _bestShortPrice is decimal bestShort && candle.ClosePrice - bestShort >= _trailingDistance && IsFormedAndOnlineAndAllowTrading())
			{
				BuyMarket(-Position);
				_bestShortPrice = null;
				return;
			}

			if (_latestLowSignal != 0m && _latestLowSignal != _lastExecutedLow && IsFormedAndOnlineAndAllowTrading())
			{
				BuyMarket(-Position);
				_bestShortPrice = null;
				return;
			}
		}
		else
		{
			_bestLongPrice = null;
			_bestShortPrice = null;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		// Avoid placing more than one order within the same candle.
		if (_lastSignalTime == candle.OpenTime)
			return;

		// Execute a new short position when a fresh reversal high is detected.
		if (_latestHighSignal != 0m && _latestHighSignal != _lastExecutedHigh)
		{
			SellMarket();
			_lastExecutedHigh = _latestHighSignal;
			_lastSignalTime = candle.OpenTime;
			_bestShortPrice = candle.ClosePrice;
			_bestLongPrice = null;
			return;
		}

		// Execute a new long position when a fresh reversal low is detected.
		if (_latestLowSignal != 0m && _latestLowSignal != _lastExecutedLow)
		{
			BuyMarket();
			_lastExecutedLow = _latestLowSignal;
			_lastSignalTime = candle.OpenTime;
			_bestLongPrice = candle.ClosePrice;
			_bestShortPrice = null;
		}
	}
}
