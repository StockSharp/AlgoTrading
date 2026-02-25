using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI breakout strategy. Opens long when CCI drops below lower band, shorts when above upper band.
/// </summary>
public class FtCciStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal UpperThreshold { get => _upperThreshold.Value; set => _upperThreshold.Value = value; }
	public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FtCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Averaging period for CCI", "Indicator");

		_upperThreshold = Param(nameof(UpperThreshold), 210m)
			.SetDisplay("CCI Upper", "CCI level for short entries", "Indicator");

		_lowerThreshold = Param(nameof(LowerThreshold), -210m)
			.SetDisplay("CCI Lower", "CCI level for long entries", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (cciValue <= LowerThreshold && Position <= 0)
			BuyMarket();
		else if (cciValue >= UpperThreshold && Position >= 0)
			SellMarket();

		// Exit when CCI returns to zero
		if (Position > 0 && cciValue >= 0)
			SellMarket();
		else if (Position < 0 && cciValue <= 0)
			BuyMarket();
	}
}
