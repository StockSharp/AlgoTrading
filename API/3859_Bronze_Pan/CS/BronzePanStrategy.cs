using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bronze Pan strategy - CCI with momentum confirmation.
/// Buys when CCI crosses above positive level and momentum is positive.
/// Sells when CCI crosses below negative level and momentum is negative.
/// </summary>
public class BronzePanStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _hasPrev;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciLevel { get => _cciLevel.Value; set => _cciLevel.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BronzePanStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI lookback", "Indicators");

		_cciLevel = Param(nameof(CciLevel), 100m)
			.SetDisplay("CCI Level", "CCI threshold level", "Levels");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevCci = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var mom = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, mom, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci, decimal mom)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevCci = cci;
			_hasPrev = true;
			return;
		}

		if (_prevCci <= CciLevel && cci > CciLevel && mom > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevCci >= -CciLevel && cci < -CciLevel && mom < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevCci = cci;
	}
}
