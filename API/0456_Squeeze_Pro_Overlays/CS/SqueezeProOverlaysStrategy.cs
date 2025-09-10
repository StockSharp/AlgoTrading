using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Squeeze Pro Overlays Strategy - trades breakouts when volatility expands from a squeeze.
/// </summary>
public class SqueezeProOverlaysStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _squeezeLength;

	private BollingerBands _bollingerBands;
	private KeltnerChannels _keltnerLow;
	private KeltnerChannels _keltnerMid;
	private KeltnerChannels _keltnerHigh;
	private LinearRegression _momentum;

	private bool _wasSqueezed;
	private decimal _currentMomentum;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Calculation length for all indicators.
	/// </summary>
	public int SqueezeLength
	{
		get => _squeezeLength.Value;
		set => _squeezeLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SqueezeProOverlaysStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_squeezeLength = Param(nameof(SqueezeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Squeeze Length", "Calculation length", "Squeeze")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);
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

		_wasSqueezed = false;
		_currentMomentum = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollingerBands = new BollingerBands { Length = SqueezeLength, Width = 2m };
		_keltnerLow = new KeltnerChannels { Length = SqueezeLength, Multiplier = 2m };
		_keltnerMid = new KeltnerChannels { Length = SqueezeLength, Multiplier = 1.5m };
		_keltnerHigh = new KeltnerChannels { Length = SqueezeLength, Multiplier = 1m };
		_momentum = new LinearRegression { Length = SqueezeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bbValue = _bollingerBands.Process(candle);
		var kcLowValue = _keltnerLow.Process(candle);
		var kcMidValue = _keltnerMid.Process(candle);
		var kcHighValue = _keltnerHigh.Process(candle);

		if (!_bollingerBands.IsFormed || !_keltnerLow.IsFormed || !_keltnerMid.IsFormed || !_keltnerHigh.IsFormed)
			return;

		var bb = (BollingerBandsValue)bbValue;
		var kcLow = (KeltnerChannelsValue)kcLowValue;
		var kcMid = (KeltnerChannelsValue)kcMidValue;
		var kcHigh = (KeltnerChannelsValue)kcHighValue;

		if (bb.UpBand is not decimal bbUpper ||
			bb.LowBand is not decimal bbLower ||
			kcLow.Upper is not decimal lowUpper ||
			kcLow.Lower is not decimal lowLower ||
			kcMid.Upper is not decimal midUpper ||
			kcMid.Lower is not decimal midLower ||
			kcHigh.Upper is not decimal highUpper ||
			kcHigh.Lower is not decimal highLower ||
			!momentumValue.IsFinal)
		{
			return; // Not enough data
		}

		_currentMomentum = ((LinearRegressionValue)momentumValue).LinearRegSlope ?? 0m;

		var lowSqz = bbUpper < lowUpper && bbLower > lowLower;
		var midSqz = bbUpper < midUpper && bbLower > midLower;
		var highSqz = bbUpper < highUpper && bbLower > highLower;
		var anySqz = lowSqz || midSqz || highSqz;

		if (_wasSqueezed && !anySqz)
		{
			if (_currentMomentum > 0 && Position <= 0)
			{
				BuyMarket();
			}
			else if (_currentMomentum < 0 && Position >= 0)
			{
				SellMarket();
			}
		}

		_wasSqueezed = anySqz;
	}
}
