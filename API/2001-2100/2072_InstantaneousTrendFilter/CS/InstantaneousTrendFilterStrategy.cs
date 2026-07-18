using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Instantaneous Trend Filter strategy.
/// Uses a custom digital filter formula to detect trend changes.
/// </summary>
public class InstantaneousTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _alpha;

	private decimal _k0, _k1, _k2, _k3, _k4;
	private decimal _prevClose;
	private decimal _prevPrevClose;
	private decimal _itrendPrev1;
	private decimal _itrendPrev2;
	private decimal _triggerPrev;
	private int _bars;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal Alpha { get => _alpha.Value; set => _alpha.Value = value; }

	public InstantaneousTrendFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
		_alpha = Param(nameof(Alpha), 0.07m)
			.SetDisplay("Alpha", "Filter smoothing coefficient", "Indicator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bars = default;
		_prevClose = default;
		_prevPrevClose = default;
		_itrendPrev1 = default;
		_itrendPrev2 = default;
		_triggerPrev = default;
		_k0 = default;
		_k1 = default;
		_k2 = default;
		_k3 = default;
		_k4 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bars = 0;
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
			itrend = close;
		else if (_bars < 4)
			itrend = (close + 2m * _prevClose + _prevPrevClose) / 4m;
		else
			itrend = _k0 * close + _k1 * _prevClose - _k2 * _prevPrevClose + _k3 * _itrendPrev1 - _k4 * _itrendPrev2;

		var trigger = 2m * itrend - _itrendPrev2;

		var crossDown = _triggerPrev > _itrendPrev1 && trigger < itrend;
		var crossUp = _triggerPrev < _itrendPrev1 && trigger > itrend;

		if (crossDown && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossUp && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_itrendPrev2 = _itrendPrev1;
		_itrendPrev1 = itrend;
		_triggerPrev = trigger;
		_prevPrevClose = _prevClose;
		_prevClose = close;
		_bars++;
	}
}
