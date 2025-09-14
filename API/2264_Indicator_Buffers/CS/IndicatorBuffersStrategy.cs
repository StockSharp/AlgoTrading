using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates how to read multiple indicator buffers.
/// Uses Bollinger Bands and logs values of up to eight buffers.
/// </summary>
public class IndicatorBuffersStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the Bollinger Bands.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Width multiplier for the Bollinger Bands.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public IndicatorBuffersStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Bollinger Bands period", "Indicator");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bands Width", "Bollinger Bands width", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bands = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bands, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bands);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)indicatorValue;

		if (bb.MovingAverage is decimal middle)
			LogInfo($"Buffer0 = {middle}");
		else
			LogInfo("Buffer0 = n/a");

		if (bb.UpBand is decimal upper)
			LogInfo($"Buffer1 = {upper}");
		else
			LogInfo("Buffer1 = n/a");

		if (bb.LowBand is decimal lower)
			LogInfo($"Buffer2 = {lower}");
		else
			LogInfo("Buffer2 = n/a");

		LogInfo("Buffer3 = n/a");
		LogInfo("Buffer4 = n/a");
		LogInfo("Buffer5 = n/a");
		LogInfo("Buffer6 = n/a");
		LogInfo("Buffer7 = n/a");
	}
}
