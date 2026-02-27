namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Morning/Evening Star + MFI strategy.
/// Buys on morning star with low MFI, sells on evening star with high MFI.
/// </summary>
public class MorningEveningMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiLow;
	private readonly StrategyParam<decimal> _mfiHigh;

	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal MfiLow { get => _mfiLow.Value; set => _mfiLow.Value = value; }
	public decimal MfiHigh { get => _mfiHigh.Value; set => _mfiHigh.Value = value; }

	public MorningEveningMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "MFI period", "Indicators");
		_mfiLow = Param(nameof(MfiLow), 40m)
			.SetDisplay("MFI Low", "MFI oversold threshold", "Signals");
		_mfiHigh = Param(nameof(MfiHigh), 60m)
			.SetDisplay("MFI High", "MFI overbought threshold", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		_prevPrevCandle = null;
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCandle != null && _prevPrevCandle != null)
		{
			var prevBody = Math.Abs(_prevCandle.ClosePrice - _prevCandle.OpenPrice);
			var prevRange = _prevCandle.HighPrice - _prevCandle.LowPrice;
			var isSmallBody = prevRange > 0 && prevBody < prevRange * 0.3m;

			var firstBearish = _prevPrevCandle.OpenPrice > _prevPrevCandle.ClosePrice;
			var currBullish = candle.ClosePrice > candle.OpenPrice;
			var isMorningStar = firstBearish && isSmallBody && currBullish;

			var firstBullish = _prevPrevCandle.ClosePrice > _prevPrevCandle.OpenPrice;
			var currBearish = candle.OpenPrice > candle.ClosePrice;
			var isEveningStar = firstBullish && isSmallBody && currBearish;

			if (isMorningStar && mfiValue < MfiLow && Position <= 0)
				BuyMarket();
			else if (isEveningStar && mfiValue > MfiHigh && Position >= 0)
				SellMarket();
		}

		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;
	}
}
