using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on averaged RSI values.
/// </summary>
public class AutoTradeWithRsiStrategy : Strategy
{
	private readonly StrategyParam<bool> _buyEnabled;
	private readonly StrategyParam<bool> _sellEnabled;
	private readonly StrategyParam<bool> _closeBySignal;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _averagePeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<decimal> _closeBuyThreshold;
	private readonly StrategyParam<decimal> _closeSellThreshold;
	private readonly StrategyParam<DataType> _candleType;

	public bool BuyEnabled { get => _buyEnabled.Value; set => _buyEnabled.Value = value; }
	public bool SellEnabled { get => _sellEnabled.Value; set => _sellEnabled.Value = value; }
	public bool CloseBySignal { get => _closeBySignal.Value; set => _closeBySignal.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int AveragePeriod { get => _averagePeriod.Value; set => _averagePeriod.Value = value; }
	public decimal BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }
	public decimal SellThreshold { get => _sellThreshold.Value; set => _sellThreshold.Value = value; }
	public decimal CloseBuyThreshold { get => _closeBuyThreshold.Value; set => _closeBuyThreshold.Value = value; }
	public decimal CloseSellThreshold { get => _closeSellThreshold.Value; set => _closeSellThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AutoTradeWithRsiStrategy()
	{
		_buyEnabled = Param(nameof(BuyEnabled), true)
			.SetDisplay("Open Buy", "Enable long entries", "General");

		_sellEnabled = Param(nameof(SellEnabled), true)
			.SetDisplay("Open Sell", "Enable short entries", "General");

		_closeBySignal = Param(nameof(CloseBySignal), false)
			.SetDisplay("Close By Signal", "Exit on opposite RSI signal", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicator")
			.SetCanOptimize(true);

		_averagePeriod = Param(nameof(AveragePeriod), 21)
			.SetDisplay("Average Period", "Number of RSI values to average", "Indicator")
			.SetCanOptimize(true);

		_buyThreshold = Param(nameof(BuyThreshold), 55m)
			.SetDisplay("Buy Threshold", "Average RSI above which to buy", "Rules")
			.SetCanOptimize(true);

		_sellThreshold = Param(nameof(SellThreshold), 45m)
			.SetDisplay("Sell Threshold", "Average RSI below which to sell", "Rules")
			.SetCanOptimize(true);

		_closeBuyThreshold = Param(nameof(CloseBuyThreshold), 47m)
			.SetDisplay("Close Buy Threshold", "Average RSI below which to close long", "Rules")
			.SetCanOptimize(true);

		_closeSellThreshold = Param(nameof(CloseSellThreshold), 52m)
			.SetDisplay("Close Sell Threshold", "Average RSI above which to close short", "Rules")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Candle data type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create RSI indicator and SMA to average its values
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var rsiAverage = new SimpleMovingAverage { Length = AveragePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, rsiAverage, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal averageRsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (BuyEnabled && Position == 0 && averageRsi > BuyThreshold)
			BuyMarket();
		else if (SellEnabled && Position == 0 && averageRsi < SellThreshold)
			SellMarket();

		if (!CloseBySignal)
			return;

		if (Position > 0 && averageRsi < CloseBuyThreshold)
			SellMarket();
		else if (Position < 0 && averageRsi > CloseSellThreshold)
			BuyMarket();
	}
}
