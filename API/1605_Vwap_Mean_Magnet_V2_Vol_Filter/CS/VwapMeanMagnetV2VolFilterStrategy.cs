using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP mean reversion with RSI filter.
/// </summary>
public class VwapMeanMagnetV2VolFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _vwapLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	public int VwapLength { get => _vwapLength.Value; set => _vwapLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapMeanMagnetV2VolFilterStrategy()
	{
		_vwapLength = Param(nameof(VwapLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("VWAP Length", "VWAP Length", "General");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI Length", "General");
		_rsiOverbought = Param(nameof(RsiOverbought), 65)
			.SetDisplay("RSI Overbought", "RSI Overbought", "General");
		_rsiOversold = Param(nameof(RsiOversold), 35)
			.SetDisplay("RSI Oversold", "RSI Oversold", "General");
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

		var vwap = new VolumeWeightedMovingAverage { Length = VwapLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwap, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Mean reversion: buy below VWAP with oversold RSI, sell above VWAP with overbought RSI
		if (candle.ClosePrice < vwapValue && rsiValue < RsiOversold && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice > vwapValue && rsiValue > RsiOverbought && Position >= 0)
			SellMarket();

		// Exit on VWAP reversion
		if (Position > 0 && candle.ClosePrice >= vwapValue)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice <= vwapValue)
			BuyMarket();
	}
}
