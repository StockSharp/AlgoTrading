using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Fractal Adaptive Simple Moving Average (FRASMAv2).
/// Opens long when indicator changes from bullish to neutral or bearish.
/// Opens short when indicator changes from bearish to neutral or bullish.
/// </summary>
public class FrasmaV2Strategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private bool _isFirst;
	private decimal _prevFrama;
	private int _prevColor;

	/// <summary>
	/// FRAMA calculation period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FrasmaV2Strategy"/>.
	/// </summary>
	public FrasmaV2Strategy()
	{
		_period = Param(nameof(Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period", "FRAMA calculation period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");
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

		_isFirst = true;
		_prevFrama = 0;
		_prevColor = 1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fdi = new FractalDimension { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fdi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Point),
			new Unit(StopLoss, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fdiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fdiValue.IsFinal)
			return;

		var fdi = fdiValue.GetValue<decimal>();
		var alpha = (decimal)Math.Exp(-4.6 * ((double)fdi - 1.0));
		var price = candle.ClosePrice;
		var frama = _isFirst ? price : alpha * price + (1 - alpha) * _prevFrama;

		int color;
		if (_isFirst)
		{
			color = 1;
			_isFirst = false;
		}
		else if (frama > _prevFrama)
		{
			color = 0; // Uptrend
		}
		else if (frama < _prevFrama)
		{
			color = 2; // Downtrend
		}
		else
		{
			color = 1; // Flat
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFrama = frama;
			_prevColor = color;
			return;
		}

		if (_prevColor == 0 && color > 0)
		{
			if (Position < 0)
				ClosePosition();
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevColor == 2 && color < 2)
		{
			if (Position > 0)
				ClosePosition();
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevFrama = frama;
		_prevColor = color;
	}
}
