using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triangle breakout strategy with TP/SL and EMA filter.
/// Uses pivot-based triangle and trades breakouts to the upside.
/// </summary>
public class TriangleBreakoutTpSlEmaFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaFast;
	private readonly StrategyParam<int> _emaSlow;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferIndex;
	private int _bufferCount;
	private int _barIndex;
	private decimal? _lastTop;
	private decimal? _prevTop;
	private int _lastTopIndex;
	private int _prevTopIndex;
	private decimal? _lastBottom;
	private decimal? _prevBottom;
	private int _lastBottomIndex;
	private int _prevBottomIndex;
	private decimal _stopPrice;
	private decimal _takePrice;

	private ExponentialMovingAverage _ema20 = null!;
	private ExponentialMovingAverage _ema50 = null!;

	/// <summary>
	/// Pivot length for triangle detection.
	/// </summary>
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Use EMA filter.
	/// </summary>
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int EmaFast { get => _emaFast.Value; set => _emaFast.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int EmaSlow { get => _emaSlow.Value; set => _emaSlow.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="TriangleBreakoutTpSlEmaFilterStrategy"/>.
	/// </summary>
	public TriangleBreakoutTpSlEmaFilterStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars on each side for pivot detection", "General")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 1.5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true);

		_useEmaFilter = Param(nameof(UseEmaFilter), true)
			.SetDisplay("Use EMA Filter", "Require price above EMAs", "General");

		_emaFast = Param(nameof(EmaFast), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA length", "General");

		_emaSlow = Param(nameof(EmaSlow), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA length", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_bufferIndex = 0;
		_bufferCount = 0;
		_barIndex = 0;
		_lastTop = null;
		_prevTop = null;
		_lastTopIndex = 0;
		_prevTopIndex = 0;
		_lastBottom = null;
		_prevBottom = null;
		_lastBottomIndex = 0;
		_prevBottomIndex = 0;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var size = PivotLength * 2 + 1;
		_highBuffer = new decimal[size];
		_lowBuffer = new decimal[size];
		_bufferIndex = 0;
		_bufferCount = 0;
		_barIndex = 0;
		_ema20 = new ExponentialMovingAverage { Length = EmaFast };
		_ema50 = new ExponentialMovingAverage { Length = EmaSlow };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema20, _ema50, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema20);
			DrawIndicator(area, _ema50);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var size = _highBuffer.Length;
		_highBuffer[_bufferIndex] = candle.HighPrice;
		_lowBuffer[_bufferIndex] = candle.LowPrice;
		_bufferIndex = (_bufferIndex + 1) % size;
		if (_bufferCount < size)
		{
			_bufferCount++;
			_barIndex++;
			return;
		}
		var center = (_bufferIndex + size - PivotLength - 1) % size;
		var centerHigh = _highBuffer[center];
		var centerLow = _lowBuffer[center];
		var isPivotHigh = true;
		var isPivotLow = true;
		for (var i = 0; i < size; i++)
		{
			if (i == center)
				continue;
			if (isPivotHigh && _highBuffer[i] >= centerHigh)
				isPivotHigh = false;
			if (isPivotLow && _lowBuffer[i] <= centerLow)
				isPivotLow = false;
			if (!isPivotHigh && !isPivotLow)
				break;
		}
		if (isPivotHigh)
		{
			_prevTop = _lastTop;
			_prevTopIndex = _lastTopIndex;
			_lastTop = centerHigh;
			_lastTopIndex = _barIndex - PivotLength - 1;
		}
		if (isPivotLow)
		{
			_prevBottom = _lastBottom;
			_prevBottomIndex = _lastBottomIndex;
			_lastBottom = centerLow;
			_lastBottomIndex = _barIndex - PivotLength - 1;
		}
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_barIndex++;
			return;
		}
		if (_lastTop is decimal top2 && _prevTop is decimal top1 && _lastBottom is decimal bot2 && _prevBottom is decimal bot1)
		{
			var slopeTop = (top2 - top1) / (_lastTopIndex - _prevTopIndex);
			var slopeBottom = (bot2 - bot1) / (_lastBottomIndex - _prevBottomIndex);
			var interceptTop = top2 - slopeTop * _lastTopIndex;
			var interceptBottom = bot2 - slopeBottom * _lastBottomIndex;
			var expectedTop = slopeTop * _barIndex + interceptTop;
			var expectedBottom = slopeBottom * _barIndex + interceptBottom;
			var breakoutUp = candle.ClosePrice > expectedTop;
			var emaOk = !UseEmaFilter || (candle.ClosePrice > emaFast && candle.ClosePrice > emaSlow);
			if (breakoutUp && emaOk && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_stopPrice = candle.ClosePrice * (1m - StopLossPercent / 100m);
				_takePrice = candle.ClosePrice * (1m + TakeProfitPercent / 100m);
			}
			else if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
					SellMarket(Position);
			}
		}
		_barIndex++;
	}
}
