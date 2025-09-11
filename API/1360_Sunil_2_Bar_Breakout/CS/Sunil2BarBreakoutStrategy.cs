using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two-bar breakout strategy by Sunil.
/// Places stop orders based on breakout conditions.
/// </summary>
public class Sunil2BarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevHigh3;
	private decimal _prevLow1;
	private decimal _prevLow2;
	private decimal _prevLow3;
	private int _barCount;
	private decimal? _longStop;
	private decimal? _shortStop;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Sunil2BarBreakoutStrategy"/>.
	/// </summary>
	public Sunil2BarBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevClose = 0m;
		_prevHigh1 = 0m;
		_prevHigh2 = 0m;
		_prevHigh3 = 0m;
		_prevLow1 = 0m;
		_prevLow2 = 0m;
		_prevLow3 = 0m;
		_barCount = 0;
		_longStop = null;
		_shortStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_barCount < 3)
		{
			_barCount++;
			UpdateHistory(candle);
			return;
		}

		var longCond = candle.ClosePrice > _prevClose && _prevHigh1 > _prevHigh2 && _prevHigh1 > _prevHigh3;
		var shortCond = candle.ClosePrice < _prevClose && _prevLow1 < _prevLow2 && _prevLow1 < _prevLow3;

		if (longCond)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position == 0)
			{
				BuyStop(Volume, _prevClose);
				_longStop = _prevLow1;
			}
		}

		if (shortCond)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position == 0)
			{
				SellStop(Volume, _prevClose);
				_shortStop = _prevHigh1;
			}
		}

		if (Position > 0 && _longStop.HasValue && candle.LowPrice <= _longStop)
		{
			SellMarket(Math.Abs(Position));
			_longStop = null;
		}
		else if (Position < 0 && _shortStop.HasValue && candle.HighPrice >= _shortStop)
		{
			BuyMarket(Math.Abs(Position));
			_shortStop = null;
		}

		UpdateHistory(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_prevClose = candle.ClosePrice;
		_prevHigh3 = _prevHigh2;
		_prevHigh2 = _prevHigh1;
		_prevHigh1 = candle.HighPrice;
		_prevLow3 = _prevLow2;
		_prevLow2 = _prevLow1;
		_prevLow1 = candle.LowPrice;
	}
}
