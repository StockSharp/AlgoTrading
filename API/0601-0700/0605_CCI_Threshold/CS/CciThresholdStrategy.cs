using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Threshold strategy.
/// Long only: buys when CCI(LookbackPeriod) is below BuyThreshold and closes the long when the close is above the previous close.
/// Optional stop loss and take profit are StopLossPoints and TakeProfitPoints price steps from the entry.
/// </summary>
public class CciThresholdStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// CCI level below which the strategy buys.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable the stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Enable the take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
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
	public CciThresholdStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "CCI period", "CCI");

		_buyThreshold = Param(nameof(BuyThreshold), -90m)
			.SetDisplay("Buy Threshold", "CCI level below which the strategy buys", "CCI");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Use Stop Loss", "Enable the stop loss", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable the take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;

		var cci = new CommodityChannelIndex { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		var takeProfit = UseTakeProfit && TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Absolute) : new Unit();
		var stopLoss = UseStopLoss && StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Absolute) : new Unit();
		StartProtection(takeProfit, stopLoss, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		_prevClose = close;

		if (!cciValue.IsFormed || prevClose is not decimal pc)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (close > pc)
				SellMarket(Position);
		}
		else if (Position == 0 && cciValue.GetValue<decimal>() < BuyThreshold)
		{
			BuyMarket(Volume);
		}
	}
}
