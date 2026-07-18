namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Hammer/Hanging Man + MFI strategy.
/// Buys on hammer with low MFI (oversold), sells on hanging man with high MFI (overbought).
/// </summary>
public class AhHmMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiLow;
	private readonly StrategyParam<decimal> _mfiHigh;
	private readonly StrategyParam<int> _signalCooldownCandles;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal MfiLow { get => _mfiLow.Value; set => _mfiLow.Value = value; }
	public decimal MfiHigh { get => _mfiHigh.Value; set => _mfiHigh.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public AhHmMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "MFI period", "Indicators");
		_mfiLow = Param(nameof(MfiLow), 35m)
			.SetDisplay("MFI Low", "MFI oversold threshold for buy", "Signals");
		_mfiHigh = Param(nameof(MfiHigh), 65m)
			.SetDisplay("MFI High", "MFI overbought threshold for sell", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candlesSinceTrade = SignalCooldownCandles;
		var rsi = new RelativeStrengthIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0 || body <= 0) return;

		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var isHammer = lowerShadow > body * 2.5m && upperShadow < body * 0.5m;
		var isHangingMan = upperShadow > body * 2.5m && lowerShadow < body * 0.5m;

		if (isHammer && mfiValue < MfiLow && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (isHangingMan && mfiValue > MfiHigh && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}
	}
}
