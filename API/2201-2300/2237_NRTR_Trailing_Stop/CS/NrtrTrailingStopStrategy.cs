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
/// NRTR trailing stop strategy based on the NRTR indicator.
/// Opens long when trend turns up and short when trend turns down.
/// Includes optional stop-loss and take-profit.
/// </summary>
public class NrtrTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _digitsShift;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _price;
	private decimal _value;
	private int _trend;
	private bool _isInitialized;

	/// <summary>
	/// Number of bars for average range calculation.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Digits adjustment for indicator sensitivity.
	/// </summary>
	public int DigitsShift
	{
		get => _digitsShift.Value;
		set => _digitsShift.Value = value;
	}

	/// <summary>
	/// Take-profit in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop-loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NrtrTrailingStopStrategy"/>.
	/// </summary>
	public NrtrTrailingStopStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("NRTR Length", "Number of bars for average range", "Indicator")
			
			.SetOptimize(5, 20, 5);

		_digitsShift = Param(nameof(DigitsShift), 0)
			.SetDisplay("Digits Shift", "Adjustment for price digits", "Indicator");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pts)", "Take profit level in points", "Risk")
			
			.SetOptimize(500m, 3000m, 500m);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pts)", "Stop loss level in points", "Risk")
			
			.SetOptimize(500m, 3000m, 500m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
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
		_price = 0m;
		_value = 0m;
		_trend = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = Length };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();
		var dK = atr / Length * (decimal)Math.Pow(10, -DigitsShift);

		if (!_isInitialized)
		{
			_price = candle.ClosePrice;
			_value = _price;
			_trend = 0;
			_isInitialized = true;
			return;
		}

		var isOnline = IsFormedAndOnlineAndAllowTrading();

		if (_trend >= 0)
		{
			_price = Math.Max(_price, candle.ClosePrice);
			_value = Math.Max(_value, _price * (1m - dK));
			if (candle.ClosePrice < _value)
			{
				_price = candle.ClosePrice;
				_value = _price * (1m + dK);
				_trend = -1;
				if (isOnline && Position >= 0)
					SellMarket();
			}
		}
		else
		{
			_price = Math.Min(_price, candle.ClosePrice);
			_value = Math.Min(_value, _price * (1m + dK));
			if (candle.ClosePrice > _value)
			{
				_price = candle.ClosePrice;
				_value = _price * (1m - dK);
				_trend = 1;
				if (isOnline && Position <= 0)
					BuyMarket();
			}
		}
	}
}