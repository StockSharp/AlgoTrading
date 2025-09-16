using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX based trading system with trailing stop and fixed take profit/stop loss.
/// </summary>
public class AdxSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;

	private decimal _prevAdx;
	private decimal _prevPlusDi;
	private decimal _prevMinusDi;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
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
	/// Initializes a new instance of the <see cref="AdxSystemStrategy"/> class.
	/// </summary>
	public AdxSystemStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Distance for profit target", "Risk");

		_stopLoss = Param(nameof(StopLoss), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Distance for protective stop", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop", "Distance for trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_prevAdx = 0m;
		_prevPlusDi = 0m;
		_prevMinusDi = 0m;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_adx, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!adxValue.IsFinal)
		return;

		var adxVal = (AverageDirectionalIndexValue)adxValue;

		if (adxVal.MovingAverage is not decimal currentAdx)
		return;

		var dx = adxVal.Dx;
		if (dx.Plus is not decimal currentPlusDi || dx.Minus is not decimal currentMinusDi)
		return;

		if (_prevAdx != 0m)
		{
			if (Position == 0)
			{
				if (_prevAdx < currentAdx && _prevPlusDi < _prevAdx && currentPlusDi > currentAdx)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - StopLoss;
					_takePrice = _entryPrice + TakeProfit;
				}
				else if (_prevAdx < currentAdx && _prevMinusDi < _prevAdx && currentMinusDi > currentAdx)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + StopLoss;
					_takePrice = _entryPrice - TakeProfit;
				}
			}
			else if (Position > 0)
			{
				if (_prevAdx > currentAdx && _prevPlusDi > _prevAdx && currentPlusDi < currentAdx)
				{
					SellMarket(Position);
					_entryPrice = null;
					_stopPrice = null;
					_takePrice = null;
					return;
				}

				if (_entryPrice is decimal entry)
				{
					if (candle.ClosePrice - entry > TrailingStop)
					{
						var newStop = candle.ClosePrice - TrailingStop;
						if (_stopPrice is decimal stop && newStop > stop)
						_stopPrice = newStop;
					}

					if (_takePrice is decimal take && candle.HighPrice >= take)
					{
						SellMarket(Position);
						_entryPrice = null;
						_stopPrice = null;
						_takePrice = null;
					}
					else if (_stopPrice is decimal s && candle.LowPrice <= s)
					{
						SellMarket(Position);
						_entryPrice = null;
						_stopPrice = null;
						_takePrice = null;
					}
				}
			}
			else if (Position < 0)
			{
				if (_prevAdx > currentAdx && _prevMinusDi > _prevAdx && currentMinusDi < currentAdx)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = null;
					_stopPrice = null;
					_takePrice = null;
					return;
				}

				if (_entryPrice is decimal entry)
				{
					if (entry - candle.ClosePrice > TrailingStop)
					{
						var newStop = candle.ClosePrice + TrailingStop;
						if (_stopPrice is decimal stop && newStop < stop)
						_stopPrice = newStop;
					}

					if (_takePrice is decimal take && candle.LowPrice <= take)
					{
						BuyMarket(Math.Abs(Position));
						_entryPrice = null;
						_stopPrice = null;
						_takePrice = null;
					}
					else if (_stopPrice is decimal s && candle.HighPrice >= s)
					{
						BuyMarket(Math.Abs(Position));
						_entryPrice = null;
						_stopPrice = null;
						_takePrice = null;
					}
				}
			}
		}

		_prevAdx = currentAdx;
		_prevPlusDi = currentPlusDi;
		_prevMinusDi = currentMinusDi;
	}
}

