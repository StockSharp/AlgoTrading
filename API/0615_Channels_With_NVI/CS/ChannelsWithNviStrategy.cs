using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using channel bands with Negative Volume Index.
/// Buys when price is below the lower band and NVI is above its EMA.
/// Closes the position when NVI falls below its EMA.
/// Optional stop-loss and take-profit in percent.
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
	
	private IIndicator _channel;
	private ExponentialMovingAverage _nviEma;
	private decimal _nvi;
	private decimal _prevClose;
	private decimal _prevVolume;
	
	/// <summary>
	/// Channel type: "BB" (Bollinger Bands) or "KC" (Keltner Channels).
	/// </summary>
	public string ChannelType { get => _channelType.Value; set => _channelType.Value = value; }
	
	/// <summary>
	/// Channel period length.
	/// </summary>
	public int ChannelLength { get => _channelLength.Value; set => _channelLength.Value = value; }
	
	/// <summary>
	/// Channel multiplier.
	/// </summary>
	public decimal ChannelMultiplier { get => _channelMultiplier.Value; set => _channelMultiplier.Value = value; }
	
	/// <summary>
	/// EMA length for NVI.
	/// </summary>
	public int NviEmaLength { get => _nviEmaLength.Value; set => _nviEmaLength.Value = value; }
	
	/// <summary>
	/// Enable stop-loss in percent.
	/// </summary>
	public bool EnableStopLoss { get => _enableStopLoss.Value; set => _enableStopLoss.Value = value; }
	
	/// <summary>
	/// Stop-loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	
	/// <summary>
	/// Enable take-profit in percent.
	/// </summary>
	public bool EnableTakeProfit { get => _enableTakeProfit.Value; set => _enableTakeProfit.Value = value; }
	
	/// <summary>
	/// Take-profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public ChannelsWithNviStrategy()
	{
	_channelType = Param(nameof(ChannelType), "BB")
	.SetDisplay("Channel Type", "Use Bollinger Bands (BB) or Keltner Channels (KC)", "General");
	_channelLength = Param(nameof(ChannelLength), 20)
	.SetGreaterThanZero()
	.SetDisplay("Channel Length", "Period for channel calculation", "General")
	.SetCanOptimize(true)
	.SetOptimize(10, 50, 5);
	_channelMultiplier = Param(nameof(ChannelMultiplier), 2m)
	.SetGreaterThanZero()
	.SetDisplay("Channel Multiplier", "Multiplier for channel width", "General")
	.SetCanOptimize(true)
	.SetOptimize(1m, 4m, 0.5m);
	_nviEmaLength = Param(nameof(NviEmaLength), 200)
	.SetGreaterThanZero()
	.SetDisplay("NVI EMA Length", "Period for EMA of NVI", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(50, 300, 50);
	_enableStopLoss = Param(nameof(EnableStopLoss), false)
	.SetDisplay("Enable Stop Loss", "Use stop-loss protection", "Risk Management");
	_stopLossPercent = Param(nameof(StopLossPercent), 0m)
	.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
	.SetCanOptimize(true)
	.SetOptimize(1m, 10m, 1m);
	_enableTakeProfit = Param(nameof(EnableTakeProfit), false)
	.SetDisplay("Enable Take Profit", "Use take-profit protection", "Risk Management");
	_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
	.SetDisplay("Take Profit %", "Take-profit percentage", "Risk Management")
	.SetCanOptimize(true)
	.SetOptimize(1m, 10m, 1m);
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
	_channel = null;
	_nviEma = null;
	_nvi = 1000m;
	_prevClose = 0m;
	_prevVolume = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_channel = ChannelType == "KC"
	? new KeltnerChannels { Length = ChannelLength, Multiplier = ChannelMultiplier }
	: new BollingerBands { Length = ChannelLength, Width = ChannelMultiplier };
	
	_nviEma = new ExponentialMovingAverage { Length = NviEmaLength };
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_channel, ProcessCandle)
	.Start();
	
	StartProtection(
	EnableTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
	EnableStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit());
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _channel);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue channelValue)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	decimal upperBand;
	decimal lowerBand;
	
	if (_channel is BollingerBands)
	{
	var bb = (BollingerBandsValue)channelValue;
	if (bb.UpBand is not decimal up || bb.LowBand is not decimal low)
	return;
	upperBand = up;
	lowerBand = low;
	}
	else
	{
	var kc = (KeltnerChannelsValue)channelValue;
	if (kc.Upper is not decimal up || kc.Lower is not decimal low)
	return;
	upperBand = up;
	lowerBand = low;
	}
	
	if (_prevClose == 0m)
	{
	_prevClose = candle.ClosePrice;
	_prevVolume = candle.TotalVolume;
	_nviEma.Process(new DecimalIndicatorValue(_nviEma, _nvi));
	return;
	}
	
	if (candle.TotalVolume < _prevVolume)
	{
	var change = (candle.ClosePrice - _prevClose) / _prevClose;
	_nvi += change * _nvi;
	}
	
	var emaVal = _nviEma.Process(new DecimalIndicatorValue(_nviEma, _nvi));
	if (!emaVal.IsFinal)
	{
	_prevClose = candle.ClosePrice;
	_prevVolume = candle.TotalVolume;
	return;
	}
	
	var ema = emaVal.ToDecimal();
	
	if (candle.ClosePrice < lowerBand && _nvi > ema && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	
	if (_nvi < ema && Position > 0)
	SellMarket(Position);
	
	_prevClose = candle.ClosePrice;
	_prevVolume = candle.TotalVolume;
	}
}

