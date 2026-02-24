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
/// Simple MA crossover strategy with fixed profit and loss levels.
/// </summary>
public class TableToFilterTradesPerDayStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _profitPoints;
	private readonly StrategyParam<decimal> _lossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _target;
	private decimal _stop;
	private decimal _prevFast;
	private decimal _prevSlow;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal ProfitPoints { get => _profitPoints.Value; set => _profitPoints.Value = value; }
	public decimal LossPoints { get => _lossPoints.Value; set => _lossPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TableToFilterTradesPerDayStrategy()
	{
		_fastLength = Param(nameof(FastLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Length for fast SMA", "Parameters");
		_slowLength = Param(nameof(SlowLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length for slow SMA", "Parameters");
		_profitPoints = Param(nameof(ProfitPoints), 300m)
			.SetDisplay("Profit", "Take profit in price units", "Parameters");
		_lossPoints = Param(nameof(LossPoints), 300m)
			.SetDisplay("Loss", "Stop loss in price units", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "Parameters");
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
		_entryPrice = 0m;
		_target = 0m;
		_stop = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check exits first
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _target || candle.ClosePrice <= _stop)
			{
				SellMarket();
				_entryPrice = 0m;
				_prevFast = fastValue;
				_prevSlow = slowValue;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _target || candle.ClosePrice >= _stop)
			{
				BuyMarket();
				_entryPrice = 0m;
				_prevFast = fastValue;
				_prevSlow = slowValue;
				return;
			}
		}

		// Entries only when flat
		if (Position == 0 && _prevFast != 0 && _prevSlow != 0)
		{
			var crossUp = _prevFast <= _prevSlow && fastValue > slowValue;
			var crossDown = _prevFast >= _prevSlow && fastValue < slowValue;

			if (crossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_target = candle.ClosePrice + ProfitPoints;
				_stop = candle.ClosePrice - LossPoints;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_target = candle.ClosePrice - ProfitPoints;
				_stop = candle.ClosePrice + LossPoints;
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
