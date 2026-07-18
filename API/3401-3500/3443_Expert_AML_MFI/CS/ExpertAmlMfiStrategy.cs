namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Meeting Lines + MFI strategy.
/// Buys on bullish meeting lines with low MFI, sells on bearish meeting lines with high MFI.
/// </summary>
public class ExpertAmlMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiLow;
	private readonly StrategyParam<decimal> _mfiHigh;

	private ICandleMessage _prevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal MfiLow { get => _mfiLow.Value; set => _mfiLow.Value = value; }
	public decimal MfiHigh { get => _mfiHigh.Value; set => _mfiHigh.Value = value; }

	public ExpertAmlMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "MFI period", "Indicators");
		_mfiLow = Param(nameof(MfiLow), 40m)
			.SetDisplay("MFI Low", "MFI level for bullish entry", "Signals");
		_mfiHigh = Param(nameof(MfiHigh), 60m)
			.SetDisplay("MFI High", "MFI level for bearish entry", "Signals");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCandle != null)
		{
			var avgBody = (Math.Abs(candle.ClosePrice - candle.OpenPrice) +
						   Math.Abs(_prevCandle.ClosePrice - _prevCandle.OpenPrice)) / 2m;

			if (avgBody > 0)
			{
				var prevBearish = _prevCandle.OpenPrice > _prevCandle.ClosePrice;
				var currBullish = candle.ClosePrice > candle.OpenPrice;
				var closesNear = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				if (prevBearish && currBullish && closesNear && mfiValue < MfiLow && Position <= 0)
					BuyMarket();

				var prevBullish = _prevCandle.ClosePrice > _prevCandle.OpenPrice;
				var currBearish = candle.OpenPrice > candle.ClosePrice;
				var closesNear2 = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				if (prevBullish && currBearish && closesNear2 && mfiValue > MfiHigh && Position >= 0)
					SellMarket();
			}
		}

		_prevCandle = candle;
	}
}
