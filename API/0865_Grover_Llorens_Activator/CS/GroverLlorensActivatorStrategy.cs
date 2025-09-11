using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grover Llorens Activator strategy.
/// </summary>
public class GroverLlorensActivatorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _ts;
	private decimal _prevDiff;
	private decimal _val;
	private int _barsSince;
	private decimal _prevClose;
	private bool _initialized;

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public GroverLlorensActivatorStrategy()
	{
		_length = Param(nameof(Length), 480)
		.SetGreaterThanZero()
		.SetDisplay("Length", "ATR period length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(100, 1000, 10);

		_multiplier = Param(nameof(Multiplier), 14m)
		.SetGreaterThanZero()
		.SetDisplay("Multiplier", "ATR multiplier", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_ts = null;
		_prevDiff = 0m;
		_val = 0m;
		_barsSince = 0;
		_prevClose = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_ts = close;
			_prevClose = close;
			_val = atr / Length;
			_initialized = true;
			return;
		}

		var prevTs = _ts;
		var baseValue = prevTs ?? _prevClose;

		var diff = close - baseValue;
		var up = _prevDiff <= 0m && diff > 0m;
		var dn = _prevDiff >= 0m && diff < 0m;

		if (up || dn)
		{
			_val = atr / Length;
			_barsSince = 0;
		}
		else
		{
			_barsSince++;
		}

		var sign = diff > 0m ? 1m : diff < 0m ? -1m : 0m;
		var prevForCalc = prevTs ?? close;

		decimal newTs;
		if (up)
		newTs = prevForCalc - atr * Multiplier;
		else if (dn)
		newTs = prevForCalc + atr * Multiplier;
		else
		newTs = prevForCalc + sign * _val * _barsSince;

		if (up && Position <= 0)
		BuyMarket();
		else if (dn && Position >= 0)
		SellMarket();

		_ts = newTs;
		_prevDiff = diff;
		_prevClose = close;
	}
}
