using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on direction changes of a double smoothed EMA.
/// </summary>
public class T3MaAlarmStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _reverseOnSignal;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _emaValues = new();
	private int _prevDirection;
	private decimal _entryPrice;

	/// <summary>
	/// Period of the exponential moving average.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Number of bars used to detect direction change.
	/// </summary>
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }

	/// <summary>
	/// Protective stop-loss distance.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take-profit distance.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Close opposite position on new signal.
	/// </summary>
	public bool ReverseOnSignal { get => _reverseOnSignal.Value; set => _reverseOnSignal.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T3MaAlarmStrategy"/>.
	/// </summary>
	public T3MaAlarmStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 19)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "EMA length", "Indicator");

		_maShift = Param(nameof(MaShift), 0)
			.SetDisplay("MA Shift", "Bars shift for direction check", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 300m)
			.SetDisplay("Take Profit", "Take-profit distance in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1000m, 100m);

		_reverseOnSignal = Param(nameof(ReverseOnSignal), true)
			.SetDisplay("Reverse On Signal", "Close opposite position when new signal appears", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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

		_emaValues.Clear();
		_prevDirection = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new EMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_emaValues.Add(emaValue);
		var required = MaShift + 2;
		if (_emaValues.Count > required)
			_emaValues.RemoveAt(0);
		if (_emaValues.Count < required)
			return;

		var valueShift = _emaValues[^ (1 + MaShift)];
		var valuePrev = _emaValues[^ (2 + MaShift)];
		var direction = valueShift > valuePrev ? 1 : valueShift < valuePrev ? -1 : _prevDirection;

		if (_prevDirection == -1 && direction == 1)
		{
			if (Position < 0 && ReverseOnSignal)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
			{
				_entryPrice = candle.ClosePrice;
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (_prevDirection == 1 && direction == -1)
		{
			if (Position > 0 && ReverseOnSignal)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
			{
				_entryPrice = candle.ClosePrice;
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		if (Position != 0 && _entryPrice != 0)
		{
			CheckExit(candle.ClosePrice);
		}

		_prevDirection = direction;
	}

	private void CheckExit(decimal price)
	{
		if (Position > 0)
		{
			if (StopLoss > 0m && price <= _entryPrice - StopLoss)
				SellMarket(Math.Abs(Position));
			else if (TakeProfit > 0m && price >= _entryPrice + TakeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (StopLoss > 0m && price >= _entryPrice + StopLoss)
				BuyMarket(Math.Abs(Position));
			else if (TakeProfit > 0m && price <= _entryPrice - TakeProfit)
				BuyMarket(Math.Abs(Position));
		}
	}
}
