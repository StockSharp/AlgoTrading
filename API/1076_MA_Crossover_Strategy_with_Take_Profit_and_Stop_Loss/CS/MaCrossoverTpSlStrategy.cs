using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaCrossoverTpSlStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitDynamicPercent;
	private readonly StrategyParam<decimal> _stopLossDynamicPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private bool _wasFastLess;
	private bool _initialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitDynamicPercent { get => _takeProfitDynamicPercent.Value; set => _takeProfitDynamicPercent.Value = value; }
	public decimal StopLossDynamicPercent { get => _stopLossDynamicPercent.Value; set => _stopLossDynamicPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaCrossoverTpSlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_takeProfitDynamicPercent = Param(nameof(TakeProfitDynamicPercent), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Dynamic Take Profit %", "Take profit percentage after price move", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_stopLossDynamicPercent = Param(nameof(StopLossDynamicPercent), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Dynamic Stop Loss %", "Stop loss percentage after price move", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 10m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
		_wasFastLess = false;
		_initialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

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

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_wasFastLess = fast < slow;
			_initialized = true;
			return;
		}

		var isFastLess = fast < slow;

		if (_wasFastLess && !isFastLess && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_takeProfitPrice = _entryPrice * (1 + TakeProfitPercent / 100m);
			_stopLossPrice = _entryPrice * (1 - StopLossPercent / 100m);
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!_wasFastLess && isFastLess && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0m;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice > _entryPrice)
			{
				_takeProfitPrice = candle.ClosePrice * (1 + TakeProfitDynamicPercent / 100m);
				_stopLossPrice = candle.ClosePrice * (1 - StopLossDynamicPercent / 100m);
			}

			if (candle.ClosePrice >= _takeProfitPrice || candle.ClosePrice <= _stopLossPrice)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0m;
			}
		}

		_wasFastLess = isFastLess;
	}
}
