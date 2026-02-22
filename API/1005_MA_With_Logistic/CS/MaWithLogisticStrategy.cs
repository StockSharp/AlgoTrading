using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA crossover strategy with percent-based exits.
/// </summary>
public class MaWithLogisticStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fastMa;
	private EMA _slowMa;
	private decimal _entryPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaWithLogisticStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9).SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "Indicators");
		_slowLength = Param(nameof(SlowLength), 21).SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "Indicators");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m).SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;

		_fastMa = new EMA { Length = FastLength };
		_slowMa = new EMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		var close = candle.ClosePrice;
		var longCond = close > fast && fast > slow;
		var shortCond = close < fast && fast < slow;

		if (longCond && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var tp = _entryPrice * (1m + TakeProfitPercent / 100m);
			var sl = _entryPrice * (1m - StopLossPercent / 100m);
			if (close >= tp || close <= sl)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var tp = _entryPrice * (1m - TakeProfitPercent / 100m);
			var sl = _entryPrice * (1m + StopLossPercent / 100m);
			if (close <= tp || close >= sl)
				BuyMarket(Math.Abs(Position));
		}
	}
}
