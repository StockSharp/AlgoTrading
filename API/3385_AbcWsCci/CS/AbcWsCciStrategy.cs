namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ABC WS CCI strategy: 3 white soldiers / 3 black crows pattern with CCI confirmation.
/// Buys after 3 bullish candles with CCI below 100, sells after 3 bearish candles with CCI above -100.
/// </summary>
public class AbcWsCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;

	private int _bullCount;
	private int _bearCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public AbcWsCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period for confirmation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_bullCount = 0;
		_bearCount = 0;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci)
	{
		if (candle.State != CandleStates.Finished) return;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			_bullCount++;
			_bearCount = 0;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			_bearCount++;
			_bullCount = 0;
		}
		else
		{
			_bullCount = 0;
			_bearCount = 0;
		}

		// Exit on CCI reversal
		if (Position > 0 && cci > 200) SellMarket();
		else if (Position < 0 && cci < -200) BuyMarket();

		// Entry on 3 soldiers/crows pattern
		if (_bullCount >= 3 && cci < 100 && Position <= 0) BuyMarket();
		else if (_bearCount >= 3 && cci > -100 && Position >= 0) SellMarket();
	}
}
