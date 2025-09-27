
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
/// Channel breakout strategy converted from the MetaTrader TrainYourself expert advisor.
/// Automatically rebuilds a Donchian-style channel and trades breakouts after the price has consolidated inside it.
/// </summary>
public class TrainYourselfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<decimal> _bufferPoints;
	private readonly StrategyParam<decimal> _activationPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableTrendTrade;

	private DonchianChannels _channel = null!;
	private decimal _priceStep;
	private decimal _bufferDistance;
	private decimal _activationDistance;
	private bool _isArmed;
	private decimal? _upperBand;
	private decimal? _lowerBand;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrainYourselfStrategy"/> class.
	/// </summary>
	public TrainYourselfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for channel calculations", "General");

		_channelLength = Param(nameof(ChannelLength), 20)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Channel Length", "Number of candles used for the Donchian channel", "Channel");

		_bufferPoints = Param(nameof(BufferPoints), 50m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Buffer Points", "Extra distance in points added around the current price", "Channel");

		_activationPoints = Param(nameof(ActivationPoints), 2m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Activation Margin", "Points required inside the channel before arming breakouts", "Channel");

		_stopLossPoints = Param(nameof(StopLossPoints), 100)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss", "Stop-loss distance expressed in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Take Profit", "Take-profit distance expressed in points", "Risk");

		_enableTrendTrade = Param(nameof(EnableTrendTrade), true)
			.SetDisplay("Enable Auto Breakout", "Allow automatic breakout orders", "Trading");

	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used to build the Donchian-style channel.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// Additional distance in MetaTrader points that inflates the channel around the last close.
	/// </summary>
	public decimal BufferPoints
	{
		get => _bufferPoints.Value;
		set => _bufferPoints.Value = value;
	}

	/// <summary>
	/// Margin in points that must exist between price and the channel boundaries before the breakout is armed.
	/// </summary>
	public decimal ActivationPoints
	{
		get => _activationPoints.Value;
		set => _activationPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables the automatic breakout logic.
	/// </summary>
	public bool EnableTrendTrade
	{
		get => _enableTrendTrade.Value;
		set => _enableTrendTrade.Value = value;
	}


	/// <summary>
	/// Last calculated upper boundary of the channel.
	/// </summary>
	public decimal? UpperBand => _upperBand;

	/// <summary>
	/// Last calculated lower boundary of the channel.
	/// </summary>
	public decimal? LowerBand => _lowerBand;

	/// <summary>
	/// Indicates whether the breakout logic is currently armed.
	/// </summary>
	public bool IsArmed => _isArmed;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_isArmed = false;
		_upperBand = null;
		_lowerBand = null;
		_priceStep = 0m;
		_bufferDistance = 0m;
		_activationDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_channel = new DonchianChannels
		{
			Length = ChannelLength
		};

		_priceStep = GetPriceStep();
		_bufferDistance = _priceStep * BufferPoints;
		_activationDistance = _priceStep * ActivationPoints;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var takeProfitDistance = TakeProfitPoints > 0 ? TakeProfitPoints * _priceStep : 0m;
		var stopLossDistance = StopLossPoints > 0 ? StopLossPoints * _priceStep : 0m;

		StartProtection(
			takeProfit: takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Absolute) : default,
			stopLoss: stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Absolute) : default
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _channel);
			DrawOwnTrades(area);
		}
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var channelValue = (DonchianChannelsValue)_channel.Process(candle);

		if (channelValue.UpperBand is not decimal channelUpper ||
			channelValue.LowerBand is not decimal channelLower)
			return;

		var close = candle.ClosePrice;

		// Rebuild the MetaTrader-style channel with an additional buffer around the last close.
		var upper = Math.Max(channelUpper, close + _bufferDistance);
		var lower = Math.Min(channelLower, close - _bufferDistance);

		_upperBand = upper;
		_lowerBand = lower;

		// If a position is already open we keep the breakout logic disarmed until it is closed.
		if (Position != 0)
		{
			_isArmed = false;
			return;
		}

		if (!EnableTrendTrade)
		{
			_isArmed = false;
			return;
		}

		if (upper <= lower)
		{
			_isArmed = false;
			return;
		}

		// Arm the breakout once price has spent time inside the channel with a small safety margin.
		if (!_isArmed)
		{
			if (close > lower + _activationDistance && close < upper - _activationDistance)
				_isArmed = true;

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Break above the upper boundary triggers a long entry.
		if (close >= upper && AllowBuyOpen && Position <= 0)
		{
			BuyMarket(Volume);
			_isArmed = false;
			return;
		}

		// Break below the lower boundary triggers a short entry.
		if (close <= lower && AllowSellOpen && Position >= 0)
		{
			SellMarket(Volume);
			_isArmed = false;
		}
	}

	/// <summary>
	/// Opens a manual long trade, emulating the BUY triangle from the original script.
	/// </summary>
	public void TriggerManualBuy()
	{
		if (!AllowBuyOpen)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		BuyMarket(Volume);
		_isArmed = false;
	}

	/// <summary>
	/// Opens a manual short trade, emulating the SELL triangle from the original script.
	/// </summary>
	public void TriggerManualSell()
	{
		if (!AllowSellOpen)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		SellMarket(Volume);
		_isArmed = false;
	}

	/// <summary>
	/// Closes any open position, replacing the CLOSE ORDER label from the MetaTrader version.
	/// </summary>
	public void ClosePositionManually()
	{
		ClosePosition();
		_isArmed = false;
	}
}
