using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Parabolic SAR with volume confirmation.
/// Enters trades when price crosses the Parabolic SAR with above-average volume.
/// </summary>
public class ParabolicSarVolumeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _parabolicSar;
	private VolumeIndicator _volumeIndicator;
	private SimpleMovingAverage _volumeAverage;
	
	private decimal _prevSar;
	private decimal _currentAvgVolume;
	private bool _prevPriceAboveSar;
	private int _cooldown;

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Period for volume moving average.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarVolumeStrategy"/>.
	/// </summary>
	public ParabolicSarVolumeStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetRange(0.01m, 0.1m)
			
			.SetDisplay("SAR Acceleration", "Starting acceleration factor", "Indicators");

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetRange(0.1m, 0.5m)
			
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor", "Indicators");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetRange(10, 50)
			
			.SetDisplay("Volume Period", "Period for volume moving average", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 30)
			.SetRange(1, 100)
			.SetDisplay("Cooldown Bars", "Bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevSar = 0;
		_currentAvgVolume = 0;
		_prevPriceAboveSar = false;
		_cooldown = 0;
		_parabolicSar = null;
		_volumeIndicator = null;
		_volumeAverage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Initialize indicators
		_parabolicSar = new ParabolicSar
		{
			Acceleration = Acceleration,
			AccelerationMax = MaxAcceleration
		};

		_volumeIndicator = new VolumeIndicator();
		
		_volumeAverage = new SMA
		{
			Length = VolumePeriod
		};

		// Create candle subscription
		var subscription = SubscribeCandles(CandleType);

		// Binding for Parabolic SAR indicator
		subscription
			.Bind(_parabolicSar, _volumeIndicator, ProcessIndicators)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			
			var volumeArea = CreateChartArea();
			if (volumeArea != null)
			{
				DrawIndicator(volumeArea, _volumeIndicator);
				DrawIndicator(volumeArea, _volumeAverage);
			}
			
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, decimal sarValue, decimal volumeValue)
	{
		var avgValue = _volumeAverage.Process(new DecimalIndicatorValue(_volumeAverage, volumeValue, candle.ServerTime));
		if (avgValue == null)
			return;

		_currentAvgVolume = avgValue.ToDecimal();
		if (_currentAvgVolume <= 0)
			return;

		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until strategy and indicators are ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get current price and volume
		var currentPrice = candle.ClosePrice;
		var currentVolume = candle.TotalVolume;
		var isPriceAboveSar = currentPrice > sarValue;
		
		// Determine if volume is above average
		var isHighVolume = currentVolume > _currentAvgVolume * 1.5m;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevSar = sarValue;
			_prevPriceAboveSar = isPriceAboveSar;
			return;
		}

		// Check for SAR crossover with volume confirmation
		// Bullish crossover: Price crosses above SAR with high volume
		if (isPriceAboveSar && !_prevPriceAboveSar && isHighVolume && Position <= 0)
		{
			CancelActiveOrders();
			
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = CooldownBars;
		}
		// Bearish crossover: Price crosses below SAR with high volume
		else if (!isPriceAboveSar && _prevPriceAboveSar && isHighVolume && Position >= 0)
		{
			CancelActiveOrders();
			
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = CooldownBars;
		}
		// Exit signals based on SAR crossover (without volume confirmation)
		else if ((Position > 0 && !isPriceAboveSar) || (Position < 0 && isPriceAboveSar))
		{
			ClosePosition();
			_cooldown = CooldownBars;
		}

		// Update previous values for next candle
		_prevSar = sarValue;
		_prevPriceAboveSar = isPriceAboveSar;
	}
}
