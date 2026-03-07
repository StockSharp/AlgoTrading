using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility-based trend reversal strategy simulating renko brick logic on regular candles.
/// Uses StandardDeviation instead of ATR for brick sizing.
/// </summary>
public class RenkoTrendReversalV2Strategy : Strategy
{
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<decimal> _brickMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _brickHigh;
	private decimal _brickLow;
	private bool _isUpTrend;
	private bool _hasBrick;

	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }
	public decimal BrickMultiplier { get => _brickMultiplier.Value; set => _brickMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RenkoTrendReversalV2Strategy()
	{
		_stdLength = Param(nameof(StdLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "StdDev period for brick size", "General");

		_brickMultiplier = Param(nameof(BrickMultiplier), 0.5m)
			.SetDisplay("Brick Multiplier", "Multiplier for brick size", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_hasBrick = false;
		_brickHigh = 0;
		_brickLow = 0;
		_isUpTrend = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = StdLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdValue <= 0)
			return;

		var brickSize = stdValue * BrickMultiplier;

		if (!_hasBrick)
		{
			_brickHigh = candle.ClosePrice + brickSize;
			_brickLow = candle.ClosePrice - brickSize;
			_isUpTrend = true;
			_hasBrick = true;
			return;
		}

		// Check for trend reversal via brick break
		if (candle.ClosePrice >= _brickHigh)
		{
			// Bullish brick formed
			if (!_isUpTrend)
			{
				// Reversal from down to up
				if (Position <= 0)
					BuyMarket();
			}

			_isUpTrend = true;
			_brickHigh = candle.ClosePrice + brickSize;
			_brickLow = candle.ClosePrice - brickSize;
		}
		else if (candle.ClosePrice <= _brickLow)
		{
			// Bearish brick formed
			if (_isUpTrend)
			{
				// Reversal from up to down
				if (Position >= 0)
					SellMarket();
			}

			_isUpTrend = false;
			_brickHigh = candle.ClosePrice + brickSize;
			_brickLow = candle.ClosePrice - brickSize;
		}
	}
}
