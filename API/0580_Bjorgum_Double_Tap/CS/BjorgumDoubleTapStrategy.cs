using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bjorgum Double Tap pattern strategy.
/// Detects double top and double bottom formations and trades breakouts.
/// </summary>
public class BjorgumDoubleTapStrategy : Strategy
{
	private readonly StrategyParam<bool> _detectBottoms;
	private readonly StrategyParam<bool> _detectTops;
	private readonly StrategyParam<bool> _flipTrades;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _pivotTolerance;
	private readonly StrategyParam<decimal> _targetFib;
	private readonly StrategyParam<decimal> _stopFib;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal? _firstTop;
	private decimal? _secondTop;
	private decimal _necklineTop;
	private int _barsAfterFirstTop;
	private bool _topReady;

	private decimal? _firstBottom;
	private decimal? _secondBottom;
	private decimal _necklineBottom;
	private int _barsAfterFirstBottom;
	private bool _bottomReady;

	private decimal? _stopPrice;
	private decimal? _targetPrice;

	public bool DetectBottoms
	{
		get => _detectBottoms.Value;
		set => _detectBottoms.Value = value;
	}
	public bool DetectTops
	{
		get => _detectTops.Value;
		set => _detectTops.Value = value;
	}
	public bool FlipTrades
	{
		get => _flipTrades.Value;
		set => _flipTrades.Value = value;
	}
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}
	public decimal PivotTolerance
	{
		get => _pivotTolerance.Value;
		set => _pivotTolerance.Value = value;
	}
	public decimal TargetFib
	{
		get => _targetFib.Value;
		set => _targetFib.Value = value;
	}
	public decimal StopLossFib
	{
		get => _stopFib.Value;
		set => _stopFib.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BjorgumDoubleTapStrategy()
	{
		_detectBottoms =
			Param(nameof(DetectBottoms), true).SetDisplay("Detect Bottoms", "Detect double bottom patterns", "General");

		_detectTops =
			Param(nameof(DetectTops), true).SetDisplay("Detect Tops", "Detect double top patterns", "General");

		_flipTrades =
			Param(nameof(FlipTrades), true).SetDisplay("Flip Trades", "Allow reversing existing position", "General");

		_pivotLength = Param(nameof(PivotLength), 50)
						   .SetGreaterThanZero()
						   .SetDisplay("Pivot Length", "Bars used to search pivots", "Pattern");

		_pivotTolerance = Param(nameof(PivotTolerance), 15m)
							  .SetDisplay("Pivot Tolerance %", "Allowed percent difference between pivots", "Pattern")
							  .SetCanOptimize(true);

		_targetFib = Param(nameof(TargetFib), 100m)
						 .SetDisplay("Target Fib %", "Target extension percentage", "Risk Management")
						 .SetCanOptimize(true);

		_stopFib = Param(nameof(StopLossFib), 0m)
					   .SetDisplay("Stop Fib %", "Stop loss extension percentage", "Risk Management")
					   .SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
						  .SetDisplay("Candle Type", "Time frame for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_firstTop = null;
		_secondTop = null;
		_necklineTop = 0m;
		_barsAfterFirstTop = 0;
		_topReady = false;

		_firstBottom = null;
		_secondBottom = null;
		_necklineBottom = 0m;
		_barsAfterFirstBottom = 0;
		_bottomReady = false;

		_stopPrice = null;
		_targetPrice = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = PivotLength };
		_lowest = new Lowest { Length = PivotLength };

		StartProtection(new(), new());

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManagePosition(candle);

		var highest = _highest.Process(candle).ToDecimal();
		var lowest = _lowest.Process(candle).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (DetectTops)
			ProcessTopPattern(candle, highest);

		if (DetectBottoms)
			ProcessBottomPattern(candle, lowest);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = _targetPrice = null;
			}
			else if (_targetPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				_stopPrice = _targetPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				_stopPrice = _targetPrice = null;
			}
			else if (_targetPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(-Position);
				_stopPrice = _targetPrice = null;
			}
		}
	}

	private void ProcessTopPattern(ICandleMessage candle, decimal highest)
	{
		if (_firstTop == null)
		{
			if (candle.HighPrice == highest)
			{
				_firstTop = candle.HighPrice;
				_necklineTop = decimal.MaxValue;
				_barsAfterFirstTop = 0;
			}
			return;
		}

		if (!_topReady)
		{
			_barsAfterFirstTop++;
			_necklineTop = Math.Min(_necklineTop, candle.LowPrice);

			if (candle.HighPrice == highest)
			{
				var height = _firstTop.Value - _necklineTop;
				if (height > 0m && Math.Abs(candle.HighPrice - _firstTop.Value) <= height * PivotTolerance / 100m)
				{
					_secondTop = candle.HighPrice;
					_topReady = true;
				}
				else
				{
					_firstTop = candle.HighPrice;
					_necklineTop = decimal.MaxValue;
					_barsAfterFirstTop = 0;
				}
			}
			else if (_barsAfterFirstTop > PivotLength * 3)
			{
				ResetTop();
			}
		}
		else
		{
			var height = Math.Max(_firstTop.Value, _secondTop!.Value) - _necklineTop;

			if (candle.ClosePrice < _necklineTop)
			{
				if (Position <= 0 || FlipTrades)
				{
					_stopPrice = _secondTop - height * StopLossFib / 100m;
					_targetPrice = _necklineTop - height * TargetFib / 100m;
					var volume = Volume + (Position > 0 ? Position : 0m);
					SellMarket(volume);
				}
				ResetTop();
			}
			else if (candle.HighPrice > Math.Max(_firstTop.Value, _secondTop.Value))
			{
				ResetTop();
			}
		}
	}

	private void ProcessBottomPattern(ICandleMessage candle, decimal lowest)
	{
		if (_firstBottom == null)
		{
			if (candle.LowPrice == lowest)
			{
				_firstBottom = candle.LowPrice;
				_necklineBottom = decimal.MinValue;
				_barsAfterFirstBottom = 0;
			}
			return;
		}

		if (!_bottomReady)
		{
			_barsAfterFirstBottom++;
			_necklineBottom = Math.Max(_necklineBottom, candle.HighPrice);

			if (candle.LowPrice == lowest)
			{
				var height = _necklineBottom - _firstBottom.Value;
				if (height > 0m && Math.Abs(candle.LowPrice - _firstBottom.Value) <= height * PivotTolerance / 100m)
				{
					_secondBottom = candle.LowPrice;
					_bottomReady = true;
				}
				else
				{
					_firstBottom = candle.LowPrice;
					_necklineBottom = decimal.MinValue;
					_barsAfterFirstBottom = 0;
				}
			}
			else if (_barsAfterFirstBottom > PivotLength * 3)
			{
				ResetBottom();
			}
		}
		else
		{
			var height = _necklineBottom - Math.Min(_firstBottom.Value, _secondBottom!.Value);

			if (candle.ClosePrice > _necklineBottom)
			{
				if (Position >= 0 || FlipTrades)
				{
					_stopPrice = _secondBottom + height * StopLossFib / 100m;
					_targetPrice = _necklineBottom + height * TargetFib / 100m;
					var volume = Volume + (Position < 0 ? -Position : 0m);
					BuyMarket(volume);
				}
				ResetBottom();
			}
			else if (candle.LowPrice < Math.Min(_firstBottom.Value, _secondBottom.Value))
			{
				ResetBottom();
			}
		}
	}

	private void ResetTop()
	{
		_firstTop = null;
		_secondTop = null;
		_necklineTop = 0m;
		_barsAfterFirstTop = 0;
		_topReady = false;
	}

	private void ResetBottom()
	{
		_firstBottom = null;
		_secondBottom = null;
		_necklineBottom = 0m;
		_barsAfterFirstBottom = 0;
		_bottomReady = false;
	}
}
