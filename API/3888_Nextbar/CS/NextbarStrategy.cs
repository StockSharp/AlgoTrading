using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NextbarStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _cooldownCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int CooldownCandles { get => _cooldownCandles.Value; set => _cooldownCandles.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NextbarStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50).SetDisplay("EMA Period", "EMA filter", "Indicators");
		_cooldownCandles = Param(nameof(CooldownCandles), 200).SetDisplay("Cooldown", "Candles between signals", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = default;
		_prevClose = default;
		_hasPrev = default;
		_cooldownRemaining = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevOpen = 0;
		_prevClose = 0;
		_hasPrev = false;
		_cooldownRemaining = 0;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (!_hasPrev) { _prevOpen = candle.OpenPrice; _prevClose = close; _hasPrev = true; return; }

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevOpen = candle.OpenPrice;
			_prevClose = close;
			return;
		}

		var prevBullish = _prevClose > _prevOpen;
		var prevBearish = _prevClose < _prevOpen;

		if (prevBullish && close > ema && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownCandles;
		}
		else if (prevBearish && close < ema && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownCandles;
		}
		_prevOpen = candle.OpenPrice;
		_prevClose = close;
	}
}
