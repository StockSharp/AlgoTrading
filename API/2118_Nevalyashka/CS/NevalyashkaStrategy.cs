using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating long/short strategy (Nevalyashka / Tumbler).
/// Enters on RSI overbought/oversold, exits on TP/SL, then reverses.
/// </summary>
public class NevalyashkaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	private decimal _entryPrice;
	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	public NevalyashkaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period", "Parameters");

		_overbought = Param(nameof(Overbought), 65m)
			.SetDisplay("Overbought", "Overbought level", "Parameters");

		_oversold = Param(nameof(Oversold), 35m)
			.SetDisplay("Oversold", "Oversold level", "Parameters");
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
		_entryPrice = 0;
		_prevRsi = 50;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevRsi = 50;
		_hasPrev = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

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

		var price = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevRsi = rsiValue;
			_hasPrev = true;
			return;
		}

		// Exit on TP/SL
		if (Position > 0 && _entryPrice > 0)
		{
			var pnlPct = (price - _entryPrice) / _entryPrice * 100m;
			if (pnlPct >= 2m || pnlPct <= -1m || rsiValue > Overbought)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var pnlPct = (_entryPrice - price) / _entryPrice * 100m;
			if (pnlPct >= 2m || pnlPct <= -1m || rsiValue < Oversold)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry signals
		if (Position == 0)
		{
			if (_prevRsi <= Oversold && rsiValue > Oversold)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (_prevRsi >= Overbought && rsiValue < Overbought)
			{
				SellMarket();
				_entryPrice = price;
			}
		}

		_prevRsi = rsiValue;
	}
}
