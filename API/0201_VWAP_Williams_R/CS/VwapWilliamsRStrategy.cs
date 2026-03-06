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
/// Strategy based on VWAP and Williams %R indicators
/// </summary>
public class VwapWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// Store previous values
	private decimal _previousWilliamsR;
	private int _cooldown;
	private DateTime _vwapDate;
	private decimal _vwapCumPv;
	private decimal _vwapCumVol;

	/// <summary>
	/// Williams %R period
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	public VwapWilliamsRStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
			;

		_cooldownBars = Param(nameof(CooldownBars), 60)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
	
		_previousWilliamsR = default;
		_cooldown = 0;
		_vwapDate = default;
		_vwapCumPv = 0m;
		_vwapCumVol = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
	
		// Initialize indicator
		var williamsR = new WilliamsR { Length = WilliamsRPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(williamsR, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal williamsRValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var date = candle.ServerTime.Date;
		if (_vwapDate != date)
		{
			_vwapDate = date;
			_vwapCumPv = 0m;
			_vwapCumVol = 0m;
		}

		_vwapCumPv += candle.ClosePrice * candle.TotalVolume;
		_vwapCumVol += candle.TotalVolume;
		if (_vwapCumVol <= 0m)
			return;

		var vwapValue = _vwapCumPv / _vwapCumVol;

		// Store previous value to detect changes
		var previousWilliamsR = _previousWilliamsR;
		_previousWilliamsR = williamsRValue;

		var price = candle.ClosePrice;
		var crossedIntoOversold = previousWilliamsR > -80m && williamsRValue <= -80m;
		var crossedIntoOverbought = previousWilliamsR < -20m && williamsRValue >= -20m;

		if (_cooldown > 0)
			_cooldown--;

		if (_cooldown == 0 && price < vwapValue * 0.999m && crossedIntoOversold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = CooldownBars;
		}
		else if (_cooldown == 0 && price > vwapValue * 1.001m && crossedIntoOverbought && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = CooldownBars;
		}
	}
}
