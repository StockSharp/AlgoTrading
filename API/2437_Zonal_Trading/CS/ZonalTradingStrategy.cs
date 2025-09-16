using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zone trading strategy based on Bill Williams' Awesome and Accelerator Oscillators.
/// Buys when both oscillators turn green and sells when both turn red.
/// Positions are closed once an opposite color appears.
/// </summary>
public class ZonalTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _aoCandleType;
	private readonly StrategyParam<DataType> _acCandleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	
	private readonly AwesomeOscillator _aoMain = new() { ShortPeriod = 5, LongPeriod = 34 };
	private readonly AwesomeOscillator _aoAc = new() { ShortPeriod = 5, LongPeriod = 34 };
	private readonly SimpleMovingAverage _acMa = new() { Length = 5 };
	
	private decimal? _prevAo;
	private decimal? _prevAc;
	private int _aoTrend;
	private int _acTrend;
	
	/// <summary>
	/// Candle series used for Awesome Oscillator.
	/// </summary>
	public DataType AoCandleType { get => _aoCandleType.Value; set => _aoCandleType.Value = value; }
	
	/// <summary>
	/// Candle series used for Accelerator Oscillator.
	/// </summary>
	public DataType AcCandleType { get => _acCandleType.Value; set => _acCandleType.Value = value; }
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	
	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	
	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ZonalTradingStrategy"/> class.
	/// </summary>
	public ZonalTradingStrategy()
	{
		_aoCandleType = Param(nameof(AoCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("AO Candle Type", "Candle type for Awesome Oscillator", "General");
		
		_acCandleType = Param(nameof(AcCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("AC Candle Type", "Candle type for Accelerator Oscillator", "General");
		
		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Allow Long Entry", "Enable long position opening", "Trading");
		
		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Allow Short Entry", "Enable short position opening", "Trading");
		
		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");
		
		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, AoCandleType);
		if (AcCandleType != AoCandleType)
		yield return (Security, AcCandleType);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var aoSubscription = SubscribeCandles(AoCandleType);
		aoSubscription.BindEx(_aoMain, ProcessAo).Start();
		
		if (AcCandleType == AoCandleType)
		{
			aoSubscription.BindEx(_aoAc, ProcessAc).Start();
		}
		else
		{
			var acSubscription = SubscribeCandles(AcCandleType);
			acSubscription.BindEx(_aoAc, ProcessAc).Start();
		}
	}
	
	private void ProcessAo(ICandleMessage candle, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished || !aoValue.IsFinal)
		return;
		
		var ao = aoValue.GetValue<decimal>();
		if (_prevAo is null)
		{
			_prevAo = ao;
			return;
		}
		
		_aoTrend = ao > _prevAo ? 1 : ao < _prevAo ? -1 : _aoTrend;
		_prevAo = ao;
		
		TryTrade(candle.ServerTime);
	}
	
	private void ProcessAc(ICandleMessage candle, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished || !aoValue.IsFinal)
		return;
		
		var ao = aoValue.GetValue<decimal>();
		var aoSma = _acMa.Process(new DecimalIndicatorValue(_acMa, ao, candle.ServerTime));
		if (!aoSma.IsFormed)
		return;
		
		var ac = ao - aoSma.GetValue<decimal>();
		if (_prevAc is null)
		{
			_prevAc = ac;
			return;
		}
		
		_acTrend = ac > _prevAc ? 1 : ac < _prevAc ? -1 : _acTrend;
		_prevAc = ac;
		
		TryTrade(candle.ServerTime);
	}
	
	private void TryTrade(DateTimeOffset time)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (BuyClose && Position > 0 && (_aoTrend < 0 || _acTrend < 0))
		{
			SellMarket();
		}
		else if (SellClose && Position < 0 && (_aoTrend > 0 || _acTrend > 0))
		{
			BuyMarket();
		}
		
		if (BuyOpen && Position <= 0 && _aoTrend > 0 && _acTrend > 0)
		{
			BuyMarket();
		}
		else if (SellOpen && Position >= 0 && _aoTrend < 0 && _acTrend < 0)
		{
			SellMarket();
		}
	}
}
