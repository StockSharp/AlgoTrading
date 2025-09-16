using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI cross signals from the original cm_RSI expert.
/// </summary>
public class CmRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _isFirst = true;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI level to trigger long entries.
	/// </summary>
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }

	/// <summary>
	/// RSI level to trigger short entries.
	/// </summary>
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Volume applied to each trade.
	/// </summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="CmRsiStrategy"/>.
	/// </summary>
	public CmRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_buyLevel = Param(nameof(BuyLevel), 30m)
			.SetDisplay("Buy Level", "RSI level to enter long", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_sellLevel = Param(nameof(SellLevel), 70m)
			.SetDisplay("Sell Level", "RSI level to enter short", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_takeProfit = Param(nameof(TakeProfit), 200)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 400, 50);

		_stopLoss = Param(nameof(StopLoss), 100)
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 50);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of each trade", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		// Set trade volume from parameter
		Volume = OrderVolume;

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevRsi = rsiValue;
			_isFirst = false;
			return;
		}

		if (Position == 0)
		{
			// Open long when RSI crosses above buy level
			if (_prevRsi < BuyLevel && rsiValue > BuyLevel)
				BuyMarket();

			// Open short when RSI crosses below sell level
			if (_prevRsi > SellLevel && rsiValue < SellLevel)
				SellMarket();
		}

		_prevRsi = rsiValue;
	}
}
