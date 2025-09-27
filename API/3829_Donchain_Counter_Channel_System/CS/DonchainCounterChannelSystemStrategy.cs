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
/// Donchain counter-channel system converted from the original MQL4 expert advisor.
/// Buys when the lower Donchian band turns upward and sells when the upper band turns downward.
/// </summary>
public class DonchainCounterChannelSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<TimeSpan> _tradeCooldown;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian = null!;
	private decimal _priceStep;
	private decimal _tolerance;
	private decimal _currentUpper;
	private decimal _currentLower;
	private decimal? _previousUpper;
	private decimal? _previousLower;
	private decimal? _earlierUpper;
	private decimal? _earlierLower;
	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;
	private DateTimeOffset? _lastTradeTime;

	/// <summary>
	/// Initializes a new instance of <see cref="DonchainCounterChannelSystemStrategy"/>.
	/// </summary>
	public DonchainCounterChannelSystemStrategy()
	{

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Donchian Period", "Lookback period for the Donchian channel", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_tradeCooldown = Param(nameof(TradeCooldown), TimeSpan.FromDays(1))
		.SetDisplay("Trade Cooldown", "Minimum waiting time between entries", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for Donchian calculations", "General");
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
	/// Minimum time between consecutive trades.
	/// </summary>
	public TimeSpan TradeCooldown
	{
		get => _tradeCooldown.Value;
		set => _tradeCooldown.Value = value;
	}

	/// <summary>
	/// Candle type used for the Donchian indicator.
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
		_previousUpper = null;
		_previousLower = null;
		_earlierUpper = null;
		_earlierLower = null;
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
			Length = ChannelPeriod,
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
		// Process only completed candles to avoid premature signals.
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

		if (Position != 0m)
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
		if (Position > 0m)
		{
			var candidateStop = _currentLower;
			if (!_longStopLevel.HasValue || candidateStop > _longStopLevel.Value + _tolerance)
			{
				_longStopLevel = candidateStop;
				LogInfo($"Updated long stop to {_longStopLevel.Value}");
			}

			if (_longStopLevel.HasValue && candle.LowPrice <= _longStopLevel.Value + _tolerance)
			{
				SellMarket(Position);
				LogInfo($"Long exit triggered at {_longStopLevel.Value}");
				_longStopLevel = null;
			}
		}
		else if (Position < 0m)
		{
			var candidateStop = _currentUpper;
			if (!_shortStopLevel.HasValue || candidateStop < _shortStopLevel.Value - _tolerance)
			{
				_shortStopLevel = candidateStop;
				LogInfo($"Updated short stop to {_shortStopLevel.Value}");
			}

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

		if (_previousLower is not decimal previousLower || _earlierLower is not decimal earlierLower ||
		_previousUpper is not decimal previousUpper || _earlierUpper is not decimal earlierUpper)
		{
			return;
		}

		var now = candle.CloseTime;
		if (_lastTradeTime.HasValue && now - _lastTradeTime.Value < TradeCooldown)
		{
			return;
		}

		if (previousLower > earlierLower + _tolerance)
		{
			BuyMarket(Volume);
			_longStopLevel = _currentLower;
			_lastTradeTime = now;
			LogInfo($"Long entry at {candle.ClosePrice} with stop {_currentLower}");
			return;
		}

		if (previousUpper < earlierUpper - _tolerance)
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

