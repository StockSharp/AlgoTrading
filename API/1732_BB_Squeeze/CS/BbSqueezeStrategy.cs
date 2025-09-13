using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands squeeze breakout strategy.
/// Detects periods of low volatility when bands contract and enters on breakout.
/// </summary>
public class BbSqueezeStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _squeezeThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousBandWidth;
	private bool _isFirstValue = true;
	private bool _isInSqueeze;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Band width threshold to detect squeeze.
	/// </summary>
	public decimal SqueezeThreshold
	{
		get => _squeezeThreshold.Value;
		set => _squeezeThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BbSqueezeStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of Bollinger Bands", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_squeezeThreshold = Param(nameof(SqueezeThreshold), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Squeeze Threshold", "Relative band width threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");
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

		_previousBandWidth = 0m;
		_isFirstValue = true;
		_isInSqueeze = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = 2m
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bandWidth = (upper - lower) / middle;

		if (_isFirstValue)
		{
			_previousBandWidth = bandWidth;
			_isFirstValue = false;
			return;
		}

		var isSqueeze = bandWidth < SqueezeThreshold;

		if (_isInSqueeze && !isSqueeze && bandWidth > _previousBandWidth)
		{
			if (candle.ClosePrice > upper && Position <= 0)
			{
				BuyMarket();
			}
			else if (candle.ClosePrice < lower && Position >= 0)
			{
				SellMarket();
			}
		}

		_isInSqueeze = isSqueeze;
		_previousBandWidth = bandWidth;
	}
}
