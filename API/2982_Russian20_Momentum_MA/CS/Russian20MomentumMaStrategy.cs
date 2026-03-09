using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Russian20 Momentum MA strategy. Combines SMA filter with momentum confirmation.
/// </summary>
public class Russian20MomentumMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _momPeriod;

	private decimal? _prevMom;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int MomPeriod
	{
		get => _momPeriod.Value;
		set => _momPeriod.Value = value;
	}

	public Russian20MomentumMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA period for trend filter", "Indicators");

		_momPeriod = Param(nameof(MomPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMom = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMom = null;

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var mom = new Momentum { Length = MomPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, mom, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maVal, decimal momVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMom = momVal;
			return;
		}

		if (_prevMom == null)
		{
			_prevMom = momVal;
			return;
		}

		var close = candle.ClosePrice;

		// Price above MA + momentum crosses above zero → buy
		if (close > maVal && _prevMom.Value <= 100m && momVal > 100m && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Price below MA + momentum crosses below zero → sell
		else if (close < maVal && _prevMom.Value >= 100m && momVal < 100m && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMom = momVal;
	}
}
