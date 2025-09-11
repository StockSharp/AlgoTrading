namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy for testing external long and short signals with optional reversal and risk management.
/// </summary>
public class ExternalSignalsStrategyTesterStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _closeOnReverse;
	private readonly StrategyParam<bool> _reversePosition;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<decimal> _breakevenPerc;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLongSignal;
	private decimal _prevShortSignal;
	private bool _initialized;
	private bool _breakevenActivated;

	/// <summary>
	/// Start date to begin trading.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// End date to stop trading.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	/// <summary>
	/// Close position on opposite signal when reversal disabled.
	/// </summary>
	public bool CloseOnReverse { get => _closeOnReverse.Value; set => _closeOnReverse.Value = value; }

	/// <summary>
	/// Reverse position on opposite signal.
	/// </summary>
	public bool ReversePosition { get => _reversePosition.Value; set => _reversePosition.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPerc { get => _takeProfitPerc.Value; set => _takeProfitPerc.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }

	/// <summary>
	/// Break-even trigger percentage.
	/// </summary>
	public decimal BreakevenPerc { get => _breakevenPerc.Value; set => _breakevenPerc.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExternalSignalsStrategyTesterStrategy()
	{
		_startDate = Param(nameof(StartDate), new DateTimeOffset(2024, 11, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Begin trading from this date", "Dates");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2025, 3, 31, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Stop trading after this date", "Dates");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "Signals");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short entries", "Signals");

		_closeOnReverse = Param(nameof(CloseOnReverse), true)
			.SetDisplay("Close On Opposite", "Close on opposite signal", "Signals");

		_reversePosition = Param(nameof(ReversePosition), false)
			.SetDisplay("Reverse Position", "Reverse on opposite signal", "Signals");

		_takeProfitPerc = Param(nameof(TakeProfitPerc), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPerc = Param(nameof(StopLossPerc), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_breakevenPerc = Param(nameof(BreakevenPerc), 1m)
			.SetGreaterThanZero()
			.SetDisplay("BreakEven %", "Move stop to entry when profit reaches percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_prevLongSignal = 0m;
		_prevShortSignal = 0m;
		_initialized = false;
		_breakevenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			new Unit(TakeProfitPerc, UnitTypes.Percent),
			new Unit(StopLossPerc, UnitTypes.Percent)
		);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		var longSignal = candle.ClosePrice;
		var shortSignal = candle.ClosePrice;

		var longCondition = _initialized && _prevLongSignal <= 0m && longSignal > 0m;
		var shortCondition = _initialized && _prevShortSignal <= 0m && shortSignal > 0m;

		_prevLongSignal = longSignal;
		_prevShortSignal = shortSignal;
		_initialized = true;

		var reverseDone = false;

		if (ReversePosition && Position < 0 && longCondition && EnableLong)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			reverseDone = true;
		}

		if (ReversePosition && Position > 0 && shortCondition && EnableShort)
		{
			CancelActiveOrders();
			SellMarket(Volume + Position);
			reverseDone = true;
		}

		if (!ReversePosition && CloseOnReverse)
		{
			if (Position > 0 && shortCondition)
				SellMarket(Position);
			if (Position < 0 && longCondition)
				BuyMarket(Math.Abs(Position));
		}

		if (Position == 0 && !reverseDone)
		{
			if (EnableLong && longCondition)
				BuyMarket();
			if (EnableShort && shortCondition)
				SellMarket();
		}

		if (Position != 0)
		{
			var entryPrice = PositionPrice;
			var profitPerc = (candle.ClosePrice - entryPrice) / entryPrice * 100m * (Position < 0 ? -1m : 1m);

			if (BreakevenPerc > 0m && !_breakevenActivated && profitPerc >= BreakevenPerc)
			{
				CancelActiveOrders();

				if (Position > 0)
				{
					var tp = entryPrice * (1m + TakeProfitPerc / 100m);
					SellStop(entryPrice, Position);
					if (TakeProfitPerc > 0m)
						SellLimit(tp, Position);
				}
				else
				{
					var tp = entryPrice * (1m - TakeProfitPerc / 100m);
					BuyStop(entryPrice, Math.Abs(Position));
					if (TakeProfitPerc > 0m)
						BuyLimit(tp, Math.Abs(Position));
				}

				_breakevenActivated = true;
			}
		}
		else
		{
			_breakevenActivated = false;
		}
	}
}
