namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Money Flow Index histogram strategy.
/// Buys when MFI crosses above the high level.
/// Sells when MFI crosses below the low level.
/// </summary>
public class MfiHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMfi;

	/// <summary>
	/// Period for Money Flow Index.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss value in ticks.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit value in ticks.
	/// </summary>
	public Unit TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MfiHistogramStrategy"/>.
	/// </summary>
	public MfiHistogramStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MFI Period", "Period for Money Flow Index", "MFI");

		_highLevel = Param(nameof(HighLevel), 60m)
		.SetDisplay("High Level", "Overbought threshold", "MFI");

		_lowLevel = Param(nameof(LowLevel), 40m)
		.SetDisplay("Low Level", "Oversold threshold", "MFI");

		_stopLoss = Param(nameof(StopLoss), new Unit(1000, UnitTypes.Step))
		.SetDisplay("Stop Loss", "Stop-loss in ticks", "Risk");

		_takeProfit = Param(nameof(TakeProfit), new Unit(2000, UnitTypes.Step))
		.SetDisplay("Take Profit", "Take-profit in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe of candles", "General");
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

	_prevMfi = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var mfi = new MoneyFlowIndex
	{
		Length = MfiPeriod
	};

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(mfi, ProcessCandle).Start();

	StartProtection(stopLoss: StopLoss, takeProfit: TakeProfit);
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	// MFI crosses above high level
	if (mfiValue > HighLevel && _prevMfi <= HighLevel)
	{
	if (Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	}
	// MFI crosses below low level
	else if (mfiValue < LowLevel && _prevMfi >= LowLevel)
	{
	if (Position >= 0)
	SellMarket(Volume + Math.Abs(Position));
	}

	_prevMfi = mfiValue;
	}
}
