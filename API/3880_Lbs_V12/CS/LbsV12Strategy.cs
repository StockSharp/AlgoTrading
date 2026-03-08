using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LBS V12 strategy - ATR channel breakout with EMA trend.
/// Buys when close breaks above EMA + ATR.
/// Sells when close breaks below EMA - ATR.
/// </summary>
public class LbsV12Strategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _cooldownCandles;
	private readonly StrategyParam<DataType> _candleType;

	private int _cooldownRemaining;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int CooldownCandles { get => _cooldownCandles.Value; set => _cooldownCandles.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LbsV12Strategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetDisplay("EMA Period", "EMA lookback", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR lookback", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "Channel width multiplier", "Indicators");
		_cooldownCandles = Param(nameof(CooldownCandles), 100)
			.SetDisplay("Cooldown", "Candles between signals", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldownRemaining = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_cooldownRemaining = 0;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;
		var mult = AtrMultiplier;

		if (close > ema + atr * mult && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownCandles;
		}
		else if (close < ema - atr * mult && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownCandles;
		}
	}
}
