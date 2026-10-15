using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade assistant strategy with configurable stop-loss and take-profit.
/// Simplified from the backtesting trade assistant panel.
/// </summary>
public class BacktestingTradeAssistantPanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BacktestingTradeAssistantPanelStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for trading signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sma = null;
		_pipSize = 0m;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = CalculatePipSize();

		_sma = new SimpleMovingAverage { Length = 20 };

		SubscribeCandles(CandleType)
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var price = candle.ClosePrice;

		// Check stop-loss and take-profit
		if (Position != 0 && _entryPrice > 0m)
		{
			if (Position > 0)
			{
				if (_stopPrice.HasValue && price <= _stopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
				if (_takePrice.HasValue && price >= _takePrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
			else if (Position < 0)
			{
				if (_stopPrice.HasValue && price >= _stopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
				if (_takePrice.HasValue && price <= _takePrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
		}

		// Entry: SMA crossover
		if (Position == 0)
		{
			var pip = _pipSize > 0m ? _pipSize : 1m;

			if (price > smaValue)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = StopLossPips > 0m ? price - StopLossPips * pip : null;
				_takePrice = TakeProfitPips > 0m ? price + TakeProfitPips * pip : null;
			}
			else if (price < smaValue)
			{
				SellMarket();
				_entryPrice = price;
				_stopPrice = StopLossPips > 0m ? price + StopLossPips * pip : null;
				_takePrice = TakeProfitPips > 0m ? price - TakeProfitPips * pip : null;
			}
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 5 or 3 ? step * 10m : step;
	}
}
