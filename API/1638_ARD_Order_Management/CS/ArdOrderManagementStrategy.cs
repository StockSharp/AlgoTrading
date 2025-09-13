using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on DeMarker crossing a threshold.
/// </summary>
public class ArdOrderManagementStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private DeMarker _deMarker;
	private decimal? _previousValue;

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Threshold for cross detection.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ArdOrderManagementStrategy"/>.
	/// </summary>
	public ArdOrderManagementStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "DeMarker indicator period", "Parameters")
			.SetCanOptimize(true);

		_threshold = Param(nameof(Threshold), 0.5m)
			.SetDisplay("Threshold", "DeMarker crossing level", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_deMarker = default;
		_previousValue = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_deMarker = new DeMarker { Length = DeMarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_deMarker, ProcessCandle).Start();

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

		if (_previousValue is null)
		{
			_previousValue = deMarkerValue;
			return;
		}

		var buySignal = _previousValue > Threshold && deMarkerValue < Threshold;
		var sellSignal = _previousValue < Threshold && deMarkerValue > Threshold;

		if (buySignal && Position <= 0)
			BuyMarket();
		else if (sellSignal && Position >= 0)
			SellMarket();

		_previousValue = deMarkerValue;
	}
}
