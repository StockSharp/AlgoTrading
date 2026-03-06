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
/// Strategy that uses CCI and VWAP indicators to identify oversold and overbought conditions.
/// Enters long when CCI is below -100 and price is below VWAP.
/// Enters short when CCI is above 100 and price is above VWAP.
/// </summary>
public class CciVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private CommodityChannelIndex _cci;
	private int _cooldown;
	private DateTime _vwapDate;
	private decimal _vwapCumPv;
	private decimal _vwapCumVol;
	
	/// <summary>
	/// CCI period parameter.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
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
	/// Stop-loss percentage parameter.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public CciVwapStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI period", "CCI indicator period", "Indicators")
			
			.SetOptimize(10, 30, 5);

		_cooldownBars = Param(nameof(CooldownBars), 60)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");
			
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
			
			.SetOptimize(1m, 3m, 0.5m);
			
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Type of candles to use", "General");
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

		_cci = null;
		_cooldown = 0;
		_vwapDate = default;
		_vwapCumPv = 0m;
		_vwapCumVol = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Initialize CCI indicator
		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		// Bind CCI to candle subscription
		var candlesSubscription = SubscribeCandles(CandleType)
			.Bind(_cci, ProcessCandle)
			.Start();
		
		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candlesSubscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal cci)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
			
		// Skip if strategy is not ready to trade
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

		var currentVwap = _vwapCumPv / _vwapCumVol;
		if (currentVwap == 0)
			return;

		if (_cooldown > 0)
			_cooldown--;
			
		// Long signal: CCI below -100 and price below VWAP
		if (_cooldown == 0 && cci < -150 && candle.ClosePrice < currentVwap * 0.998m && Position <= 0)
		{
			BuyMarket(Volume);
			_cooldown = CooldownBars;
		}
		// Short signal: CCI above 100 and price above VWAP
		else if (_cooldown == 0 && cci > 150 && candle.ClosePrice > currentVwap * 1.002m && Position >= 0)
		{
			SellMarket(Volume);
			_cooldown = CooldownBars;
		}
		// Exit long position: Price crosses above VWAP
		else if (Position > 0 && candle.ClosePrice > currentVwap)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		// Exit short position: Price crosses below VWAP
		else if (Position < 0 && candle.ClosePrice < currentVwap)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
	
