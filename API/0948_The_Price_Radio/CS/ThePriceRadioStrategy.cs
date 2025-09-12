using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// John Ehlers' Price Radio strategy.
/// Uses derivative-based amplitude and frequency thresholds to trade.
/// </summary>
public class ThePriceRadioStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _envelope = null!;
	private SimpleMovingAverage _amSma = null!;
	private Highest _derivHigh = null!;
	private Lowest _derivLow = null!;
	private SimpleMovingAverage _fmSma = null!;
	private decimal _prevClose;

	/// <summary>
	/// Lookback period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Initializes a new instance of the <see cref="ThePriceRadioStrategy"/> class.
	/// </summary>
	public ThePriceRadioStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback period", "General")
			.SetCanOptimize(true);

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

		_prevClose = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_envelope = new Highest { Length = 4 };
		_amSma = new SimpleMovingAverage { Length = Length };
		_derivHigh = new Highest { Length = Length };
		_derivLow = new Lowest { Length = Length };
		_fmSma = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _amSma);
			DrawIndicator(area, _fmSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var deriv = candle.ClosePrice - _prevClose;
		_prevClose = candle.ClosePrice;

		var envelope = _envelope.Process(Math.Abs(deriv), candle.OpenTime, true).ToDecimal();
		var am = _amSma.Process(envelope, candle.OpenTime, true).ToDecimal();

		var high = _derivHigh.Process(deriv, candle.OpenTime, true).ToDecimal();
		var low = _derivLow.Process(deriv, candle.OpenTime, true).ToDecimal();

		var clamped = Math.Min(Math.Max(10m * deriv, low), high);
		var fm = _fmSma.Process(clamped, candle.OpenTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && deriv > am && deriv > fm)
			BuyMarket();
		else if (Position >= 0 && deriv < -am && deriv < -fm)
			SellMarket();
	}
}
