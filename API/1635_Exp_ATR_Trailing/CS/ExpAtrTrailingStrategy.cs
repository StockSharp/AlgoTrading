using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that manages existing positions using an ATR based trailing stop.
/// The strategy does not generate entry signals and only trails open positions.
/// </summary>
public class ExpAtrTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _buyFactor;
	private readonly StrategyParam<decimal> _sellFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longTrail;
	private decimal _shortTrail;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal BuyFactor { get => _buyFactor.Value; set => _buyFactor.Value = value; }
	public decimal SellFactor { get => _sellFactor.Value; set => _sellFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpAtrTrailingStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period");
		_buyFactor = Param(nameof(BuyFactor), 2m).SetDisplay("Buy Factor");
		_sellFactor = Param(nameof(SellFactor), 2m).SetDisplay("Sell Factor");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longTrail = 0m;
		_shortTrail = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		// Work only with finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Manage long position trailing stop
		if (Position > 0)
		{
			var stop = candle.ClosePrice - atrValue * BuyFactor;
			if (stop > _longTrail)
				_longTrail = stop;

			if (candle.LowPrice <= _longTrail)
				SellMarket(Position);
		}
		// Manage short position trailing stop
		else if (Position < 0)
		{
			var stop = candle.ClosePrice + atrValue * SellFactor;
			if (stop < _shortTrail || _shortTrail == 0m)
				_shortTrail = stop;

			if (candle.HighPrice >= _shortTrail)
				BuyMarket(Math.Abs(Position));
		}
	}
}
