using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy entering on extreme Stochastic and Williams %R values.
/// </summary>
public class TheMasterMindStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _breakEven;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;
	private WilliamsR _wpr = null!;

	private decimal _entryPrice;
	private decimal _stopLossLevel;
	private decimal _takeProfitLevel;

	/// <summary>
	/// Stochastic base length.
	/// </summary>
	public int StochasticLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Trailing step in points.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Profit in points to move stop loss to entry price.
	/// </summary>
	public decimal BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TheMasterMindStrategy"/>.
	/// </summary>
	public TheMasterMindStrategy()
	{
		_stochLength = Param(nameof(StochasticLength), 100)
			.SetDisplay("Stochastic Length", "Base length for Stochastic", "Indicators")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 150m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 450m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 350m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 80m)
			.SetDisplay("Trailing Step", "Trailing step size in points", "Risk");

		_breakEven = Param(nameof(BreakEven), 0m)
			.SetDisplay("Break Even", "Distance to move stop to entry", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = 3 },
			D = { Length = 3 }
		};

		_wpr = new WilliamsR { Length = StochasticLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, _wpr, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.D is not decimal d)
			return;

		var wpr = wprValue.ToDecimal();

		var buySignal = d < 3m && wpr < -99.9m;
		var sellSignal = d > 97m && wpr > -0.1m;

		if (!_stochastic.IsFormed || !_wpr.IsFormed)
			return;

		var step = Security.PriceStep ?? 1m;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
			{
				BuyMarket(Volume + (Position < 0 ? -Position : 0m));

				_entryPrice = candle.ClosePrice;
				_stopLossLevel = _entryPrice - StopLoss * step;
				_takeProfitLevel = _entryPrice + TakeProfit * step;
			}
			else if (sellSignal && Position >= 0)
			{
				SellMarket(Volume + (Position > 0 ? Position : 0m));

				_entryPrice = candle.ClosePrice;
				_stopLossLevel = _entryPrice + StopLoss * step;
				_takeProfitLevel = _entryPrice - TakeProfit * step;
			}
		}

		if (Position > 0)
		{
			if (BreakEven > 0 && candle.ClosePrice - _entryPrice > BreakEven * step)
				_stopLossLevel = Math.Max(_stopLossLevel, _entryPrice);

			if (TrailingStop > 0 && candle.ClosePrice - _entryPrice > TrailingStop * step)
				_stopLossLevel = Math.Max(_stopLossLevel, candle.ClosePrice - TrailingStep * step);

			if (candle.LowPrice <= _stopLossLevel || candle.HighPrice >= _takeProfitLevel)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (BreakEven > 0 && _entryPrice - candle.ClosePrice > BreakEven * step)
				_stopLossLevel = Math.Min(_stopLossLevel, _entryPrice);

			if (TrailingStop > 0 && _entryPrice - candle.ClosePrice > TrailingStop * step)
				_stopLossLevel = Math.Min(_stopLossLevel, candle.ClosePrice + TrailingStep * step);

			if (candle.HighPrice >= _stopLossLevel || candle.LowPrice <= _takeProfitLevel)
				BuyMarket(-Position);
		}
	}
}
