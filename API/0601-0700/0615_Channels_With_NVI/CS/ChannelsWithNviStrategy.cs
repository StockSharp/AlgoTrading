using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Channels with NVI strategy.
/// The channel is a Bollinger band (ChannelType "BB") or a Keltner channel (ChannelType "KC": EMA plus and minus ChannelMultiplier
/// ATRs) of ChannelLength. The Negative Volume Index starts at 1000 and moves with the close only on candles whose volume is below the
/// previous one. Long only: buys when the close is below the lower channel line and NVI is above EMA(NviEmaLength) of NVI, and closes the
/// long when NVI falls below that EMA. Optional percent stop loss and take profit protect the position.
/// </summary>
public class ChannelsWithNviStrategy : Strategy
{
	private readonly StrategyParam<string> _channelType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<decimal> _channelMultiplier;
	private readonly StrategyParam<int> _nviEmaLength;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _nviEma;
	private decimal _nvi;
	private decimal? _prevClose;
	private decimal? _prevVolume;

	/// <summary>
	/// Channel kind: "BB" for Bollinger Bands, "KC" for Keltner Channels.
	/// </summary>
	public string ChannelType
	{
		get => _channelType.Value;
		set => _channelType.Value = value;
	}

	/// <summary>
	/// Channel period.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// Channel width multiplier.
	/// </summary>
	public decimal ChannelMultiplier
	{
		get => _channelMultiplier.Value;
		set => _channelMultiplier.Value = value;
	}

	/// <summary>
	/// EMA period of NVI.
	/// </summary>
	public int NviEmaLength
	{
		get => _nviEmaLength.Value;
		set => _nviEmaLength.Value = value;
	}

	/// <summary>
	/// Enable the stop loss.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable the take profit.
	/// </summary>
	public bool EnableTakeProfit
	{
		get => _enableTakeProfit.Value;
		set => _enableTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ChannelsWithNviStrategy()
	{
		_channelType = Param(nameof(ChannelType), "BB")
			.SetDisplay("Channel Type", "Channel kind: BB for Bollinger Bands, KC for Keltner Channels", "Channel");

		_channelLength = Param(nameof(ChannelLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Channel period", "Channel");

		_channelMultiplier = Param(nameof(ChannelMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Channel Multiplier", "Channel width multiplier", "Channel");

		_nviEmaLength = Param(nameof(NviEmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("NVI EMA Length", "EMA period of NVI", "NVI");

		_enableStopLoss = Param(nameof(EnableStopLoss), false)
			.SetDisplay("Enable Stop Loss", "Enable the stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_enableTakeProfit = Param(nameof(EnableTakeProfit), false)
			.SetDisplay("Enable Take Profit", "Enable the take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_nviEma = null;
		_nvi = 1000m;
		_prevClose = null;
		_prevVolume = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_nvi = 1000m;
		_prevClose = null;
		_prevVolume = null;
		_nviEma = new ExponentialMovingAverage { Length = NviEmaLength };

		var bollinger = new BollingerBands { Length = ChannelLength, Width = ChannelMultiplier };
		var ema = new ExponentialMovingAverage { Length = ChannelLength };
		var atr = new AverageTrueRange { Length = ChannelLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ema, atr, ProcessCandle)
			.Start();

		var takeProfit = EnableTakeProfit && TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit();
		var stopLoss = EnableStopLoss && StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit();
		StartProtection(takeProfit, stopLoss, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		if (_prevClose is decimal pc && _prevVolume is decimal pv && pc != 0 && volume < pv)
			_nvi += _nvi * (close - pc) / pc;

		_prevClose = close;
		_prevVolume = volume;

		var nviEmaValue = _nviEma.Process(new DecimalIndicatorValue(_nviEma, _nvi, candle.OpenTime) { IsFinal = true });

		if (!nviEmaValue.IsFormed)
			return;

		decimal lower;

		if (ChannelType.EqualsIgnoreCase("KC"))
		{
			if (!emaValue.IsFormed || !atrValue.IsFormed)
				return;

			lower = emaValue.GetValue<decimal>() - ChannelMultiplier * atrValue.GetValue<decimal>();
		}
		else
		{
			if (!bollingerValue.IsFormed || ((BollingerBandsValue)bollingerValue).LowBand is not decimal low)
				return;

			lower = low;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var nviEma = nviEmaValue.GetValue<decimal>();

		if (Position > 0)
		{
			if (_nvi < nviEma)
				SellMarket(Position);
		}
		else if (Position == 0 && close < lower && _nvi > nviEma)
		{
			BuyMarket(Volume);
		}
	}
}
