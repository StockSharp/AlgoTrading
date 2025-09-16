using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchain Counter strategy converted from MQL5 Donchain counter expert advisor.
/// Tracks Donchian channel expansions for entries and trails stops along the channel bands.
/// </summary>
public class DonchainCounterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _bufferSteps;
	private readonly StrategyParam<TimeSpan> _tradeCooldown;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian = null!;
	private decimal _priceStep;
	private decimal _tolerance;
	private decimal _currentUpper;
	private decimal _currentLower;
	private decimal _previousUpper;
	private decimal _previousLower;
	private decimal _earlierUpper;
	private decimal _earlierLower;
	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;
	private DateTimeOffset? _lastTradeTime;

	/// <summary>
	/// Initializes a new instance of <see cref="DonchainCounterStrategy"/>.
	/// </summary>
	public DonchainCounterStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for entries", "Trading");

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bufferSteps = Param(nameof(BufferSteps), 50)
			.SetGreaterThanZero()
			.SetDisplay("Buffer Steps", "Minimum price steps before trailing stop activates", "Risk");

		_tradeCooldown = Param(nameof(TradeCooldown), TimeSpan.FromDays(1))
			.SetDisplay("Trade Cooldown", "Minimum waiting time between new entries", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for Donchian evaluation", "General");
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Donchian channel lookback period.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Number of price steps that price must move beyond the opposite band before trailing starts.
	/// </summary>
	public int BufferSteps
	{
		get => _bufferSteps.Value;
		set => _bufferSteps.Value = value;
	}

	/// <summary>
	/// Minimum cooldown between new trades.
	/// </summary>
	public TimeSpan TradeCooldown
	{
		get => _tradeCooldown.Value;
		set => _tradeCooldown.Value = value;
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_donchian = null!;
		_priceStep = 0m;
		_tolerance = 0m;
		_currentUpper = 0m;
		_currentLower = 0m;
		_previousUpper = 0m;
		_previousLower = 0m;
		_earlierUpper = 0m;
		_earlierLower = 0m;
		_longStopLevel = null;
		_shortStopLevel = null;
		_lastTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 0.0001m;
		}

		_tolerance = _priceStep / 2m;

		_donchian = new DonchianChannels
		{
			Length = ChannelPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, ProcessDonchian)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDonchian(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		// Only process completed candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!donchianValue.IsFinal)
		{
			return;
		}

		var value = (DonchianChannelsValue)donchianValue;

		if (value.UpperBand is not decimal upperBand || value.LowerBand is not decimal lowerBand)
		{
			return;
		}

		_currentUpper = upperBand;
		_currentLower = lowerBand;

		var hadPosition = Position != 0m;
		if (hadPosition)
		{
			ManageExistingPosition(candle);
			UpdateHistory();
			return;
		}

		TryOpenPosition(candle);
		UpdateHistory();
	}

	private void ManageExistingPosition(ICandleMessage candle)
	{
		// Buffer converts the point-based activation threshold into price units.
		var buffer = BufferSteps * _priceStep;

		if (Position > 0m)
		{
			// Activate or advance the trailing stop once price moves far enough from the lower band.
			if (candle.HighPrice > _currentLower + buffer)
			{
				if (!_longStopLevel.HasValue || _longStopLevel.Value < _currentLower - _tolerance)
				{
					_longStopLevel = _currentLower;
					LogInfo($"Updated long stop to {_longStopLevel.Value}");
				}
			}

			// Exit the long position when price falls back through the protected band.
			if (_longStopLevel.HasValue && candle.LowPrice <= _longStopLevel.Value + _tolerance)
			{
				SellMarket(Position);
				LogInfo($"Long exit triggered at {_longStopLevel.Value}");
				_longStopLevel = null;
			}
		}
		else if (Position < 0m)
		{
			// Activate or advance the trailing stop once price moves far enough from the upper band.
			if (candle.LowPrice < _currentUpper - buffer)
			{
				if (!_shortStopLevel.HasValue || _shortStopLevel.Value > _currentUpper + _tolerance)
				{
					_shortStopLevel = _currentUpper;
					LogInfo($"Updated short stop to {_shortStopLevel.Value}");
				}
			}

			// Exit the short position when price rallies back to the protected band.
			if (_shortStopLevel.HasValue && candle.HighPrice >= _shortStopLevel.Value - _tolerance)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit triggered at {_shortStopLevel.Value}");
				_shortStopLevel = null;
			}
		}
		else
		{
			_longStopLevel = null;
			_shortStopLevel = null;
		}
	}

	private void TryOpenPosition(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		// Require at least two completed Donchian samples for breakout comparisons.
		if (_previousUpper == 0m || _earlierUpper == 0m || _previousLower == 0m || _earlierLower == 0m)
		{
			return;
		}

		var now = candle.CloseTime;
		if (_lastTradeTime.HasValue && now - _lastTradeTime.Value < TradeCooldown)
		{
			return;
		}

		// Long entry when the upper Donchian band expanded on the previous bar.
		if (_previousUpper > _earlierUpper && !AreClose(_previousUpper, _earlierUpper))
		{
			BuyMarket(Volume);
			_longStopLevel = _currentLower;
			_lastTradeTime = now;
			LogInfo($"Long entry at {candle.ClosePrice} with stop {_currentLower}");
			return;
		}

		// Short entry when the lower Donchian band contracted on the previous bar.
		if (_previousLower < _earlierLower && !AreClose(_previousLower, _earlierLower))
		{
			SellMarket(Volume);
			_shortStopLevel = _currentUpper;
			_lastTradeTime = now;
			LogInfo($"Short entry at {candle.ClosePrice} with stop {_currentUpper}");
		}
	}

	private void UpdateHistory()
	{
		_earlierUpper = _previousUpper;
		_earlierLower = _previousLower;
		_previousUpper = _currentUpper;
		_previousLower = _currentLower;
	}

	private bool AreClose(decimal first, decimal second)
	{
		return Math.Abs(first - second) <= _tolerance;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longStopLevel = null;
			_shortStopLevel = null;
		}
		else if (Position > 0m)
		{
			_shortStopLevel = null;
		}
		else
		{
			_longStopLevel = null;
		}
	}
}
