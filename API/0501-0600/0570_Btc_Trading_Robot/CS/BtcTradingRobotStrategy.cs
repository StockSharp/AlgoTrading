using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC Trading Robot strategy using EMA crossover with stop loss and take profit.
/// Enters long on golden cross, short on death cross.
/// Manages risk with percentage-based stops.
/// </summary>
public class BtcTradingRobotStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private decimal _entryPrice;

	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BtcTradingRobotStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 450)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastEma = 0m;
		_prevSlowEma = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFastEma == 0m || _prevSlowEma == 0m)
		{
			_prevFastEma = fastEmaValue;
			_prevSlowEma = slowEmaValue;
			return;
		}

		var price = candle.ClosePrice;

		// Check stop loss / take profit
		if (Position > 0 && _entryPrice > 0)
		{
			var pnlPct = (price - _entryPrice) / _entryPrice * 100m;
			if (pnlPct >= TakeProfitPercent || pnlPct <= -StopLossPercent)
			{
				SellMarket();
				_entryPrice = 0m;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var pnlPct = (_entryPrice - price) / _entryPrice * 100m;
			if (pnlPct >= TakeProfitPercent || pnlPct <= -StopLossPercent)
			{
				BuyMarket();
				_entryPrice = 0m;
			}
		}

		// Golden cross - buy
		if (_prevFastEma <= _prevSlowEma && fastEmaValue > slowEmaValue && Position <= 0)
		{
			BuyMarket();
			_entryPrice = price;
		}
		// Death cross - sell
		else if (_prevFastEma >= _prevSlowEma && fastEmaValue < slowEmaValue && Position >= 0)
		{
			SellMarket();
			_entryPrice = price;
		}

		_prevFastEma = fastEmaValue;
		_prevSlowEma = slowEmaValue;
	}
}
