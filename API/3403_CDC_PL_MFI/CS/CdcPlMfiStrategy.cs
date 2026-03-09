namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// CDC PL MFI strategy: Dark Cloud Cover and Piercing Line candlestick patterns
/// confirmed by Money Flow Index levels.
/// </summary>
public class CdcPlMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _longLevel;
	private readonly StrategyParam<decimal> _shortLevel;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevMfi;
	private bool _hasPrevMfi;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal LongLevel { get => _longLevel.Value; set => _longLevel.Value = value; }
	public decimal ShortLevel { get => _shortLevel.Value; set => _shortLevel.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public CdcPlMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Money Flow Index period", "Indicators");
		_longLevel = Param(nameof(LongLevel), 40m)
			.SetDisplay("Long Level", "MFI below this for long entry", "Signals");
		_shortLevel = Param(nameof(ShortLevel), 60m)
			.SetDisplay("Short Level", "MFI above this for short entry", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_prevMfi = 0m;
		_hasPrevMfi = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_hasPrevMfi = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		_candles.Add(candle);
		if (_candles.Count > 5)
			_candles.RemoveAt(0);

		if (_candles.Count >= 2 && _hasPrevMfi)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			// Piercing Line: prev bearish, curr bullish, opens below prev low, closes above midpoint
			var isPiercing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice < prev.LowPrice
				&& curr.ClosePrice > (prev.OpenPrice + prev.ClosePrice) / 2m;

			// Dark Cloud Cover: prev bullish, curr bearish, opens above prev high, closes below midpoint
			var isDarkCloud = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice > prev.HighPrice
				&& curr.ClosePrice < (prev.OpenPrice + prev.ClosePrice) / 2m;

			if (isPiercing && mfiValue < LongLevel && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (isDarkCloud && mfiValue > ShortLevel && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}

			// Exit on MFI crossing
			if (Position > 0 && _prevMfi >= ShortLevel && mfiValue < ShortLevel && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
			else if (Position < 0 && _prevMfi <= LongLevel && mfiValue > LongLevel && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevMfi = mfiValue;
		_hasPrevMfi = true;
	}
}
