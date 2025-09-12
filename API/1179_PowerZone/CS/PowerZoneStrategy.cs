using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PowerZone breakout strategy.
/// Detects order block zones and trades breakouts with take profit and stop loss.
/// </summary>
public class PowerZoneStrategy : Strategy
{
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _useWicks;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<ICandleMessage> _candles = new();
	private bool _hasBullZone;
	private decimal _bullHigh;
	private decimal _bullLow;
	private decimal _bullTp;
	private decimal _bullSl;
	private bool _hasBearZone;
	private decimal _bearHigh;
	private decimal _bearLow;
	private decimal _bearTp;
	private decimal _bearSl;

	/// <summary>
	/// Number of candles to confirm the zone.
	/// </summary>
	public int Periods
	{
		get => _periods.Value;
		set => _periods.Value = value;
	}

	/// <summary>
	/// Minimum percentage move required.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Use high/low instead of open/close for zone borders.
	/// </summary>
	public bool UseWicks
	{
		get => _useWicks.Value;
		set => _useWicks.Value = value;
	}

	/// <summary>
	/// Multiplier for take profit distance.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Multiplier for stop loss distance.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PowerZoneStrategy"/>.
	/// </summary>
	public PowerZoneStrategy()
	{
		_periods = Param(nameof(Periods), 5)
			.SetDisplay("Periods", "Number of candles in PowerZone", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_threshold = Param(nameof(Threshold), 0m)
			.SetDisplay("Threshold", "Minimum move in percent", "General")
			.SetRange(0m, 10m)
			.SetCanOptimize(true);

		_useWicks = Param(nameof(UseWicks), false)
			.SetDisplay("Use Wicks", "Use full range high/low", "General");

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 1.5m)
			.SetDisplay("Take Profit Factor", "Multiplier for profit target", "Risk")
			.SetRange(0.5m, 3m)
			.SetCanOptimize(true);

		_stopLossFactor = Param(nameof(StopLossFactor), 1m)
			.SetDisplay("Stop Loss Factor", "Multiplier for stop loss", "Risk")
			.SetRange(0.5m, 3m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_candles.Clear();
		_hasBullZone = false;
		_hasBearZone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Enqueue(candle);
		while (_candles.Count > Periods + 2)
			_candles.Dequeue();

		if (_candles.Count < Periods + 2)
			return;

		var arr = _candles.ToArray();
		var prev = arr[^2];
		var ob = arr[^ (Periods + 2)];

		var move = Math.Abs((ob.ClosePrice - prev.ClosePrice) / ob.ClosePrice) * 100m;
		var relMove = move >= Threshold;

		var upCount = 0;
		var downCount = 0;
		for (var i = 1; i <= Periods; i++)
		{
			var bar = arr[^ (i + 1)];
			if (bar.ClosePrice > bar.OpenPrice)
				upCount++;
			if (bar.ClosePrice < bar.OpenPrice)
				downCount++;
		}

		var bullZone = ob.ClosePrice < ob.OpenPrice && upCount == Periods && relMove;
		if (bullZone)
		{
			_bullHigh = UseWicks ? ob.HighPrice : ob.OpenPrice;
			_bullLow = ob.LowPrice;
			_hasBullZone = true;
		}

		var bearZone = ob.ClosePrice > ob.OpenPrice && downCount == Periods && relMove;
		if (bearZone)
		{
			_bearHigh = ob.HighPrice;
			_bearLow = UseWicks ? ob.LowPrice : ob.OpenPrice;
			_hasBearZone = true;
		}

		if (_hasBullZone && candle.ClosePrice > _bullHigh && Position <= 0)
		{
			var range = _bullHigh - _bullLow;
			_bullTp = candle.ClosePrice + range * TakeProfitFactor;
			_bullSl = _bullLow - range * StopLossFactor;
			BuyMarket(Volume + Math.Abs(Position));
			_hasBullZone = false;
		}
		else if (_hasBearZone && candle.ClosePrice < _bearLow && Position >= 0)
		{
			var range = _bearHigh - _bearLow;
			_bearTp = candle.ClosePrice - range * TakeProfitFactor;
			_bearSl = _bearHigh + range * StopLossFactor;
			SellMarket(Volume + Math.Abs(Position));
			_hasBearZone = false;
		}

		if (Position > 0)
		{
			if (candle.HighPrice >= _bullTp || candle.LowPrice <= _bullSl)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _bearTp || candle.HighPrice >= _bearSl)
				BuyMarket(-Position);
		}
	}
}
