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
/// Strategy trading Bollinger Band breakouts with RSI momentum filter.
/// Enters on close beyond bands with RSI confirmation, trails stop at middle band.
/// </summary>
public class BollingerBreakoutMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevMiddle;
	private bool _hasPrev;

	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollingerBreakoutMomentumStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 18)
			.SetDisplay("BB Length", "Bollinger Bands length", "Parameters");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Parameters");

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
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevMiddle = 0m;
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

		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
			return;

		var rsiVal = rsiValue.ToDecimal();
		var step = Security.PriceStep ?? 1m;
		var price = candle.ClosePrice;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket();
			else
				_stopPrice = Math.Max(_stopPrice, middle);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket();
			else
				_stopPrice = Math.Min(_stopPrice, middle);
		}
		else if (_hasPrev)
		{
			var buySignal = rsiVal > 50m && price > _prevUpper;
			var sellSignal = rsiVal < 50m && price < _prevLower;

			if (buySignal)
			{
				BuyMarket();
				_stopPrice = middle;
				_takePrice = price + TakeProfitPips * step;
			}
			else if (sellSignal)
			{
				SellMarket();
				_stopPrice = middle;
				_takePrice = price - TakeProfitPips * step;
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevMiddle = middle;
		_hasPrev = true;
	}
}
