using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI and ADX based long/short strategy.
/// </summary>
public class RsiAdxLongShortStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// ADX threshold to confirm trend strength.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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
	/// Initializes a new instance of the <see cref="RsiAdxLongShortStrategy"/> class.
	/// </summary>
	public RsiAdxLongShortStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI calculation", "Indicators")
			.SetCanOptimize(true);

		_adxLength = Param(nameof(AdxLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true);

		_adxThreshold = Param(nameof(AdxThreshold), 14m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value to allow trades", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevRsi is null)
		{
			_prevRsi = rsiValue;
			return;
		}

		var prev = _prevRsi.Value;

		if (prev >= 30m && rsiValue < 30m && Position > 0)
			SellMarket(Math.Abs(Position));
		else if (prev <= 70m && rsiValue > 70m && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (prev <= 70m && rsiValue > 70m && adxValue > AdxThreshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (prev >= 30m && rsiValue < 30m && adxValue > AdxThreshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevRsi = rsiValue;
	}
}
