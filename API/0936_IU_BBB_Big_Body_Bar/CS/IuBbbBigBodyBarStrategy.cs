using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big body bar strategy with ATR trailing stop.
/// </summary>
public class IuBbbBigBodyBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _bigBodyThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrFactor;

	private SimpleMovingAverage _bodySma;
	private decimal? _atrStop;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Big body threshold multiplier.
	/// </summary>
	public decimal BigBodyThreshold { get => _bigBodyThreshold.Value; set => _bigBodyThreshold.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR factor for trailing stop.
	/// </summary>
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="IuBbbBigBodyBarStrategy"/>.
	/// </summary>
	public IuBbbBigBodyBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bigBodyThreshold = Param(nameof(BigBodyThreshold), 4m)
			.SetDisplay("Big Body Threshold", "Multiplier of average body", "Parameters")
			
			.SetOptimize(2m, 6m, 1m);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Period", "ATR indicator period", "Indicators")
			
			.SetOptimize(7, 28, 7);

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetDisplay("ATR Factor", "ATR multiplier for trailing stop", "Risk Management")
			
			.SetOptimize(1m, 3m, 0.5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bodySma?.Reset();
		_atrStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bodySma = new SMA { Length = 20 };

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var avgBody = _bodySma.Process(new DecimalIndicatorValue(_bodySma, body, candle.ServerTime)).ToDecimal();

		if (!_bodySma.IsFormed)
			return;

		var longCond = body > avgBody * BigBodyThreshold && candle.ClosePrice > candle.OpenPrice;
		var shortCond = body > avgBody * BigBodyThreshold && candle.ClosePrice < candle.OpenPrice;

		if (Position == 0)
		{
			if (longCond)
				BuyMarket(Volume);
			else if (shortCond)
				SellMarket(Volume);
		}

		if (Position > 0)
		{
			if (_atrStop is null)
				_atrStop = PositionPrice - atr * AtrFactor;
			else
				_atrStop = Math.Max(_atrStop.Value, candle.ClosePrice - atr * AtrFactor);

			if (candle.LowPrice <= _atrStop)
			{
				SellMarket(Position);
				_atrStop = null;
			}
		}
		else if (Position < 0)
		{
			if (_atrStop is null)
				_atrStop = PositionPrice + atr * AtrFactor;
			else
				_atrStop = Math.Min(_atrStop.Value, candle.ClosePrice + atr * AtrFactor);

			if (candle.HighPrice >= _atrStop)
			{
				BuyMarket(Math.Abs(Position));
				_atrStop = null;
			}
		}
		else
		{
			_atrStop = null;
		}
	}
}

