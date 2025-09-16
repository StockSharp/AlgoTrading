using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Universum 3.0 strategy based on DeMarker indicator with martingale volume.
/// </summary>
public class Universum30Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _lossesLimit;

	private decimal _currentVolume;
	private int _losses;
	private decimal _lastPnL;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DemarkerPeriod { get => _demarkerPeriod.Value; set => _demarkerPeriod.Value = value; }

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Starting order volume.
	/// </summary>
	public decimal InitialVolume { get => _initialVolume.Value; set => _initialVolume.Value = value; }

	/// <summary>
	/// Maximum allowed consecutive losses.
	/// </summary>
	public int LossesLimit { get => _lossesLimit.Value; set => _lossesLimit.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="Universum30Strategy"/>.
	/// </summary>
	public Universum30Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");

		_demakerPeriod = Param(nameof(DemarkerPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "Length of DeMarker indicator", "Indicators");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target profit in absolute points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in absolute points", "Risk");

		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Base order volume", "Trading");

		_lossesLimit = Param(nameof(LossesLimit), 100)
			.SetGreaterThanZero()
			.SetDisplay("Losses Limit", "Max consecutive losses before stop", "Trading");
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

		_currentVolume = InitialVolume;
		_losses = 0;
		_lastPnL = 0m;

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Absolute));

		var demarker = new DeMarker { Length = DemarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(demarker, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, demarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal demarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (demarkerValue > 0.5m && Position <= 0)
			BuyMarket(_currentVolume + Math.Abs(Position));
		else if (demarkerValue < 0.5m && Position >= 0)
			SellMarket(_currentVolume + Math.Abs(Position));
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		var tradePnL = PnL - _lastPnL;
		_lastPnL = PnL;

		if (tradePnL > 0)
		{
			_currentVolume = InitialVolume;
			_losses = 0;
		}
		else if (tradePnL < 0)
		{
			_currentVolume *= 2;
			_losses++;
			if (_losses >= LossesLimit)
				Stop();
		}
	}
}
