using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FrakTrak XonaX strategy.
/// Uses fractal breakouts with fixed take profit and trailing stop.
/// </summary>
public class FraktrakXonaxStrategy : Strategy
{
	private const int FractalOffset = 15;

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingCorrection;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _upFractal;
	private decimal? _downFractal;
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _tickSize;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop in points.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Correction for trailing stop in points.
	/// </summary>
	public int TrailingCorrection
	{
		get => _trailingCorrection.Value;
		set => _trailingCorrection.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FraktrakXonaxStrategy"/>.
	/// </summary>
	public FraktrakXonaxStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop in points", "Risk");

		_trailingCorrection = Param(nameof(TrailingCorrection), 10)
			.SetDisplay("Trailing Correction", "Correction for trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_upFractal = _downFractal = _lastUpFractal = _lastDownFractal = null;
		_stopPrice = _takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Shift high and low buffers
		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;

		if (candle.State != CandleStates.Finished)
			return;

		// Detect new fractals
		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			_upFractal = _h3;
		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			_downFractal = _l3;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Entry signals
		if (_upFractal is decimal up && _lastUpFractal != up)
		{
			var trigger = up + FractalOffset * _tickSize;
			if (candle.ClosePrice > trigger && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = _downFractal;
				_takePrice = candle.ClosePrice + TakeProfit * _tickSize;
				_lastUpFractal = up;
			}
		}

		if (_downFractal is decimal low && _lastDownFractal != low)
		{
			var trigger = low - FractalOffset * _tickSize;
			if (candle.ClosePrice < trigger && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = _upFractal;
				_takePrice = candle.ClosePrice - TakeProfit * _tickSize;
				_lastDownFractal = low;
			}
		}

		// Manage position
		if (Position > 0)
		{
			if (_takePrice is decimal tp && candle.HighPrice >= tp)
				SellMarket(Position);

			if (_stopPrice is decimal sl && candle.LowPrice <= sl)
				SellMarket(Position);
			else if (TrailingStop > 0 && candle.ClosePrice - PositionAvgPrice > TrailingStop * _tickSize)
			{
				var newStop = candle.ClosePrice - TrailingStop * _tickSize - TrailingCorrection * _tickSize;
				if (_stopPrice is null || newStop > _stopPrice)
					_stopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			if (_takePrice is decimal tp && candle.LowPrice <= tp)
				BuyMarket(-Position);

			if (_stopPrice is decimal sl && candle.HighPrice >= sl)
				BuyMarket(-Position);
			else if (TrailingStop > 0 && PositionAvgPrice - candle.ClosePrice > TrailingStop * _tickSize)
			{
				var newStop = candle.ClosePrice + TrailingStop * _tickSize + TrailingCorrection * _tickSize;
				if (_stopPrice is null || newStop < _stopPrice)
					_stopPrice = newStop;
			}
		}
	}
}
