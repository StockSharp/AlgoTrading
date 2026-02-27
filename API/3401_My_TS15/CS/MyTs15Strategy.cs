namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// My TS15 strategy: WMA trend following with trailing stop management.
/// Enters on price crossing WMA, exits with trailing stop logic.
/// </summary>
public class MyTs15Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _trailMultiplier;

	private decimal _entryPrice;
	private decimal _bestPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal TrailMultiplier { get => _trailMultiplier.Value; set => _trailMultiplier.Value = value; }

	public MyTs15Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "WMA period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing", "Indicators");
		_trailMultiplier = Param(nameof(TrailMultiplier), 2m)
			.SetDisplay("Trail Multiplier", "ATR multiplier for trailing stop", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		_bestPrice = 0;
		var wma = new WeightedMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var trailDist = atrValue * TrailMultiplier;

		// Trailing stop check
		if (Position > 0)
		{
			if (close > _bestPrice) _bestPrice = close;
			if (_bestPrice - close > trailDist)
			{
				SellMarket();
				_entryPrice = 0;
				_bestPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (close < _bestPrice) _bestPrice = close;
			if (close - _bestPrice > trailDist)
			{
				BuyMarket();
				_entryPrice = 0;
				_bestPrice = 0;
				return;
			}
		}

		// Entry signals
		if (close > wmaValue && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_bestPrice = close;
		}
		else if (close < wmaValue && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
			_bestPrice = close;
		}
	}
}
