using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy with take profit and stop loss on lower timeframe.
/// </summary>
public class RsiStrategyWithTpSlLowerTfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _buyLevel;
	private readonly StrategyParam<int> _sellLevel;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<Sides?> _direction;

	private RelativeStrengthIndex _rsi;

	/// <summary>
	/// Initializes a new instance of <see cref="RsiStrategyWithTpSlLowerTfStrategy"/>.
	/// </summary>
	public RsiStrategyWithTpSlLowerTfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(1, 100)
			.SetDisplay("RSI Period", "RSI calculation period", "RSI")
			.SetCanOptimize(true);

		_buyLevel = Param(nameof(BuyLevel), 40)
			.SetRange(0, 100)
			.SetDisplay("Buy Level", "RSI level to buy", "RSI")
			.SetCanOptimize(true);

		_sellLevel = Param(nameof(SellLevel), 60)
			.SetRange(0, 100)
			.SetDisplay("Sell Level", "RSI level to sell", "RSI")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetRange(0.1m, 100m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.1m, 100m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
			.SetCanOptimize(true);

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Allowed trade direction", "General");
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI level to buy.
	/// </summary>
	public int BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }

	/// <summary>
	/// RSI level to sell.
	/// </summary>
	public int SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(_rsi, area);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

var allowLong = Direction != Sides.Sell;
var allowShort = Direction != Sides.Buy;

		if (allowLong && Position <= 0 && rsiValue < BuyLevel)
			BuyMarket(Volume);
		else if (allowShort && Position >= 0 && rsiValue > SellLevel)
			SellMarket(Volume);
	}
}

