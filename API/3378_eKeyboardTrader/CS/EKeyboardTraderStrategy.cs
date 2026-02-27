namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// eKeyboard Trader strategy: CCI trend following.
/// Buys when CCI crosses above +100, sells when CCI crosses below -100.
/// </summary>
public class EKeyboardTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;

	private decimal _prevCci;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public EKeyboardTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevCci <= 100 && cci > 100 && Position <= 0) BuyMarket();
			else if (_prevCci >= -100 && cci < -100 && Position >= 0) SellMarket();
		}

		_prevCci = cci;
		_hasPrev = true;
	}
}
