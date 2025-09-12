namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid like strategy using martingale or anti-martingale position sizing.
/// </summary>
public class GridLikeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _point;
	private readonly StrategyParam<decimal> _orderSize;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _antiMartingale;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _baseline;
	private decimal _upper;
	private decimal _lower;
	private decimal _size;

	public GridLikeStrategy()
	{
		_point = Param(nameof(Point), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Point", "Distance from baseline to grid levels", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_orderSize = Param(nameof(OrderSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Size", "Base order size", "General");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Martingale multiplier", "General");

		_antiMartingale = Param(nameof(AntiMartingale), false)
			.SetDisplay("Anti Martingale", "Increase size after wins", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public decimal Point
	{
		get => _point.Value;
		set => _point.Value = value;
	}

	public decimal OrderSize
	{
		get => _orderSize.Value;
		set => _orderSize.Value = value;
	}

	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	public bool AntiMartingale
	{
		get => _antiMartingale.Value;
		set => _antiMartingale.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_baseline = null;
		_upper = 0m;
		_lower = 0m;
		_size = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void EnsureSize()
	{
		if (_size == 0m)
			_size = OrderSize;
	}

	private void OnTradeClosed(bool win)
	{
		if (AntiMartingale)
			_size = win ? _size * Multiplier : OrderSize;
		else
			_size = win ? OrderSize : _size * Multiplier;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_baseline is null)
		{
			_baseline = candle.ClosePrice;
			_upper = _baseline.Value + Point;
			_lower = _baseline.Value - Point;
			return;
		}

		// exit logic
		if (Position > 0)
		{
			if (candle.LowPrice <= _lower)
			{
				SellMarket(Position);
				OnTradeClosed(false);
			}
			else if (candle.HighPrice >= _upper)
			{
				SellMarket(Position);
				OnTradeClosed(true);
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _upper)
			{
				BuyMarket(-Position);
				OnTradeClosed(false);
			}
			else if (candle.LowPrice <= _lower)
			{
				BuyMarket(-Position);
				OnTradeClosed(true);
			}
		}

		var prevBaseline = _baseline.Value;

		if (candle.ClosePrice > prevBaseline + Point || candle.ClosePrice < prevBaseline - Point)
		{
			_baseline = candle.ClosePrice;
			_upper = _baseline.Value + Point;
			_lower = _baseline.Value - Point;
		}

		if (Position == 0)
		{
			if (_baseline.Value > prevBaseline)
			{
				EnsureSize();
				BuyMarket(_size);
			}
			else if (_baseline.Value < prevBaseline)
			{
				EnsureSize();
				SellMarket(_size);
			}
		}
	}
}
