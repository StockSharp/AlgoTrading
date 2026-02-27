using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ang_Zad_C adaptive channel strategy.
/// Trades when the upper/lower lines cross, indicating trend reversal.
/// </summary>
public class AngZadCTimeMMRecoveryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _ki;

	private bool _hasState;
	private decimal _upperLine;
	private decimal _lowerLine;
	private decimal _previousPrice;
	private decimal? _prevUp;
	private decimal? _prevDn;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal Ki
	{
		get => _ki.Value;
		set => _ki.Value = value;
	}

	public AngZadCTimeMMRecoveryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_ki = Param(nameof(Ki), 4.000001m)
			.SetDisplay("Ki", "Smoothing coefficient", "Indicator")
			.SetGreaterThanZero();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasState = false;
		_prevUp = null;
		_prevDn = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var (upper, lower) = UpdateIndicator(price);

		if (_prevUp == null || _prevDn == null)
		{
			_prevUp = upper;
			_prevDn = lower;
			return;
		}

		var prevUp = _prevUp.Value;
		var prevDn = _prevDn.Value;

		// Buy signal: previous upper was below lower, now crossing above
		if (prevUp <= prevDn && upper > lower)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Sell signal: previous upper was above lower, now crossing below
		else if (prevUp >= prevDn && upper < lower)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevUp = upper;
		_prevDn = lower;
	}

	private (decimal Up, decimal Down) UpdateIndicator(decimal price)
	{
		if (!_hasState)
		{
			_upperLine = price;
			_lowerLine = price;
			_previousPrice = price;
			_hasState = true;
			return (_upperLine, _lowerLine);
		}

		var ki = Ki;

		if (price > _upperLine && price > _previousPrice)
			_upperLine += (price - _upperLine) / ki;

		if (price < _upperLine && price < _previousPrice)
			_upperLine += (price - _upperLine) / ki;

		if (price > _lowerLine && price < _previousPrice)
			_lowerLine += (price - _lowerLine) / ki;

		if (price < _lowerLine && price > _previousPrice)
			_lowerLine += (price - _lowerLine) / ki;

		_previousPrice = price;

		return (_upperLine, _lowerLine);
	}
}
