using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Donchian Channels and CCI indicators
/// </summary>
public class DonchianCciStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period for Donchian Channel
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Period for CCI indicator
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public DonchianCciStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
			;

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
			;

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	private int _cooldown;

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldown = 0;
	}


	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Initialize Indicators
		var donchian = new DonchianChannels { Length = DonchianPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, cci, ProcessIndicators)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue cciValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		var donchianTyped = (DonchianChannelsValue)donchianValue;
		var upperBand = donchianTyped.UpperBand ?? 0m;
		var lowerBand = donchianTyped.LowerBand ?? 0m;
		var middleBand = donchianTyped.Middle ?? 0m;
		if (upperBand == 0m || lowerBand == 0m)
			return;

		var cciDec = cciValue.ToDecimal();

		var price = candle.ClosePrice;

		if (_cooldown > 0)
			_cooldown--;

		if (_cooldown == 0 && price >= upperBand && cciDec > 0 && Position <= 0)
		{
			BuyMarket();
			_cooldown = 50;
		}
		else if (_cooldown == 0 && price <= lowerBand && cciDec < 0 && Position >= 0)
		{
			SellMarket();
			_cooldown = 50;
		}
		else if (Position > 0 && price < middleBand)
		{
			SellMarket();
			_cooldown = 50;
		}
		else if (Position < 0 && price > middleBand)
		{
			BuyMarket();
			_cooldown = 50;
		}
	}
}
