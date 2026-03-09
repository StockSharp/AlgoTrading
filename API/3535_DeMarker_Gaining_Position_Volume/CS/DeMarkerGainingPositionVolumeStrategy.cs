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

	private RelativeStrengthIndex _rsi;
	private decimal? _prevOscillator;

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

		_upperLevel = Param(nameof(UpperLevel), 0.7m)
			.SetDisplay("Upper Level", "Threshold that prepares short exposure.", "Indicator");

		_lowerLevel = Param(nameof(LowerLevel), 0.3m)
			.SetDisplay("Lower Level", "Threshold that prepares long exposure.", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for DeMarker calculations.", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevOscillator = null;
		_rsi = new RelativeStrengthIndex { Length = DeMarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
		{
			_prevOscillator = rsiValue / 100m;
			return;
		}

		var oscillatorValue = rsiValue / 100m;
		if (_prevOscillator is null)
		{
			_prevOscillator = oscillatorValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Oscillator crosses below the lower level => oversold => buy
		if (_prevOscillator > LowerLevel && oscillatorValue <= LowerLevel)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		// Oscillator crosses above the upper level => overbought => sell
		else if (_prevOscillator < UpperLevel && oscillatorValue >= UpperLevel)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		_prevOscillator = oscillatorValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_rsi = null;
		_prevOscillator = null;

		base.OnReseted();
	}
}
