using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading Bollinger Band breakouts with band momentum confirmation.
/// </summary>
public class BollingerBreakoutMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _breakoutPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _prevMiddle;
	private int _cooldownRemaining;

	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BreakoutPercent { get => _breakoutPercent.Value; set => _breakoutPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public BollingerBreakoutMomentumStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 18)
			.SetDisplay("BB Length", "Bollinger Bands length", "Parameters");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Parameters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 200)
			.SetDisplay("Take Profit (pips)", "Distance for profit target", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of working candles", "General");

		_breakoutPercent = Param(nameof(BreakoutPercent), 0.002m)
			.SetDisplay("Breakout %", "Minimum breakout beyond the Bollinger boundary", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_takePrice = 0m;
		_prevUpper = null;
		_prevLower = null;
		_prevMiddle = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished || !bbValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
			return;

		var step = Security.PriceStep ?? 1m;
		var price = candle.ClosePrice;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
			else
			{
				_stopPrice = Math.Max(_stopPrice, middle);
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else
			{
				_stopPrice = Math.Min(_stopPrice, middle);
			}
		}
		else if (_prevUpper is decimal prevUpper && _prevLower is decimal prevLower && _prevMiddle is decimal prevMiddle && _cooldownRemaining == 0)
		{
			var upperRising = upper > prevUpper && middle > prevMiddle;
			var lowerFalling = lower < prevLower && middle < prevMiddle;
			var buySignal = upperRising && price > upper * (1m + BreakoutPercent);
			var sellSignal = lowerFalling && price < lower * (1m - BreakoutPercent);

			if (buySignal)
			{
				BuyMarket();
				_stopPrice = middle;
				_takePrice = price + TakeProfitPips * step;
				_cooldownRemaining = CooldownBars;
			}
			else if (sellSignal)
			{
				SellMarket();
				_stopPrice = middle;
				_takePrice = price - TakeProfitPips * step;
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevMiddle = middle;
	}
}
