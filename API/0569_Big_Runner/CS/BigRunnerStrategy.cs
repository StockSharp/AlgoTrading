using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Runner Strategy - trades SMA crossover with stop loss and take profit.
/// </summary>
public class BigRunnerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BigRunnerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 120)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "SMA");

		_slowLength = Param(nameof(SlowLength), 450)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "SMA");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent from entry", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent from entry", "Risk");

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
		_prevFast = 0m;
		_prevSlow = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SimpleMovingAverage { Length = FastLength };
		var slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0m || _prevSlow == 0m)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		// Golden cross - buy
		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		// Death cross - sell
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		// Stop loss / take profit for long
		if (Position > 0 && _entryPrice > 0)
		{
			var pnlPercent = (candle.ClosePrice - _entryPrice) / _entryPrice * 100m;
			if (pnlPercent <= -StopLossPercent || pnlPercent >= TakeProfitPercent)
			{
				SellMarket();
				_entryPrice = 0m;
			}
		}
		// Stop loss / take profit for short
		else if (Position < 0 && _entryPrice > 0)
		{
			var pnlPercent = (_entryPrice - candle.ClosePrice) / _entryPrice * 100m;
			if (pnlPercent <= -StopLossPercent || pnlPercent >= TakeProfitPercent)
			{
				BuyMarket();
				_entryPrice = 0m;
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
