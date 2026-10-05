using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR CCI strategy.
/// A close above the Parabolic SAR with CCI below CciOversold buys the dip in the uptrend and a close below the SAR with CCI above
/// CciOverbought sells the rally in the downtrend,
/// reversing an opposite position. The SAR is the trailing stop: a long closes when price closes below it and a short when price closes above it.
/// </summary>
public class ParabolicSarCciStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAccelerationFactor;
	private readonly StrategyParam<decimal> _sarMaxAccelerationFactor;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initial acceleration factor of the SAR.
	/// </summary>
	public decimal SarAccelerationFactor
	{
		get => _sarAccelerationFactor.Value;
		set => _sarAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of the SAR.
	/// </summary>
	public decimal SarMaxAccelerationFactor
	{
		get => _sarMaxAccelerationFactor.Value;
		set => _sarMaxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Period of CCI.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI level for longs.
	/// </summary>
	public decimal CciOversold
	{
		get => _cciOversold.Value;
		set => _cciOversold.Value = value;
	}

	/// <summary>
	/// CCI level for shorts.
	/// </summary>
	public decimal CciOverbought
	{
		get => _cciOverbought.Value;
		set => _cciOverbought.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ParabolicSarCciStrategy()
	{
		_sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR");

		_sarMaxAccelerationFactor = Param(nameof(SarMaxAccelerationFactor), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI", "CCI");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI level for longs", "CCI");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI level for shorts", "CCI");

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

		var sar = new ParabolicSar
		{
			Acceleration = SarAccelerationFactor,
			AccelerationMax = SarMaxAccelerationFactor,
		};
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The first SAR value is formed but empty.
		if (!sarValue.IsFormed || sarValue.IsEmpty || !cciValue.IsFormed)
			return;

		var sar = sarValue.GetValue<decimal>();
		var cci = cciValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (close > sar && cci < CciOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < sar && cci > CciOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < sar)
			SellMarket(Position);
		else if (Position < 0 && close > sar)
			BuyMarket(-Position);
	}
}
