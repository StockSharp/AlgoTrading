using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that manages existing position with profit trailing and take profit.
/// </summary>
public class ProfitTrailingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;
	private bool _trailingActive;

	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ProfitTrailingStrategy()
	{
		_trailingStep = Param(nameof(TrailingStep), 2m)
			.SetDisplay("Trailing Profit", "Profit step to activate trailing", "General");

		_takeProfit = Param(nameof(TakeProfit), 5m)
			.SetDisplay("Take Profit", "Profit to close position", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			_trailingActive = false;
			_entryPrice = 0m;
			return;
		}

		if (_entryPrice == 0m)
			_entryPrice = candle.ClosePrice;

		var profit = (candle.ClosePrice - _entryPrice) * Position;

		if (!_trailingActive)
		{
			if (profit > TrailingStep)
			{
				_trailingStop = profit - TrailingStep;
				_trailingActive = true;
			}
		}
		else
		{
			var newStop = profit - TrailingStep;
			if (newStop > _trailingStop)
				_trailingStop = newStop;
		}

		if ((_trailingActive && profit <= _trailingStop) || profit >= TakeProfit)
			ClosePosition();
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket();
		else if (Position < 0)
			BuyMarket();

		_trailingActive = false;
		_trailingStop = 0m;
		_entryPrice = 0m;
	}
}
