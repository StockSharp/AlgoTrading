using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on anchored momentum indicator.
/// </summary>
public class AnchoredMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _prev;
	private decimal _prevPrev;
	private bool _initialized;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public AnchoredMomentumStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 34).SetRange(10,100).SetDisplay("SMA Period","Period for simple moving average","Indicators").SetCanOptimize(true);
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetRange(5,50).SetDisplay("EMA Period","Period for exponential moving average","Indicators").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame()).SetDisplay("Candle Type","Type of candles to use","General");
		_stopLossPercent = Param(nameof(StopLossPercent),2m).SetRange(0.5m,10m).SetDisplay("Stop Loss %","Stop loss percentage","Risk Management").SetCanOptimize(true);
		_takeProfitPercent = Param(nameof(TakeProfitPercent),4m).SetRange(1m,20m).SetDisplay("Take Profit %","Take profit percentage","Risk Management").SetCanOptimize(true);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = 0;
		_prevPrev = 0;
		_initialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(sma, ema, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, sma);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (smaVal == 0)
			return;

		var mom = 100m * (emaVal / smaVal - 1m);

		if (!_initialized)
		{
			_prev = mom;
			_prevPrev = mom;
			_initialized = true;
			return;
		}

		var volume = Volume + Math.Abs(Position);

		if (_prev < _prevPrev && mom >= _prev && Position <= 0)
			BuyMarket(volume);
		else if (_prev > _prevPrev && mom <= _prev && Position >= 0)
			SellMarket(volume);

		_prevPrev = _prev;
		_prev = mom;
	}
}
