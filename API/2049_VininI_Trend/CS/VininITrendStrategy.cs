using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the VininI Trend concept using the CCI indicator.
/// Buys when CCI breaks above upper level, sells when CCI breaks below lower level.
/// </summary>
public class VininITrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public int UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public int DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VininITrendStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for the CCI indicator", "Parameters");

		_upLevel = Param(nameof(UpLevel), 10)
			.SetDisplay("Upper Level", "Upper threshold to trigger buy", "Parameters");

		_downLevel = Param(nameof(DownLevel), -10)
			.SetDisplay("Lower Level", "Lower threshold to trigger sell", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = _prev2 = null;

		var cci = new CommodityChannelIndex { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!cciValue.IsFormed)
			return;

		var cci = cciValue.ToDecimal();

		var buySignal = false;
		var sellSignal = false;

		if (_prev1 is not null)
		{
			// Breakdown mode: CCI crosses level
			if (_prev1 <= UpLevel && cci > UpLevel)
				buySignal = true;

			if (_prev1 >= DownLevel && cci < DownLevel)
				sellSignal = true;
		}

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = cci;
	}
}
