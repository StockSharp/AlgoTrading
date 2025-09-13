using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope based scalping strategy.
/// Enters when price crosses moving average bands and exits with take profit, stop loss or trailing stop.
/// </summary>
public class RampokScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _prevClose;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// Deviation percent for envelopes.
	/// </summary>
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }

	/// <summary>
	/// Take profit value.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss value.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Trailing stop value.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RampokScalpStrategy"/> class.
	/// </summary>
	public RampokScalpStrategy()
	{
		_period = Param(nameof(Period), 15)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Moving average period", "General");

		_deviation = Param(nameof(Deviation), 0.07m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Envelope deviation percent", "General");

		_takeProfit = Param(nameof(TakeProfit), 0.004m)
			.SetDisplay("Take Profit", "Target profit in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.01m, 0.001m);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop loss in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 0.01m, 0.001m);

		_trailingStop = Param(nameof(TrailingStop), 0.0011m)
			.SetDisplay("Trailing Stop", "Trailing stop in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 0.01m, 0.001m);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SMA { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper = smaValue * (1 + Deviation);
		var lower = smaValue * (1 - Deviation);

		if (Position == 0)
		{
			if (_prevLower.HasValue && _prevClose.HasValue && _prevClose < _prevLower && candle.ClosePrice > lower)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_highestPrice = candle.ClosePrice;
			}
			else if (_prevUpper.HasValue && _prevClose.HasValue && _prevClose > _prevUpper && candle.ClosePrice < upper)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_lowestPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

			if (TakeProfit > 0m && candle.ClosePrice - _entryPrice >= TakeProfit)
				SellMarket();
			else if (StopLoss > 0m && _entryPrice - candle.ClosePrice >= StopLoss)
				SellMarket();
			else if (TrailingStop > 0m)
			{
				var trail = _highestPrice - TrailingStop;
				if (candle.ClosePrice <= trail)
					SellMarket();
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

			if (TakeProfit > 0m && _entryPrice - candle.ClosePrice >= TakeProfit)
				BuyMarket();
			else if (StopLoss > 0m && candle.ClosePrice - _entryPrice >= StopLoss)
				BuyMarket();
			else if (TrailingStop > 0m)
			{
				var trail = _lowestPrice + TrailingStop;
				if (candle.ClosePrice >= trail)
					BuyMarket();
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
