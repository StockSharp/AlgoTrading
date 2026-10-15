namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from "DeMarker gaining position volume 2" expert advisor.
/// Uses DeMarker oscillator thresholds for entry signals.
/// </summary>
public class DeMarkerGainingPositionVolume2Strategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _prevOscillator;

	/// <summary>
	/// DeMarker averaging period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker threshold.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Lower DeMarker threshold.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Candle type for DeMarker calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DeMarkerGainingPositionVolume2Strategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator");

		_upperLevel = Param(nameof(UpperLevel), 0.7m)
			.SetDisplay("Upper Level", "Overbought threshold.", "Indicator");

		_lowerLevel = Param(nameof(LowerLevel), 0.3m)
			.SetDisplay("Lower Level", "Oversold threshold.", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations.", "Data");
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

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Cross below lower level => oversold => buy
		if (_prevOscillator is decimal prev)
		{
			var crossBelow = prev >= LowerLevel && oscillatorValue < LowerLevel;
			var crossAbove = prev <= UpperLevel && oscillatorValue > UpperLevel;

			if (crossBelow)
			{
				if (Position <= 0)
					BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
			}
			else if (crossAbove)
			{
				if (Position >= 0)
					SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
			}
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
