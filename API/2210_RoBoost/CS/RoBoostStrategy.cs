using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based strategy converted from the original RoBoostj MQL4 robot.
/// Opens long or short positions depending on price momentum and RSI values.
/// Includes optional trailing stop management.
/// </summary>
public class RoBoostStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiUp;
	private readonly StrategyParam<int> _rsiDown;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;
	private decimal _trailingStopPrice;
	private decimal _previousClose;
	private bool _isFirst = true;

	/// <summary>
	/// Take profit distance from entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance from entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// RSI indicator period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold for long entries.
	/// </summary>
	public int RsiUp
	{
		get => _rsiUp.Value;
		set => _rsiUp.Value = value;
	}

	/// <summary>
	/// RSI threshold for short entries.
	/// </summary>
	public int RsiDown
	{
		get => _rsiDown.Value;
		set => _rsiDown.Value = value;
	}

	/// <summary>
	/// Enables trailing stop logic.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Distance at which trailing stop becomes active.
	/// </summary>
	public decimal TrailStart
	{
		get => _trailStart.Value;
		set => _trailStart.Value = value;
	}

	/// <summary>
	/// Distance maintained from current price when trailing stop is active.
	/// </summary>
	public decimal TrailStep
	{
		get => _trailStep.Value;
		set => _trailStep.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RoBoostStrategy"/>.
	/// </summary>
	public RoBoostStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_rsiUp = Param(nameof(RsiUp), 50)
			.SetDisplay("RSI Up", "RSI threshold for longs", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(45, 70, 5);

		_rsiDown = Param(nameof(RsiDown), 50)
			.SetDisplay("RSI Down", "RSI threshold for shorts", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(30, 55, 5);

		_useTrailing = Param(nameof(UseTrailing), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk Management");

		_trailStart = Param(nameof(TrailStart), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Start", "Profit distance to activate trailing", "Risk Management");

		_trailStep = Param(nameof(TrailStep), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Step", "Distance between price and trailing stop", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");
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

		_entryPrice = 0m;
		_isLong = false;
		_trailingStopPrice = 0m;
		_previousClose = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentClose = candle.ClosePrice;

		if (_isFirst)
		{
			_previousClose = currentClose;
			_isFirst = false;
			return;
		}

		if (Position == 0)
		{
			if (_previousClose > currentClose && rsiValue < RsiDown)
			{
				SellMarket();
				_entryPrice = currentClose;
				_isLong = false;
				_trailingStopPrice = 0m;
			}
			else if (_previousClose <= currentClose && rsiValue >= RsiUp)
			{
				BuyMarket();
				_entryPrice = currentClose;
				_isLong = true;
				_trailingStopPrice = 0m;
			}
		}
		else
		{
			ManagePosition(currentClose);
		}

		_previousClose = currentClose;
	}

	private void ManagePosition(decimal currentPrice)
	{
		if (_entryPrice == 0m)
			return;

		if (_isLong)
		{
			var profit = currentPrice - _entryPrice;
			if (profit >= TakeProfit || -profit >= StopLoss)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (UseTrailing)
			{
				if (profit >= TrailStart)
				{
					var newStop = currentPrice - TrailStep;
					if (_trailingStopPrice < newStop)
						_trailingStopPrice = newStop;
				}

				if (_trailingStopPrice != 0m && currentPrice <= _trailingStopPrice)
					SellMarket(Math.Abs(Position));
			}
		}
		else
		{
			var profit = _entryPrice - currentPrice;
			if (profit >= TakeProfit || -profit >= StopLoss)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (UseTrailing)
			{
				if (profit >= TrailStart)
				{
					var newStop = currentPrice + TrailStep;
					if (_trailingStopPrice == 0m || _trailingStopPrice > newStop)
						_trailingStopPrice = newStop;
				}

				if (_trailingStopPrice != 0m && currentPrice >= _trailingStopPrice)
					BuyMarket(Math.Abs(Position));
			}
		}
	}
}
