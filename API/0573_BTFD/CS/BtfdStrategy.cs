using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTFD Strategy - buys when volume spikes and RSI is oversold, then exits in five profit targets with stop loss.
/// </summary>
public class BtfdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _tp1;
	private readonly StrategyParam<decimal> _tp2;
	private readonly StrategyParam<decimal> _tp3;
	private readonly StrategyParam<decimal> _tp4;
	private readonly StrategyParam<decimal> _tp5;
	private readonly StrategyParam<int> _q1;
	private readonly StrategyParam<int> _q2;
	private readonly StrategyParam<int> _q3;
	private readonly StrategyParam<int> _q4;
	private readonly StrategyParam<int> _q5;
	private readonly StrategyParam<decimal> _stopLossPercent;
	
	private SimpleMovingAverage _volumeSma;
	private RelativeStrengthIndex _rsi;
	
	private decimal _entryPrice;
	private decimal _initialVolume;
	private int _soldPercent;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }
	
	/// <summary>
	/// Volume spike multiplier.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	
	/// <summary>
	/// Take profit 1 percent.
	/// </summary>
	public decimal Tp1 { get => _tp1.Value; set => _tp1.Value = value; }
	
	/// <summary>
	/// Take profit 2 percent.
	/// </summary>
	public decimal Tp2 { get => _tp2.Value; set => _tp2.Value = value; }
	
	/// <summary>
	/// Take profit 3 percent.
	/// </summary>
	public decimal Tp3 { get => _tp3.Value; set => _tp3.Value = value; }
	
	/// <summary>
	/// Take profit 4 percent.
	/// </summary>
	public decimal Tp4 { get => _tp4.Value; set => _tp4.Value = value; }
	
	/// <summary>
	/// Take profit 5 percent.
	/// </summary>
	public decimal Tp5 { get => _tp5.Value; set => _tp5.Value = value; }
	
	/// <summary>
	/// Percent to exit at TP1.
	/// </summary>
	public int Q1 { get => _q1.Value; set => _q1.Value = value; }
	
	/// <summary>
	/// Percent to exit at TP2.
	/// </summary>
	public int Q2 { get => _q2.Value; set => _q2.Value = value; }
	
	/// <summary>
	/// Percent to exit at TP3.
	/// </summary>
	public int Q3 { get => _q3.Value; set => _q3.Value = value; }
	
	/// <summary>
	/// Percent to exit at TP4.
	/// </summary>
	public int Q4 { get => _q4.Value; set => _q4.Value = value; }
	
	/// <summary>
	/// Percent to exit at TP5.
	/// </summary>
	public int Q5 { get => _q5.Value; set => _q5.Value = value; }
	
	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public BtfdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(3).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_volumeLength = Param(nameof(VolumeLength), 70)
		.SetGreaterThanZero()
		.SetDisplay("Volume Length", "SMA length for volume", "Volume");
		
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.5m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Mult", "Volume spike multiplier", "Volume");
		
		_rsiLength = Param(nameof(RsiLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "RSI");
		
		_rsiOversold = Param(nameof(RsiOversold), 30m)
		.SetGreaterThanZero()
		.SetDisplay("RSI Oversold", "Oversold threshold", "RSI");
		
		_tp1 = Param(nameof(Tp1), 0.4m)
		.SetGreaterThanZero()
		.SetDisplay("TP1 %", "First take profit percent", "Targets");
		
		_tp2 = Param(nameof(Tp2), 0.6m)
		.SetGreaterThanZero()
		.SetDisplay("TP2 %", "Second take profit percent", "Targets");
		
		_tp3 = Param(nameof(Tp3), 0.8m)
		.SetGreaterThanZero()
		.SetDisplay("TP3 %", "Third take profit percent", "Targets");
		
		_tp4 = Param(nameof(Tp4), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("TP4 %", "Fourth take profit percent", "Targets");
		
		_tp5 = Param(nameof(Tp5), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("TP5 %", "Fifth take profit percent", "Targets");
		
		_q1 = Param(nameof(Q1), 20)
		.SetGreaterThanZero()
		.SetDisplay("TP1 Qty %", "Percent to exit at TP1", "Targets");
		
		_q2 = Param(nameof(Q2), 40)
		.SetGreaterThanZero()
		.SetDisplay("TP2 Qty %", "Percent to exit at TP2", "Targets");
		
		_q3 = Param(nameof(Q3), 60)
		.SetGreaterThanZero()
		.SetDisplay("TP3 Qty %", "Percent to exit at TP3", "Targets");
		
		_q4 = Param(nameof(Q4), 80)
		.SetGreaterThanZero()
		.SetDisplay("TP4 Qty %", "Percent to exit at TP4", "Targets");
		
		_q5 = Param(nameof(Q5), 100)
		.SetGreaterThanZero()
		.SetDisplay("TP5 Qty %", "Percent to exit at TP5", "Targets");
		
		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
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
		_entryPrice = 0m;
		_initialVolume = 0m;
		_soldPercent = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var volValue = _volumeSma.Process(candle.TotalVolume);
		if (!_volumeSma.IsFormed || volValue.ToNullableDecimal() is not decimal volAvg)
		return;
		
		var volumeCond = candle.TotalVolume > volAvg * VolumeMultiplier;
		var rsiCond = rsiValue <= RsiOversold;
		
		if (Position <= 0 && volumeCond && rsiCond)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_initialVolume = Volume;
			_soldPercent = 0;
			return;
		}
		
		if (Position <= 0)
		return;
		
		var price = candle.ClosePrice;
		var slPrice = _entryPrice * (1m - StopLossPercent / 100m);
		if (price <= slPrice)
		{
			RegisterSell(Position);
			_soldPercent = 100;
			return;
		}
		
		CheckTakeProfits(price);
	}
	
	private void CheckTakeProfits(decimal price)
	{
		var levels = new (decimal tp, int qty)[]
		{
			(Tp1, Q1),
			(Tp2, Q2),
			(Tp3, Q3),
			(Tp4, Q4),
			(Tp5, Q5)
		};
		
		foreach (var (tp, qty) in levels)
		{
			if (_soldPercent >= qty)
			continue;
			
			var tpPrice = _entryPrice * (1m + tp / 100m);
			if (price >= tpPrice)
			{
				var part = qty - _soldPercent;
				var volumeToSell = _initialVolume * part / 100m;
				RegisterSell(volumeToSell);
				_soldPercent = qty;
			}
		}
	}
}

