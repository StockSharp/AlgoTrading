using System;
using System.Collections.Generic;

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
	private decimal _volume;
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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_volume = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
		}
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var crossUp = _prevFast <= _prevSlow && fastValue > slowValue;
		var crossDown = _prevFast >= _prevSlow && fastValue < slowValue;

		if (crossUp && Position <= 0)
		EnterLong(candle.ClosePrice);
		else if (crossDown && Position >= 0)
		EnterShort(candle.ClosePrice);

		ManagePosition(candle);

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}

	private void EnterLong(decimal price)
	{
		_volume = Volume + Math.Abs(Position);
		BuyMarket(_volume);
		_entryPrice = price;
		_target = price + ProfitPoints;
		_stop = price - LossPoints;
	}

	private void EnterShort(decimal price)
	{
		_volume = Volume + Math.Abs(Position);
		SellMarket(_volume);
		_entryPrice = price;
		_target = price - ProfitPoints;
		_stop = price + LossPoints;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (candle.HighPrice >= _target || candle.LowPrice <= _stop)
			{
				SellMarket(Math.Abs(Position));
				ResetTrade();
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _target || candle.HighPrice >= _stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrade();
			}
		}
	}

	private void ResetTrade()
	{
		_entryPrice = 0m;
		_target = 0m;
		_stop = 0m;
		_volume = 0m;
	}
}
