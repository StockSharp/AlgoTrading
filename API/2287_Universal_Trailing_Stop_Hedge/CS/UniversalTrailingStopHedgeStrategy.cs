using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR-based trailing stop strategy.
/// Enters on candle direction, trails stop using ATR distance.
/// </summary>
public class UniversalTrailingStopHedgeStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UniversalTrailingStopHedgeStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop distance", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailingStop = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var distance = atr * AtrMultiplier;

		if (Position == 0)
		{
			// Enter based on candle direction
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_trailingStop = candle.ClosePrice - distance;
			}
			else if (candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_trailingStop = candle.ClosePrice + distance;
			}
			return;
		}

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - distance;
			if (newStop > _trailingStop)
				_trailingStop = newStop;
			if (candle.LowPrice <= _trailingStop)
			{
				SellMarket();
				_trailingStop = 0;
			}
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + distance;
			if (newStop < _trailingStop || _trailingStop == 0)
				_trailingStop = newStop;
			if (candle.HighPrice >= _trailingStop)
			{
				BuyMarket();
				_trailingStop = 0;
			}
		}
	}
}
