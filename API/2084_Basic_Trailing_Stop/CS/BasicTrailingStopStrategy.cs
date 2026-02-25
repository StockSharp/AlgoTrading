using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing a basic trailing stop with CCI and RSI signals.
/// </summary>
public class BasicTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private decimal _stopPrice;

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BasicTrailingStopStrategy()
	{
		_stopLossPct = Param(nameof(StopLossPct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Trailing stop distance as percentage", "Risk Management");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Relative Strength Index period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, (candle, rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var cciResult = _cci.Process(candle);
				if (!cciResult.IsFormed)
					return;

				var cciValue = cciResult.ToDecimal();
				ProcessCandle(candle, cciValue, rsiValue);
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal rsiValue)
	{
		var stopOffset = candle.ClosePrice * StopLossPct / 100m;

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - stopOffset;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_stopPrice = 0m;
			}

			return;
		}

		if (Position < 0)
		{
			var newStop = candle.ClosePrice + stopOffset;
			if (_stopPrice == 0m || newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_stopPrice = 0m;
			}

			return;
		}

		// No position - evaluate entry signals
		var longSignal = cciValue < -50m && rsiValue < 40m;
		var shortSignal = cciValue > 50m && rsiValue > 60m;

		if (longSignal)
		{
			BuyMarket();
			_stopPrice = candle.ClosePrice - stopOffset;
		}
		else if (shortSignal)
		{
			SellMarket();
			_stopPrice = candle.ClosePrice + stopOffset;
		}
	}
}
