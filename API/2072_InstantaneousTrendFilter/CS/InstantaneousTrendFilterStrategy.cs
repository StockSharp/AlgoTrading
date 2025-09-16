namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class InstantaneousTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _alpha;

	private decimal _k0;
	private decimal _k1;
	private decimal _k2;
	private decimal _k3;
	private decimal _k4;

	private decimal _prevClose;
	private decimal _prevPrevClose;
	private decimal _itrendPrev1;
	private decimal _itrendPrev2;
	private decimal _triggerPrev;
	private int _bars;

	public InstantaneousTrendFilterStrategy()
	{
		_candleType = Param("Candle Type", TimeSpan.FromHours(4).TimeFrame());
		_alpha = Param("Alpha", 0.07m);
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal Alpha { get => _alpha.Value; set => _alpha.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var a2 = Alpha * Alpha;
		_k0 = Alpha - a2 / 4m;
		_k1 = 0.5m * a2;
		_k2 = Alpha - 0.75m * a2;
		_k3 = 2m * (1m - Alpha);
		_k4 = (1m - Alpha) * (1m - Alpha);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		decimal itrend;
		if (_bars < 2)
			itrend = close; // not enough data, use close price
		else if (_bars < 4)
			itrend = (close + 2m * _prevClose + _prevPrevClose) / 4m; // warm-up phase
		else
			itrend = _k0 * close + _k1 * _prevClose - _k2 * _prevPrevClose + _k3 * _itrendPrev1 - _k4 * _itrendPrev2; // main formula

		var trigger = 2m * itrend - _itrendPrev2; // trigger line

		var crossDown = _triggerPrev > _itrendPrev1 && trigger < itrend; // trigger crossed below trend
		var crossUp = _triggerPrev < _itrendPrev1 && trigger > itrend; // trigger crossed above trend

		if (crossDown)
		{
			if (Position < 0)
				BuyMarket(-Position); // close short position
			if (Position <= 0)
				BuyMarket(); // open long position
		}
		else if (crossUp)
		{
			if (Position > 0)
				SellMarket(Position); // close long position
			if (Position >= 0)
				SellMarket(); // open short position
		}

		_itrendPrev2 = _itrendPrev1;
		_itrendPrev1 = itrend;
		_triggerPrev = trigger;
		_prevPrevClose = _prevClose;
		_prevClose = close;
		_bars++;
	}
}
