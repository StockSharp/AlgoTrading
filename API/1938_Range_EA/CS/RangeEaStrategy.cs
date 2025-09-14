namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Range breakout strategy with optional step-down averaging and turn reversal.
/// </summary>
public class RangeEaStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _range;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<bool> _useTurn;
	private readonly StrategyParam<decimal> _turn;
	private readonly StrategyParam<decimal> _lotMultiplicator;
	private readonly StrategyParam<decimal> _turnTakeProfit;
	private readonly StrategyParam<bool> _useStepDown;
	private readonly StrategyParam<decimal> _stepDown;
	private readonly StrategyParam<bool> _useTradeTime;
	private readonly StrategyParam<TimeSpan> _openTradeTime;
	private readonly StrategyParam<TimeSpan> _closeTradeTime;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _nextStepPrice;
	private decimal _turnPrice;

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal Range
	{
		get => _range.Value;
		set => _range.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	public bool UseTurn
	{
		get => _useTurn.Value;
		set => _useTurn.Value = value;
	}

	public decimal Turn
	{
		get => _turn.Value;
		set => _turn.Value = value;
	}

	public decimal LotMultiplicator
	{
		get => _lotMultiplicator.Value;
		set => _lotMultiplicator.Value = value;
	}

	public decimal TurnTakeProfit
	{
		get => _turnTakeProfit.Value;
		set => _turnTakeProfit.Value = value;
	}

	public bool UseStepDown
	{
		get => _useStepDown.Value;
		set => _useStepDown.Value = value;
	}

	public decimal StepDown
	{
		get => _stepDown.Value;
		set => _stepDown.Value = value;
	}

	public bool UseTradeTime
	{
		get => _useTradeTime.Value;
		set => _useTradeTime.Value = value;
	}

	public TimeSpan OpenTradeTime
	{
		get => _openTradeTime.Value;
		set => _openTradeTime.Value = value;
	}

	public TimeSpan CloseTradeTime
	{
		get => _closeTradeTime.Value;
		set => _closeTradeTime.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RangeEaStrategy()
	{
		_maLength = Param(nameof(MaLength), 21)
			.SetDisplay("MA Length", "Moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_range = Param(nameof(Range), 250m)
			.SetDisplay("Range", "Price range from MA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Fixed take profit", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 250m)
			.SetDisplay("Stop Loss", "Fixed stop loss", "Parameters");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Parameters");

		_trailingStop = Param(nameof(TrailingStop), 250m)
			.SetDisplay("Trailing", "Trailing stop distance", "Parameters");

		_useTurn = Param(nameof(UseTurn), true)
			.SetDisplay("Use Turn", "Enable reversal module", "Parameters");

		_turn = Param(nameof(Turn), 250m)
			.SetDisplay("Turn", "Reversal distance", "Parameters");

		_lotMultiplicator = Param(nameof(LotMultiplicator), 1.65m)
			.SetDisplay("Lot Mult", "Volume multiplier for reversal", "Parameters");

		_turnTakeProfit = Param(nameof(TurnTakeProfit), 500m)
			.SetDisplay("Turn TP", "Take profit after reversal", "Parameters");

		_useStepDown = Param(nameof(UseStepDown), false)
			.SetDisplay("Use StepDown", "Enable averaging module", "Parameters");

		_stepDown = Param(nameof(StepDown), 150m)
			.SetDisplay("Step Down", "Averaging step", "Parameters");

		_useTradeTime = Param(nameof(UseTradeTime), false)
			.SetDisplay("Use Trade Time", "Limit trading hours", "Parameters");

		_openTradeTime = Param(nameof(OpenTradeTime), TimeSpan.Parse("08:00:00"))
			.SetDisplay("Open Time", "Trading start time", "Parameters");

		_closeTradeTime = Param(nameof(CloseTradeTime), TimeSpan.Parse("21:30:00"))
			.SetDisplay("Close Time", "Trading end time", "Parameters");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Volume", "Order volume", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_entryPrice = default;
		_stopPrice = default;
		_takeProfitPrice = default;
		_nextStepPrice = default;
		_turnPrice = default;

		var ma = new SimpleMovingAverage
		{
			Length = MaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.TimeOfDay;
		if (UseTradeTime)
		{
			if (time < OpenTradeTime || time > CloseTradeTime)
			{
				if (Position != 0)
					ClosePosition();
				return;
			}
		}

		var price = candle.ClosePrice;

		// Manage existing long position
		if (Position > 0)
		{
			if (price >= _takeProfitPrice || price <= _stopPrice)
			{
				ClosePosition();
				return;
			}

			if (UseTrailingStop && price - _entryPrice >= TrailingStop)
				_stopPrice = Math.Max(_stopPrice, price - TrailingStop);

			if (UseTurn && price <= _turnPrice)
			{
				ClosePosition();
				SellMarket(OrderVolume * LotMultiplicator);
				_entryPrice = price;
				_stopPrice = price + StopLoss;
				_takeProfitPrice = price - TurnTakeProfit;
				if (UseStepDown)
					_nextStepPrice = _entryPrice + StepDown;
				if (UseTurn)
					_turnPrice = _entryPrice + Turn;
				return;
			}

			if (UseStepDown && price <= _nextStepPrice)
			{
				BuyMarket(OrderVolume);
				_nextStepPrice -= StepDown;
			}
		}
		// Manage existing short position
		else if (Position < 0)
		{
			if (price <= _takeProfitPrice || price >= _stopPrice)
			{
				ClosePosition();
				return;
			}

			if (UseTrailingStop && _entryPrice - price >= TrailingStop)
				_stopPrice = Math.Min(_stopPrice, price + TrailingStop);

			if (UseTurn && price >= _turnPrice)
			{
				ClosePosition();
				BuyMarket(OrderVolume * LotMultiplicator);
				_entryPrice = price;
				_stopPrice = price - StopLoss;
				_takeProfitPrice = price + TurnTakeProfit;
				if (UseStepDown)
					_nextStepPrice = _entryPrice - StepDown;
				if (UseTurn)
					_turnPrice = _entryPrice - Turn;
				return;
			}

			if (UseStepDown && price >= _nextStepPrice)
			{
				SellMarket(OrderVolume);
				_nextStepPrice += StepDown;
			}
		}
		else
		{
				// Entry conditions when flat
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (price >= maValue + Range)
			{
				BuyMarket(OrderVolume);
				_entryPrice = price;
				_stopPrice = price - StopLoss;
				_takeProfitPrice = price + TakeProfit;
				if (UseStepDown)
					_nextStepPrice = _entryPrice - StepDown;
				if (UseTurn)
					_turnPrice = _entryPrice - Turn;
			}
			else if (price <= maValue - Range)
			{
				SellMarket(OrderVolume);
				_entryPrice = price;
				_stopPrice = price + StopLoss;
				_takeProfitPrice = price - TakeProfit;
				if (UseStepDown)
					_nextStepPrice = _entryPrice + StepDown;
				if (UseTurn)
					_turnPrice = _entryPrice + Turn;
			}
		}
	}
}

