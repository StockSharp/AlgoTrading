using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that opens sequential orders in a single direction.
/// Closes all positions on reaching profit or loss limits.
/// </summary>
public class HelloSmartStrategy : Strategy
{
	public enum TradeModes
	{
		Buy,
		Sell
	}

	private readonly StrategyParam<TradeModes> _tradeMode;
	private readonly StrategyParam<decimal> _stepTicks;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossLimit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastPrice;

	public TradeModes Mode { get => _tradeMode.Value; set => _tradeMode.Value = value; }
	public decimal StepTicks { get => _stepTicks.Value; set => _stepTicks.Value = value; }
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	public decimal LossLimit { get => _lossLimit.Value; set => _lossLimit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HelloSmartStrategy()
	{
		_tradeMode = Param(nameof(Mode), TradeModes.Sell)
			.SetDisplay("Trade Direction", "Buy or Sell direction", "General");
		_stepTicks = Param(nameof(StepTicks), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Price movement to add position", "Risk");
		_profitTarget = Param(nameof(ProfitTarget), 60m)
			.SetDisplay("Profit Target", "Close all positions on this profit", "Risk");
		_lossLimit = Param(nameof(LossLimit), 5100m)
			.SetDisplay("Loss Limit", "Close all positions on this loss", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastPrice = 0m;

		var sma = new SimpleMovingAverage { Length = 1 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _unused)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var stepPrice = StepTicks * 0.01m;

		// Check PnL limits first
		if (Position != 0 && (PnL > ProfitTarget || PnL < -LossLimit))
		{
			if (Position > 0)
				SellMarket();
			else
				BuyMarket();
			_lastPrice = price;
			return;
		}

		if (Mode == TradeModes.Buy)
		{
			var needOpen = Position <= 0 || (Position > 0 && (_lastPrice - price) >= stepPrice);
			if (needOpen)
			{
				BuyMarket();
				_lastPrice = price;
			}
		}
		else
		{
			var needOpen = Position >= 0 || (Position < 0 && (price - _lastPrice) >= stepPrice);
			if (needOpen)
			{
				SellMarket();
				_lastPrice = price;
			}
		}
	}
}
