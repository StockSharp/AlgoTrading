using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade management strategy with break-even and trailing stop logic.
/// Enters on candle direction, manages with SL/TP/trailing/breakeven.
/// </summary>
public class LacustStopAndBeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStart;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _breakevenGain;
	private readonly StrategyParam<decimal> _breakeven;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal BreakevenGain { get => _breakevenGain.Value; set => _breakevenGain.Value = value; }
	public decimal Breakeven { get => _breakeven.Value; set => _breakeven.Value = value; }

	public LacustStopAndBeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle type", "Candle type", "General");
		_stopLoss = Param(nameof(StopLoss), 400m)
			.SetDisplay("Stop loss", "Stop loss distance", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take profit", "Take profit distance", "Risk");
		_trailingStart = Param(nameof(TrailingStart), 300m)
			.SetDisplay("Trailing start", "Profit to activate trailing", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetDisplay("Trailing stop", "Trailing stop distance", "Risk");
		_breakevenGain = Param(nameof(BreakevenGain), 250m)
			.SetDisplay("Breakeven gain", "Profit for breakeven move", "Risk");
		_breakeven = Param(nameof(Breakeven), 100m)
			.SetDisplay("Breakeven", "Profit locked at breakeven", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
				_takePrice = _entryPrice + TakeProfit;
			}
			else if (candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
				_takePrice = _entryPrice - TakeProfit;
			}
			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice - _entryPrice >= BreakevenGain && _stopPrice < _entryPrice + Breakeven)
				_stopPrice = _entryPrice + Breakeven;

			if (candle.ClosePrice - _entryPrice >= TrailingStart && _stopPrice < candle.ClosePrice - TrailingStop)
				_stopPrice = candle.ClosePrice - TrailingStop;

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - candle.ClosePrice >= BreakevenGain && _stopPrice > _entryPrice - Breakeven)
				_stopPrice = _entryPrice - Breakeven;

			if (_entryPrice - candle.ClosePrice >= TrailingStart && _stopPrice > candle.ClosePrice + TrailingStop)
				_stopPrice = candle.ClosePrice + TrailingStop;

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
			}
		}
	}
}
