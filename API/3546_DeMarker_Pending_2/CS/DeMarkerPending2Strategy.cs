namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pending order strategy driven by the DeMarker oscillator.
/// Simplified from "DeMarker Pending 2" to use market orders on threshold crossovers.
/// </summary>
public class DeMarkerPending2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _demarkerUpperLevel;
	private readonly StrategyParam<decimal> _demarkerLowerLevel;

	private RelativeStrengthIndex _rsi;
	private decimal? _prevOscillator;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// DeMarker oscillator period.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker threshold for sell signal.
	/// </summary>
	public decimal DemarkerUpperLevel
	{
		get => _demarkerUpperLevel.Value;
		set => _demarkerUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower DeMarker threshold for buy signal.
	/// </summary>
	public decimal DemarkerLowerLevel
	{
		get => _demarkerLowerLevel.Value;
		set => _demarkerLowerLevel.Value = value;
	}

	public DeMarkerPending2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "DeMarker oscillator period", "Indicator")
			.SetGreaterThanZero();

		_demarkerUpperLevel = Param(nameof(DemarkerUpperLevel), 0.7m)
			.SetDisplay("Upper Level", "Overbought threshold", "Indicator");

		_demarkerLowerLevel = Param(nameof(DemarkerLowerLevel), 0.3m)
			.SetDisplay("Lower Level", "Oversold threshold", "Indicator");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevOscillator = null;
		_rsi = new RelativeStrengthIndex { Length = DemarkerPeriod };

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

		if (_prevOscillator is not decimal prev)
		{
			_prevOscillator = rsiValue / 100m;
			return;
		}

		var oscillatorValue = rsiValue / 100m;
		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Cross above lower level from below => buy
		if (prev < DemarkerLowerLevel && oscillatorValue >= DemarkerLowerLevel)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		// Cross below upper level from above => sell
		else if (prev > DemarkerUpperLevel && oscillatorValue <= DemarkerUpperLevel)
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
