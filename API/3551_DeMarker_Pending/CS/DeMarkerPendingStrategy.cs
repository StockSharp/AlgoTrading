using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from the "DeMarker Pending 2.5" MetaTrader expert.
/// Uses DeMarker indicator level crossovers to generate buy/sell market signals.
/// Original used pending orders; this version uses market orders.
/// </summary>
public class DeMarkerPendingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _demarkerUpper;
	private readonly StrategyParam<decimal> _demarkerLower;

	private DeMarker _deMarker;
	private decimal? _prevDeMarker;

	/// <summary>
	/// Candle type used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker threshold that triggers sell signals.
	/// </summary>
	public decimal DemarkerUpperLevel
	{
		get => _demarkerUpper.Value;
		set => _demarkerUpper.Value = value;
	}

	/// <summary>
	/// Lower DeMarker threshold that triggers buy signals.
	/// </summary>
	public decimal DemarkerLowerLevel
	{
		get => _demarkerLower.Value;
		set => _demarkerLower.Value = value;
	}

	public DeMarkerPendingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal evaluation", "General");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "Averaging period for DeMarker indicator", "Indicator");

		_demarkerUpper = Param(nameof(DemarkerUpperLevel), 0.6m)
			.SetDisplay("Upper Level", "DeMarker value that triggers sell setup", "Indicator");

		_demarkerLower = Param(nameof(DemarkerLowerLevel), 0.4m)
			.SetDisplay("Lower Level", "DeMarker value that triggers buy setup", "Indicator");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDeMarker = null;
		_deMarker = new DeMarker { Length = DemarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_deMarker, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_deMarker.IsFormed)
		{
			_prevDeMarker = deMarkerValue;
			return;
		}

		if (_prevDeMarker is null)
		{
			_prevDeMarker = deMarkerValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// DeMarker crosses below lower level -> buy signal
		var crossDown = _prevDeMarker.Value > DemarkerLowerLevel && deMarkerValue <= DemarkerLowerLevel;
		// DeMarker crosses above upper level -> sell signal
		var crossUp = _prevDeMarker.Value < DemarkerUpperLevel && deMarkerValue >= DemarkerUpperLevel;

		if (crossDown)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossUp)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevDeMarker = deMarkerValue;
	}
}
