namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Opens a long or short market position when enabled and no opposite exposure exists.
/// Mirrors the behaviour of the MQL script that prevents duplicate entries.
/// </summary>
public class ConditionalPositionOpenerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;


	/// <summary>
	/// Stop-loss distance expressed in pips (price steps).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips (price steps).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Allow opening long positions when no long exposure exists.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Allow opening short positions when no short exposure exists.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Candle type used as a timing driver.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ConditionalPositionOpenerStrategy()
	{

		_stopLossPips = Param(nameof(StopLossPips), 100m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in price steps", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance in price steps", "Risk");

		_enableBuy = Param(nameof(EnableBuy), false)
			.SetDisplay("Enable Buy", "Open buy positions when allowed", "General");

		_enableSell = Param(nameof(EnableSell), false)
			.SetDisplay("Enable Sell", "Open sell positions when allowed", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Series driving the entry checks", "General");
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
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		var takeProfit = TakeProfitPips > 0m
			? new Unit(ConvertPipsToPrice(TakeProfitPips), UnitTypes.Point)
			: new Unit();

		var stopLoss = StopLossPips > 0m
			? new Unit(ConvertPipsToPrice(StopLossPips), UnitTypes.Point)
			: new Unit();

		StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		if (_priceStep > 0m)
			return pips * _priceStep;

		return pips;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentPosition = Position;

		if (EnableBuy && currentPosition <= 0m)
		{
			// Enter long when enabled and there is no existing long exposure.
			BuyMarket(Volume);
			currentPosition += Volume;
		}

		if (EnableSell && currentPosition >= 0m)
		{
			// Enter short when enabled and there is no existing short exposure.
			SellMarket(Volume);
		}
	}
}

