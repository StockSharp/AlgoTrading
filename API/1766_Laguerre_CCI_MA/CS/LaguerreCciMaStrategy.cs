using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining CCI crossover with EMA trend filter.
/// </summary>
public class LaguerreCciMaStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _hasPrev;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LaguerreCciMaStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var ma = new ExponentialMovingAverage { Length = MaPeriod };

		SubscribeCandles(CandleType)
			.Bind(cci, ma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevCci = cciValue;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: CCI crosses above 0 and price above MA
		if (_prevCci <= 0 && cciValue > 0 && close > maValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell: CCI crosses below 0 and price below MA
		else if (_prevCci >= 0 && cciValue < 0 && close < maValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevCci = cciValue;
	}
}
