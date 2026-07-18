using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on direction changes of a smoothed EMA.
/// </summary>
public class T3MaAlarmStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _reverseOnSignal;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _emaValues = new();
	private int _prevDirection;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public bool ReverseOnSignal { get => _reverseOnSignal.Value; set => _reverseOnSignal.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public T3MaAlarmStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 19)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "EMA length", "Indicator");

		_maShift = Param(nameof(MaShift), 1)
			.SetDisplay("MA Shift", "Bars shift for direction check", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 200m)
			.SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk")
			.SetOptimize(0m, 1000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 400m)
			.SetDisplay("Take Profit", "Take-profit distance in price", "Risk")
			.SetOptimize(0m, 1000m, 100m);

		_reverseOnSignal = Param(nameof(ReverseOnSignal), true)
			.SetDisplay("Reverse On Signal", "Close opposite position when new signal appears", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
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

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		_emaValues.Add(emaValue);
		var required = MaShift + 2;
		if (_emaValues.Count > required)
			_emaValues.RemoveAt(0);
		if (_emaValues.Count < required)
			return;

		var valueShift = _emaValues[^ (1 + MaShift)];
		var valuePrev = _emaValues[^ (2 + MaShift)];
		var direction = valueShift > valuePrev ? 1 : valueShift < valuePrev ? -1 : _prevDirection;

		if (_cooldownRemaining == 0)
		{
			if (_prevDirection == -1 && direction == 1)
			{
				if (Position < 0 && ReverseOnSignal)
					BuyMarket();

				if (Position <= 0)
				{
					_entryPrice = candle.ClosePrice;
					BuyMarket();
					_cooldownRemaining = CooldownBars;
				}
			}
			else if (_prevDirection == 1 && direction == -1)
			{
				if (Position > 0 && ReverseOnSignal)
					SellMarket();

				if (Position >= 0)
				{
					_entryPrice = candle.ClosePrice;
					SellMarket();
					_cooldownRemaining = CooldownBars;
				}
			}
		}

		if (Position != 0 && _entryPrice != 0m)
			CheckExit(candle.ClosePrice);

		_prevDirection = direction;
	}

	private void CheckExit(decimal price)
	{
		if (Position > 0)
		{
			if (StopLoss > 0m && price <= _entryPrice - StopLoss)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (TakeProfit > 0m && price >= _entryPrice + TakeProfit)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (StopLoss > 0m && price >= _entryPrice + StopLoss)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (TakeProfit > 0m && price <= _entryPrice - TakeProfit)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
