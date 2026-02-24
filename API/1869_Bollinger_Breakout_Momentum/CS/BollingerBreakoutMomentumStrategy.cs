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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading Bollinger Band breakouts with additional RSI and MACD filters.
/// Executes one trade per breakout and trails stop at the middle band.
/// </summary>
public class BollingerBreakoutMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _breakoutFactor;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private bool _breakoutFlag;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _hasPrev;

	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public decimal BreakoutFactor { get => _breakoutFactor.Value; set => _breakoutFactor.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollingerBreakoutMomentumStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 18)
			.SetDisplay("BB Length", "Bollinger Bands length", "Parameters")
			.SetOptimize(10, 40, 2);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_breakoutFactor = Param(nameof(BreakoutFactor), 0.0015m)
			.SetDisplay("Breakout Factor", "Minimum width of bands", "Parameters")
			.SetOptimize(0.0005m, 0.003m, 0.0005m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetDisplay("Take Profit (pips)", "Distance for profit target", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of working candles", "General");
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
		_breakoutFlag = false;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevClose = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_hasPrev = false;
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

		var macd = new MovingAverageConvergenceDivergence();
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, macd, rsi, ProcessCandle)
			.Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue macdValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFinal || !macdValue.IsFinal || !rsiValue.IsFinal)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
			return;

		var macdVal = macdValue.ToDecimal();
		var rsiVal = rsiValue.ToDecimal();
		var step = Security.PriceStep ?? 1m;

		var diff = upper - lower;

		if (_breakoutFlag && diff < BreakoutFactor)
			_breakoutFlag = false;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket();
			_stopPrice = middle;
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket();
			_stopPrice = middle;
		}
		else if (!_breakoutFlag && _hasPrev)
		{
			var breakout = diff >= BreakoutFactor;
			var buySignal = breakout && macdVal > 0m && rsiVal > 50m && _prevClose >= _prevUpper;
			var sellSignal = breakout && macdVal < 0m && rsiVal < 50m && _prevClose <= _prevLower;

			if (buySignal)
			{
				BuyMarket();
				_stopPrice = middle;
				_takePrice = candle.ClosePrice + TakeProfitPips * step;
				_breakoutFlag = true;
			}
			else if (sellSignal)
			{
				SellMarket();
				_stopPrice = middle;
				_takePrice = candle.ClosePrice - TakeProfitPips * step;
				_breakoutFlag = true;
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
