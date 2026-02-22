using System;
using System.Linq;

using Ecng.Common;

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
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _wasFastLess;
	private bool _initialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaCrossoverTpSlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 12).SetGreaterThanZero();
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m).SetGreaterThanZero();
		_stopLossPercent = Param(nameof(StopLossPercent), 5m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_wasFastLess = false;
		_initialized = false;

		var fastMa = new EMA { Length = FastLength };
		var slowMa = new EMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();
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
			BuyMarket();
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var tp = _entryPrice * (1 + TakeProfitPercent / 100m);
			var sl = _entryPrice * (1 - StopLossPercent / 100m);

			if (candle.ClosePrice >= tp || candle.ClosePrice <= sl || (!_wasFastLess && isFastLess))
			{
				SellMarket();
				_entryPrice = 0m;
			}
		}

		_wasFastLess = isFastLess;
	}
}
