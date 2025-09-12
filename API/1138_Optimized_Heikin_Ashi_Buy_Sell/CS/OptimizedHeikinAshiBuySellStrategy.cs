namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Heikin-Ashi strategy trading a single direction with optional stop-loss and take-profit.
/// </summary>
public class OptimizedHeikinAshiBuySellStrategy : Strategy
{
	public enum TradeType
	{
		BuyOnly,
		SellOnly
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<TradeType> _tradeType;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;

	public OptimizedHeikinAshiBuySellStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Heikin-Ashi calculation", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2023, 1, 1), TimeSpan.Zero))
			.SetDisplay("Start Date", "Backtest start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero))
			.SetDisplay("End Date", "Backtest end date", "General");

		_tradeType = Param(nameof(TradeMode), TradeType.BuyOnly)
			.SetDisplay("Trade Type", "Choose buy or sell mode", "General");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop loss percentage", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit (%)", "Take profit percentage", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	public TradeType TradeMode
	{
		get => _tradeType.Value;
		set => _tradeType.Value = value;
	}

	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = null;
		_prevHaClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null,
			UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : null
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;
		if (time < StartDate || time > EndDate)
			return;

		decimal haOpen;
		decimal haClose;

		if (_prevHaOpen == null)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}
		else
		{
			haOpen = (_prevHaOpen.Value + _prevHaClose.Value) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}

		var isBullish = haClose > haOpen;
		var isBearish = haClose < haOpen;

		if (TradeMode == TradeType.BuyOnly)
		{
			if (isBullish && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (isBearish && Position > 0)
				SellMarket(Position);
		}
		else
		{
			if (isBearish && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (isBullish && Position < 0)
				BuyMarket(Math.Abs(Position));
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}
