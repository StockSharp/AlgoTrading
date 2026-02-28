using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AOCCI Pivot strategy - CCI crossover with momentum filter.
/// Buys when CCI crosses above 0 with positive momentum.
/// Sells when CCI crosses below 0 with negative momentum.
/// </summary>
public class AocciPivotFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _hasPrev;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AocciPivotFilterStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI period", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

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

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal momValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevCci = cciValue;
			_hasPrev = true;
			return;
		}

		// CCI crosses above 0 with positive momentum
		if (_prevCci <= 0 && cciValue > 0 && momValue > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// CCI crosses below 0 with negative momentum
		else if (_prevCci >= 0 && cciValue < 0 && momValue < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevCci = cciValue;
	}
}
