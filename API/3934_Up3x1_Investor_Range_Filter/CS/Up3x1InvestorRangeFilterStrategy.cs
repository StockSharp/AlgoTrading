using System;



using StockSharp.Algo.Indicators;

using StockSharp.Algo.Strategies;

using StockSharp.BusinessEntities;

using StockSharp.Messages;



namespace StockSharp.Samples.Strategies;



public class Up3x1InvestorRangeFilterStrategy : Strategy

{

	private readonly StrategyParam<int> _emaPeriod;

	private readonly StrategyParam<int> _atrPeriod;

	private readonly StrategyParam<DataType> _candleType;



	private decimal _prevClose; private decimal _prevEma; private bool _hasPrev;

	private int _cooldown;



	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }



	public Up3x1InvestorRangeFilterStrategy()

	{

		_emaPeriod = Param(nameof(EmaPeriod), 14).SetDisplay("EMA Period", "EMA lookback", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period", "ATR lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");

	}



	/// <inheritdoc />

	protected override void OnReseted()

	{

		base.OnReseted();

		_prevClose = default;

		_prevEma = default;

		_hasPrev = default;

		_cooldown = default;

	}



	/// <inheritdoc />

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)

	{

		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ema, ProcessCandle).Start();

	}



	private void ProcessCandle(ICandleMessage candle, decimal ema)

	{

		if (candle.State != CandleStates.Finished) return;

		if (!IsFormedAndOnlineAndAllowTrading()) return;

		var close = candle.ClosePrice;

		if (!_hasPrev) { _prevClose = close; _prevEma = ema; _hasPrev = true; return; }

		if (_cooldown > 0)

		{

			_cooldown--;

			_prevClose = close; _prevEma = ema;

			return;

		}



		if (_prevClose <= _prevEma && close > ema && Position <= 0)

		{

			var volume = Volume + Math.Abs(Position);

			BuyMarket(volume);

			_cooldown = 6;

		}

		else if (_prevClose >= _prevEma && close < ema && Position >= 0)

		{

			var volume = Volume + Math.Abs(Position);

			SellMarket(volume);

			_cooldown = 6;

		}

		_prevClose = close; _prevEma = ema;

	}

}

