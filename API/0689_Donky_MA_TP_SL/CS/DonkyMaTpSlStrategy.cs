using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with two take-profit levels and a stop-loss.
/// </summary>
public class DonkyMaTpSlStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit1Pct;
	private readonly StrategyParam<decimal> _takeProfit2Pct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private bool _tp1Triggered;
	private decimal _tp1;
	private decimal _tp2;
	private decimal _sl;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	/// <summary>
	/// Fast moving average length (default: 10)
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow moving average length (default: 30)
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// First take-profit percentage (default: 0.03)
	/// </summary>
	public decimal TakeProfit1Pct { get => _takeProfit1Pct.Value; set => _takeProfit1Pct.Value = value; }

	/// <summary>
	/// Second take-profit percentage (default: 0.06)
	/// </summary>
	public decimal TakeProfit2Pct { get => _takeProfit2Pct.Value; set => _takeProfit2Pct.Value = value; }

	/// <summary>
	/// Stop-loss percentage (default: 0.01)
	/// </summary>
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public DonkyMaTpSlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast MA Length", "Fast moving average length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow MA Length", "Slow moving average length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 10);

		_takeProfit1Pct = Param(nameof(TakeProfit1Pct), 0.03m)
			.SetDisplay("Take Profit 1 (%)", "First take-profit percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_takeProfit2Pct = Param(nameof(TakeProfit2Pct), 0.06m)
			.SetDisplay("Take Profit 2 (%)", "Second take-profit percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.02m, 0.1m, 0.02m);

		_stopLossPct = Param(nameof(StopLossPct), 0.01m)
			.SetDisplay("Stop Loss (%)", "Stop-loss percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.03m, 0.005m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var fastMa = new SimpleMovingAverage { Length = FastLength };
		var slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_hasPrev)
		{
			var crossedUp = _prevFast <= _prevSlow && fast > slow;
			var crossedDown = _prevFast >= _prevSlow && fast < slow;

			if (crossedUp)
			{
				if (Position <= 0)
					OpenLong(candle.ClosePrice);
			}
			else if (crossedDown)
			{
				if (Position >= 0)
					OpenShort(candle.ClosePrice);
			}
		}

		ManagePosition(candle.ClosePrice);

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}

	private void OpenLong(decimal price)
	{
		BuyMarket();
		_tp1 = price * (1 + TakeProfit1Pct);
		_tp2 = price * (1 + TakeProfit2Pct);
		_sl = price * (1 - StopLossPct);
		_tp1Triggered = false;
	}

	private void OpenShort(decimal price)
	{
		SellMarket();
		_tp1 = price * (1 - TakeProfit1Pct);
		_tp2 = price * (1 - TakeProfit2Pct);
		_sl = price * (1 + StopLossPct);
		_tp1Triggered = false;
	}

	private void ManagePosition(decimal price)
	{
		if (Position > 0)
		{
			if (!_tp1Triggered && price >= _tp1)
			{
				SellMarket(Position / 2m);
				_tp1Triggered = true;
			}
			else if (price <= _sl || price >= _tp2)
			{
				SellMarket();
				_tp1Triggered = false;
			}
		}
		else if (Position < 0)
		{
			if (!_tp1Triggered && price <= _tp1)
			{
				BuyMarket(Math.Abs(Position) / 2m);
				_tp1Triggered = true;
			}
			else if (price >= _sl || price <= _tp2)
			{
				BuyMarket(Math.Abs(Position));
				_tp1Triggered = false;
			}
		}
	}
}
