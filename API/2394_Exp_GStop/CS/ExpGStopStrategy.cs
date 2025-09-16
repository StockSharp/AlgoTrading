using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Specifies how stop limits are calculated.
/// </summary>
public enum StopMode
{
	Percent,
	Currency
}

/// <summary>
/// Global stop strategy that closes all positions when profit or loss reaches defined thresholds.
/// </summary>
public class ExpGStopStrategy : Strategy
{
	private readonly StrategyParam<StopMode> _mode;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _startValue;
	private bool _stop;

	/// <summary>
	/// Stop calculation mode.
	/// </summary>
	public StopMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Maximum allowed loss.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Desired profit target.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic checking.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpGStopStrategy"/>.
	/// </summary>
	public ExpGStopStrategy()
	{
		_mode = Param(nameof(Mode), StopMode.Percent)
			.SetDisplay("Mode", "Stop mode", "Risk")
			.SetCanOptimize();

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Maximum loss (percent or currency)", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target (percent or currency)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_startValue = Portfolio.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var equity = Portfolio.CurrentValue ?? 0m;
		var profit = equity - _startValue;

		if (!_stop)
		{
			var reached = Mode == StopMode.Percent
				? profit / _startValue * 100m <= -StopLoss || profit / _startValue * 100m >= TakeProfit
				: profit <= -StopLoss || profit >= TakeProfit;

			if (reached)
				_stop = true;
		}

		if (_stop)
		{
			if (Position != 0)
			{
				// Close open position to enforce global stop.
				ClosePosition();
			}
			else
			{
				// Reset stop state after position is flattened.
				_stop = false;
				_startValue = equity;
			}
		}
	}
}
