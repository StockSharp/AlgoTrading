using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend Enhanced Pivot Reversal strategy.
/// Places stop orders at pivot levels against SuperTrend direction.
/// </summary>
public class SuperTrendEnhancedPivotReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _leftBars;
	private readonly StrategyParam<int> _rightBars;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _superTrend;
	private decimal[] _highs = Array.Empty<decimal>();
	private decimal[] _lows = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal? _pivotHigh;
	private decimal? _pivotLow;
	private bool _longSignal;
	private bool _shortSignal;
	private bool _buyOrderPlaced;
	private bool _sellOrderPlaced;
	private decimal? _longStop;
	private decimal? _shortStop;

	private int TotalBars => LeftBars + RightBars + 1;

	/// <summary>
	/// Bars to the left of the pivot.
	/// </summary>
	public int LeftBars
	{
		get => _leftBars.Value;
		set => _leftBars.Value = value;
	}

	/// <summary>
	/// Bars to the right of the pivot.
	/// </summary>
	public int RightBars
	{
		get => _rightBars.Value;
		set => _rightBars.Value = value;
	}

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Multiplier for SuperTrend.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Stop-loss percent from pivot price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public Sides? TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SuperTrendEnhancedPivotReversalStrategy"/>.
	/// </summary>
	public SuperTrendEnhancedPivotReversalStrategy()
	{
		_leftBars = Param(nameof(LeftBars), 6)
		.SetDisplay("Left Bars", "Bars left of pivot", "Pivot");

		_rightBars = Param(nameof(RightBars), 3)
		.SetDisplay("Right Bars", "Bars right of pivot", "Pivot");

		_atrLength = Param(nameof(AtrLength), 5)
		.SetDisplay("ATR Length", "ATR period for SuperTrend", "SuperTrend");

		_factor = Param(nameof(Factor), 2.618m)
		.SetDisplay("Factor", "Multiplier for SuperTrend", "SuperTrend");

		_stopLossPercent = Param(nameof(StopLossPercent), 20m)
		.SetDisplay("Stop Loss (%)", "Percent stop-loss from pivot", "Risk");

		_tradeDirection = Param<Sides?>(nameof(TradeDirection), null)
		.SetDisplay("Trade Direction", "Allowed trade direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bufferCount = 0;
		_pivotHigh = null;
		_pivotLow = null;
		_longSignal = false;
		_shortSignal = false;
		_buyOrderPlaced = false;
		_sellOrderPlaced = false;
		_longStop = null;
		_shortStop = null;
		_highs = Array.Empty<decimal>();
		_lows = Array.Empty<decimal>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new SuperTrend { Length = AtrLength, Multiplier = Factor };

		_highs = new decimal[TotalBars];
		_lows = new decimal[TotalBars];
		_bufferCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_superTrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawOwnTrades(area);
		}
	}

	private void UpdateBuffers(decimal high, decimal low)
	{
		if (_bufferCount < _highs.Length)
		{
			_highs[_bufferCount] = high;
			_lows[_bufferCount] = low;
			_bufferCount++;
		}
		else
		{
			for (var i = 0; i < _highs.Length - 1; i++)
			{
				_highs[i] = _highs[i + 1];
				_lows[i] = _lows[i + 1];
			}

			_highs[^1] = high;
			_lows[^1] = low;
		}
	}

	private bool DetectPivotHigh(out decimal price)
	{
		price = 0m;
		if (_bufferCount < _highs.Length)
			return false;

		var idx = _highs.Length - 1 - RightBars;
		var center = _highs[idx];

		for (var i = 0; i < _highs.Length; i++)
		{
			if (i == idx)
				continue;

			if (_highs[i] >= center)
				return false;
		}

		price = center;
		return true;
	}

	private bool DetectPivotLow(out decimal price)
	{
		price = 0m;
		if (_bufferCount < _lows.Length)
			return false;

		var idx = _lows.Length - 1 - RightBars;
		var center = _lows[idx];

		for (var i = 0; i < _lows.Length; i++)
		{
			if (i == idx)
				continue;

			if (_lows[i] <= center)
				return false;
		}

		price = center;
		return true;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBuffers(candle.HighPrice, candle.LowPrice);

		if (DetectPivotHigh(out var ph))
		{
			_pivotHigh = ph;
			_longSignal = true;
			_buyOrderPlaced = false;
			CancelActiveOrders();
		}

		if (DetectPivotLow(out var pl))
		{
			_pivotLow = pl;
			_shortSignal = true;
			_sellOrderPlaced = false;
			CancelActiveOrders();
		}

		if (_longSignal && _pivotHigh is decimal hp && candle.HighPrice > hp)
		{
			_longSignal = false;
			_buyOrderPlaced = false;
			CancelActiveOrders();
		}

		if (_shortSignal && _pivotLow is decimal lp && candle.LowPrice < lp)
		{
			_shortSignal = false;
			_sellOrderPlaced = false;
			CancelActiveOrders();
		}

		var st = (SuperTrendIndicatorValue)stVal;
		var isUp = st.IsUpTrend;
		var step = Security.PriceStep ?? 1m;

		if (_longSignal && !isUp && (TradeDirection == Sides.Buy || TradeDirection == null) && !_buyOrderPlaced && Position <= 0 && _pivotHigh is decimal phVal)
		{
			BuyStop(Volume + Math.Abs(Position), phVal + step);
			_buyOrderPlaced = true;
		}

		if (_shortSignal && isUp && (TradeDirection == Sides.Sell || TradeDirection == null) && !_sellOrderPlaced && Position >= 0 && _pivotLow is decimal plVal)
		{
			SellStop(Volume + Math.Abs(Position), plVal - step);
			_sellOrderPlaced = true;
		}

		if (Position > 0)
		{
			if (_longStop is null && _pivotHigh is decimal hp2)
				_longStop = hp2 * (1m - StopLossPercent / 100m);

			if (_longStop is decimal ls && candle.LowPrice <= ls)
			{
				SellMarket(Position);
				_longStop = null;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop is null && _pivotLow is decimal lp2)
				_shortStop = lp2 * (1m + StopLossPercent / 100m);

			if (_shortStop is decimal ss && candle.HighPrice >= ss)
			{
				BuyMarket(-Position);
				_shortStop = null;
			}
		}
		else
		{
			_longStop = null;
			_shortStop = null;
		}

		if (TradeDirection == Sides.Buy && !isUp && Position > 0)
			SellMarket(Position);
		else if (TradeDirection == Sides.Sell && isUp && Position < 0)
			BuyMarket(-Position);
	}
}