using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens a position at a specified time and closes it at another time.
/// </summary>
public class OpeningClosingOnTimeStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _openTime;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _isBuy;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _positionOpened;
	
	/// <summary>
	/// Time of day to open the position.
	/// </summary>
	public TimeSpan OpenTime
	{
		get => _openTime.Value;
		set => _openTime.Value = value;
	}
	
	/// <summary>
	/// Time of day to close the position.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Direction of the initial trade.
	/// </summary>
	public bool IsBuy
	{
		get => _isBuy.Value;
		set => _isBuy.Value = value;
	}
	
	/// <summary>
	/// Candle type used for time tracking.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="OpeningClosingOnTimeStrategy"/>.
	/// </summary>
	public OpeningClosingOnTimeStrategy()
	{
		_openTime = Param(nameof(OpenTime), new TimeSpan(13, 0, 0))
		.SetDisplay("Open Time", "Time of day to open position", "General");
		_closeTime = Param(nameof(CloseTime), new TimeSpan(13, 1, 0))
		.SetDisplay("Close Time", "Time of day to close position", "General");
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "General");
		_isBuy = Param(nameof(IsBuy), true)
		.SetDisplay("Is Buy", "True for buy, false for sell", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_positionOpened = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_positionOpened = Position != 0;
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var t = candle.OpenTime.TimeOfDay;
		
		if (!_positionOpened && t.Hours == OpenTime.Hours && t.Minutes == OpenTime.Minutes)
		{
			if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
			if (IsBuy)
			BuyMarket(Volume);
			else
			SellMarket(Volume);
			
			_positionOpened = true;
			return;
		}
		
		if (_positionOpened && t.Hours == CloseTime.Hours && t.Minutes == CloseTime.Minutes)
		{
			ClosePosition();
			_positionOpened = false;
		}
	}
}
