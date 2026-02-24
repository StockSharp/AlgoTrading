using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uses the DeMarker oscillator to accumulate positions when extreme levels are reached.
/// </summary>
public class DeMarkerGainingPositionVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<DataType> _candleType;

	private DeMarker _deMarker;

	/// <summary>
	/// Number of candles used by the DeMarker indicator.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// DeMarker level that triggers short entries.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// DeMarker level that triggers long entries.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Candle type that defines the timeframe for the oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DeMarkerGainingPositionVolumeStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator");

		_upperLevel = Param(nameof(UpperLevel), 0.6m)
			.SetDisplay("Upper Level", "Threshold that prepares short exposure.", "Indicator");

		_lowerLevel = Param(nameof(LowerLevel), 0.4m)
			.SetDisplay("Lower Level", "Threshold that prepares long exposure.", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for DeMarker calculations.", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_deMarker = new DeMarker { Length = DeMarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_deMarker, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_deMarker.IsFormed)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// DeMarker below lower level => oversold => buy
		if (deMarkerValue <= LowerLevel)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		// DeMarker above upper level => overbought => sell
		else if (deMarkerValue >= UpperLevel)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}
	}
}
