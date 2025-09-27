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
/// Breakout strategy converted from the MQL4 expert advisor "Daydream".
/// The strategy enters long when price closes below the previous Donchian low and enters short when price closes above the previous Donchian high.
/// A virtual take profit measured in pips trails the market in the trade direction and forces exits without placing real orders on the exchange.
/// </summary>
public class DaydreamStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian = null!;
	private DateTimeOffset _lastSignalCandleTime;
	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private decimal? _virtualTakeProfit;
	private int _currentDirection;
	private decimal _pipSize;

	/// <summary>
	/// Trading volume used for every market entry.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of completed candles used in the Donchian channel calculation.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Distance to the take profit level expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used for Donchian channel calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DaydreamStrategy"/>.
	/// </summary>
	public DaydreamStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume for new market entries", "Trading")
			.SetCanOptimize(true);

		_channelPeriod = Param(nameof(ChannelPeriod), 25)
			.SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 15m)
			.SetDisplay("Take Profit (pips)", "Virtual take profit distance", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Donchian channel", "Data");

		_lastSignalCandleTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_donchian = new DonchianChannels
		{
			Length = ChannelPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var channelValue = (DonchianChannelsValue)indicatorValue;

		if (channelValue.UpperBand is not decimal upperBand ||
			channelValue.LowerBand is not decimal lowerBand)
		{
			return;
		}

		if (_previousUpperBand is null || _previousLowerBand is null)
		{
			_previousUpperBand = upperBand;
			_previousLowerBand = lowerBand;
			return;
		}

		var previousUpper = _previousUpperBand.Value;
		var previousLower = _previousLowerBand.Value;
		var takeProfitDistance = TakeProfitPips * (_pipSize != 0m ? _pipSize : CalculatePipSize());

		if (candle.ClosePrice < previousLower)
		{
			TryCloseShort(candle);
			TryOpenLong(candle, takeProfitDistance);
		}
		else if (candle.ClosePrice > previousUpper)
		{
			TryCloseLong(candle);
			TryOpenShort(candle, takeProfitDistance);
		}

		UpdateTrailingTakeProfit(candle, takeProfitDistance);

		_previousUpperBand = upperBand;
		_previousLowerBand = lowerBand;
	}

	private void TryOpenLong(ICandleMessage candle, decimal takeProfitDistance)
	{
		if (_currentDirection != 0)
			return;

		if (candle.OpenTime <= _lastSignalCandleTime)
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_currentDirection = 1;
		_virtualTakeProfit = candle.ClosePrice + takeProfitDistance;
		_lastSignalCandleTime = candle.OpenTime;
	}

	private void TryOpenShort(ICandleMessage candle, decimal takeProfitDistance)
	{
		if (_currentDirection != 0)
			return;

		if (candle.OpenTime <= _lastSignalCandleTime)
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_currentDirection = -1;
		_virtualTakeProfit = candle.ClosePrice - takeProfitDistance;
		_lastSignalCandleTime = candle.OpenTime;
	}

	private void TryCloseLong(ICandleMessage candle)
	{
		if (_currentDirection != 1)
			return;

		if (candle.OpenTime <= _lastSignalCandleTime)
			return;

		var volume = Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_currentDirection = 0;
		_virtualTakeProfit = null;
		_lastSignalCandleTime = candle.OpenTime;
	}

	private void TryCloseShort(ICandleMessage candle)
	{
		if (_currentDirection != -1)
			return;

		if (candle.OpenTime <= _lastSignalCandleTime)
			return;

		var volume = Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_currentDirection = 0;
		_virtualTakeProfit = null;
		_lastSignalCandleTime = candle.OpenTime;
	}

	private void UpdateTrailingTakeProfit(ICandleMessage candle, decimal takeProfitDistance)
	{
		if (_currentDirection == 1)
		{
			var candidate = candle.ClosePrice + takeProfitDistance;

			if (_virtualTakeProfit is null || _virtualTakeProfit.Value > candidate)
				_virtualTakeProfit = candidate;

			if (_virtualTakeProfit.HasValue && candle.ClosePrice >= _virtualTakeProfit.Value && candle.OpenTime > _lastSignalCandleTime)
			{
				var volume = Math.Max(0m, Position);
				if (volume > 0m)
				{
					SellMarket(volume);
					_currentDirection = 0;
					_virtualTakeProfit = null;
					_lastSignalCandleTime = candle.OpenTime;
				}
			}
		}
		else if (_currentDirection == -1)
		{
			var candidate = candle.ClosePrice - takeProfitDistance;

			if (_virtualTakeProfit is null || _virtualTakeProfit.Value < candidate)
				_virtualTakeProfit = candidate;

			if (_virtualTakeProfit.HasValue && candle.ClosePrice <= _virtualTakeProfit.Value && candle.OpenTime > _lastSignalCandleTime)
			{
				var volume = Math.Max(0m, -Position);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_currentDirection = 0;
					_virtualTakeProfit = null;
					_lastSignalCandleTime = candle.OpenTime;
				}
			}
		}
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		var decimals = CountDecimals(priceStep);

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}

