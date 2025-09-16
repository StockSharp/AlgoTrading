using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TakeMode
{
	Percent,
	Currency
}

/// <summary>
/// Strategy that closes all positions when profit reaches a specified threshold.
/// </summary>
public class GlobalTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<TakeMode> _mode;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _initialBalance;
	private bool _targetReached;

	/// <summary>
	/// Take profit mode (Percent or Currency).
	/// </summary>
	public TakeMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Take profit threshold.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GlobalTakeProfitStrategy"/>.
	/// </summary>
	public GlobalTakeProfitStrategy()
	{
		_mode = Param(nameof(Mode), TakeMode.Percent)
			.SetDisplay("Mode", "Take profit mode", "Strategy");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Threshold value", "Strategy")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var profit = PnL;

		if (profit < 0m && !_targetReached)
			return;

		if (!_targetReached)
		{
			if (Mode == TakeMode.Percent)
			{
				if (_initialBalance <= 0m)
					return;

				var ratio = Math.Abs(100m * profit / _initialBalance);
				if (ratio >= TakeProfit)
					_targetReached = true;
			}
			else
			{
				if (Math.Abs(profit) >= TakeProfit)
					_targetReached = true;
			}
		}

		if (_targetReached)
		{
			if (Position != 0)
			{
				CloseAll("Global take profit reached");
			}
			else
			{
				_targetReached = false;
			}
		}
	}
}
