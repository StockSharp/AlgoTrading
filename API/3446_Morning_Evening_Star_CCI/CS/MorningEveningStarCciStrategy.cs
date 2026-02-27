namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Morning/Evening Star + CCI strategy.
/// Buys on morning star with negative CCI, sells on evening star with positive CCI.
/// </summary>
public class MorningEveningStarCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;

	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciLevel { get => _cciLevel.Value; set => _cciLevel.Value = value; }

	public MorningEveningStarCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
		_cciLevel = Param(nameof(CciLevel), 0m)
			.SetDisplay("CCI Level", "CCI threshold for confirmation", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		_prevPrevCandle = null;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCandle != null && _prevPrevCandle != null)
		{
			var prevBody = Math.Abs(_prevCandle.ClosePrice - _prevCandle.OpenPrice);
			var prevRange = _prevCandle.HighPrice - _prevCandle.LowPrice;
			var isSmallBody = prevRange > 0 && prevBody < prevRange * 0.3m;

			var firstBearish = _prevPrevCandle.OpenPrice > _prevPrevCandle.ClosePrice;
			var currBullish = candle.ClosePrice > candle.OpenPrice;
			var isMorningStar = firstBearish && isSmallBody && currBullish &&
							   candle.ClosePrice > _prevPrevCandle.OpenPrice * 0.5m + _prevPrevCandle.ClosePrice * 0.5m;

			var firstBullish = _prevPrevCandle.ClosePrice > _prevPrevCandle.OpenPrice;
			var currBearish = candle.OpenPrice > candle.ClosePrice;
			var isEveningStar = firstBullish && isSmallBody && currBearish &&
							   candle.ClosePrice < _prevPrevCandle.OpenPrice * 0.5m + _prevPrevCandle.ClosePrice * 0.5m;

			if (isMorningStar && cciValue < -CciLevel && Position <= 0)
				BuyMarket();
			else if (isEveningStar && cciValue > CciLevel && Position >= 0)
				SellMarket();
		}

		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;
	}
}
