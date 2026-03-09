using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RndTradeRandomHoldStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMom; private bool _hasPrev;
	private int _cooldown;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RndTradeRandomHoldStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 14).SetDisplay("EMA Period", "EMA filter", "Indicators");
		_momentumPeriod = Param(nameof(MomentumPeriod), 10).SetDisplay("Momentum", "Momentum period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMom = default;
		_hasPrev = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var mom = new Momentum { Length = MomentumPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, mom, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal mom)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!IsFormedAndOnlineAndAllowTrading()) return;
		var close = candle.ClosePrice;
		if (!_hasPrev) { _prevMom = mom; _hasPrev = true; return; }
		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMom = mom;
			return;
		}

		if (close > ema && _prevMom <= 0 && mom > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 2;
		}
		else if (close < ema && _prevMom >= 0 && mom < 0 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 2;
		}
		_prevMom = mom;
	}
}
