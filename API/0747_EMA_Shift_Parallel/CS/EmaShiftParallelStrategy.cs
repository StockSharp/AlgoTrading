using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA strategy with shifted fast lines and optional stop-loss and trailing stop for long and short sides.
/// </summary>
public class EmaShiftParallelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<int> _emaFastLong;
	private readonly StrategyParam<int> _emaSlowLong;
	private readonly StrategyParam<decimal> _downShift;
	private readonly StrategyParam<bool> _useStopLossLong;
	private readonly StrategyParam<decimal> _stopLossLong;
	private readonly StrategyParam<bool> _useTrailingLong;
	private readonly StrategyParam<decimal> _trailLongEnter;
	private readonly StrategyParam<decimal> _trailLongOffset;
	private readonly StrategyParam<bool> _closeLongOnSignal;

	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<int> _emaFastShort;
	private readonly StrategyParam<int> _emaSlowShort;
	private readonly StrategyParam<decimal> _upShift;
	private readonly StrategyParam<bool> _useStopLossShort;
	private readonly StrategyParam<decimal> _stopLossShort;
	private readonly StrategyParam<bool> _useTrailingShort;
	private readonly StrategyParam<decimal> _trailShortEnter;
	private readonly StrategyParam<decimal> _trailShortOffset;
	private readonly StrategyParam<bool> _closeShortOnSignal;

	private readonly ExponentialMovingAverage _emaFastLongInd;
	private readonly ExponentialMovingAverage _emaSlowLongInd;
	private readonly ExponentialMovingAverage _emaFastShortInd;
	private readonly ExponentialMovingAverage _emaSlowShortInd;

	private decimal _prevFastLongShifted;
	private decimal _prevSlowLong;
	private decimal _prevFastShortShifted;
	private decimal _prevSlowShort;
	private bool _isLongInit;
	private bool _isShortInit;

	private decimal _longEntryPrice;
	private decimal _longStopPrice;
	private bool _trailLongActive;
	private decimal _trailLongActivationPrice;
	private decimal _trailLongHighest;
	private decimal _trailLongStopPrice;

	private decimal _shortEntryPrice;
	private decimal _shortStopPrice;
	private bool _trailShortActive;
	private decimal _trailShortActivationPrice;
	private decimal _trailShortLowest;
	private decimal _trailShortStopPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	public int EmaFastLong
	{
		get => _emaFastLong.Value;
		set => _emaFastLong.Value = value;
	}

	public int EmaSlowLong
	{
		get => _emaSlowLong.Value;
		set => _emaSlowLong.Value = value;
	}

	public decimal DownShift
	{
		get => _downShift.Value;
		set => _downShift.Value = value;
	}

	public bool UseStopLossLong
	{
		get => _useStopLossLong.Value;
		set => _useStopLossLong.Value = value;
	}

	public decimal StopLossLong
	{
		get => _stopLossLong.Value;
		set => _stopLossLong.Value = value;
	}

	public bool UseTrailingLong
	{
		get => _useTrailingLong.Value;
		set => _useTrailingLong.Value = value;
	}

	public decimal TrailLongEnter
	{
		get => _trailLongEnter.Value;
		set => _trailLongEnter.Value = value;
	}

	public decimal TrailLongOffset
	{
		get => _trailLongOffset.Value;
		set => _trailLongOffset.Value = value;
	}

	public bool CloseLongOnSignal
	{
		get => _closeLongOnSignal.Value;
		set => _closeLongOnSignal.Value = value;
	}

	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	public int EmaFastShort
	{
		get => _emaFastShort.Value;
		set => _emaFastShort.Value = value;
	}

	public int EmaSlowShort
	{
		get => _emaSlowShort.Value;
		set => _emaSlowShort.Value = value;
	}

	public decimal UpShift
	{
		get => _upShift.Value;
		set => _upShift.Value = value;
	}

	public bool UseStopLossShort
	{
		get => _useStopLossShort.Value;
		set => _useStopLossShort.Value = value;
	}

	public decimal StopLossShort
	{
		get => _stopLossShort.Value;
		set => _stopLossShort.Value = value;
	}

	public bool UseTrailingShort
	{
		get => _useTrailingShort.Value;
		set => _useTrailingShort.Value = value;
	}

	public decimal TrailShortEnter
	{
		get => _trailShortEnter.Value;
		set => _trailShortEnter.Value = value;
	}

	public decimal TrailShortOffset
	{
		get => _trailShortOffset.Value;
		set => _trailShortOffset.Value = value;
	}

	public bool CloseShortOnSignal
	{
		get => _closeShortOnSignal.Value;
		set => _closeShortOnSignal.Value = value;
	}

	public EmaShiftParallelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Enable Long", "Enable long trades", "Long");
		_emaFastLong = Param(nameof(EmaFastLong), 20)
		.SetDisplay("Fast EMA Long", "Fast EMA length for long", "Long")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		_emaSlowLong = Param(nameof(EmaSlowLong), 420)
		.SetDisplay("Slow EMA Long", "Slow EMA length for long", "Long")
		.SetCanOptimize(true)
		.SetOptimize(100, 600, 20);
		_downShift = Param(nameof(DownShift), 0.98m)
		.SetDisplay("Down Shift", "Multiplier for fast EMA", "Long");
		_useStopLossLong = Param(nameof(UseStopLossLong), true)
		.SetDisplay("Use Stop Loss Long", "Enable long stop-loss", "Long");
		_stopLossLong = Param(nameof(StopLossLong), 2m)
		.SetDisplay("Stop Loss % Long", "Stop-loss percent for long", "Long");
		_useTrailingLong = Param(nameof(UseTrailingLong), true)
		.SetDisplay("Use Trailing Long", "Enable long trailing", "Long");
		_trailLongEnter = Param(nameof(TrailLongEnter), 6m)
		.SetDisplay("Trail Enter % Long", "Activation percent for long trailing", "Long");
		_trailLongOffset = Param(nameof(TrailLongOffset), 3m)
		.SetDisplay("Trail Offset % Long", "Offset percent for long trailing", "Long");
		_closeLongOnSignal = Param(nameof(CloseLongOnSignal), true)
		.SetDisplay("Close Long On Signal", "Close long on opposite signal", "Long");

		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Enable Short", "Enable short trades", "Short");
		_emaFastShort = Param(nameof(EmaFastShort), 30)
		.SetDisplay("Fast EMA Short", "Fast EMA length for short", "Short")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);
		_emaSlowShort = Param(nameof(EmaSlowShort), 500)
		.SetDisplay("Slow EMA Short", "Slow EMA length for short", "Short")
		.SetCanOptimize(true)
		.SetOptimize(100, 700, 20);
		_upShift = Param(nameof(UpShift), 1.02m)
		.SetDisplay("Up Shift", "Multiplier for fast EMA", "Short");
		_useStopLossShort = Param(nameof(UseStopLossShort), true)
		.SetDisplay("Use Stop Loss Short", "Enable short stop-loss", "Short");
		_stopLossShort = Param(nameof(StopLossShort), 2m)
		.SetDisplay("Stop Loss % Short", "Stop-loss percent for short", "Short");
		_useTrailingShort = Param(nameof(UseTrailingShort), true)
		.SetDisplay("Use Trailing Short", "Enable short trailing", "Short");
		_trailShortEnter = Param(nameof(TrailShortEnter), 3m)
		.SetDisplay("Trail Enter % Short", "Activation percent for short trailing", "Short");
		_trailShortOffset = Param(nameof(TrailShortOffset), 1m)
		.SetDisplay("Trail Offset % Short", "Offset percent for short trailing", "Short");
		_closeShortOnSignal = Param(nameof(CloseShortOnSignal), true)
		.SetDisplay("Close Short On Signal", "Close short on opposite signal", "Short");

		_emaFastLongInd = new() { Length = EmaFastLong };
		_emaSlowLongInd = new() { Length = EmaSlowLong };
		_emaFastShortInd = new() { Length = EmaFastShort };
		_emaSlowShortInd = new() { Length = EmaSlowShort };
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastLongShifted = 0;
		_prevSlowLong = 0;
		_prevFastShortShifted = 0;
		_prevSlowShort = 0;
		_isLongInit = false;
		_isShortInit = false;
		_longEntryPrice = 0;
		_longStopPrice = 0;
		_trailLongActive = false;
		_trailLongActivationPrice = 0;
		_trailLongHighest = 0;
		_trailLongStopPrice = 0;
		_shortEntryPrice = 0;
		_shortStopPrice = 0;
		_trailShortActive = false;
		_trailShortActivationPrice = 0;
		_trailShortLowest = 0;
		_trailShortStopPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFastLongInd.Length = EmaFastLong;
		_emaSlowLongInd.Length = EmaSlowLong;
		_emaFastShortInd.Length = EmaFastShort;
		_emaSlowShortInd.Length = EmaSlowShort;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_emaFastLongInd, _emaSlowLongInd, _emaFastShortInd, _emaSlowShortInd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFastLongInd);
			DrawIndicator(area, _emaSlowLongInd);
			DrawIndicator(area, _emaFastShortInd);
			DrawIndicator(area, _emaSlowShortInd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastLong, decimal slowLong, decimal fastShort, decimal slowShort)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_emaFastLongInd.IsFormed || !_emaSlowLongInd.IsFormed || !_emaFastShortInd.IsFormed || !_emaSlowShortInd.IsFormed)
		return;

		var fastLongShifted = fastLong * DownShift;
		var fastShortShifted = fastShort * UpShift;

		var longSignal = fastLongShifted > slowLong && _prevFastLongShifted <= _prevSlowLong;
		var longClose = fastLongShifted < slowLong && _prevFastLongShifted >= _prevSlowLong;
		var shortSignal = fastShortShifted < slowShort && _prevFastShortShifted >= _prevSlowShort;
		var shortClose = fastShortShifted > slowShort && _prevFastShortShifted <= _prevSlowShort;

		_prevFastLongShifted = fastLongShifted;
		_prevSlowLong = slowLong;
		_prevFastShortShifted = fastShortShifted;
		_prevSlowShort = slowShort;

		if (!_isLongInit)
		{
			_isLongInit = true;
			return;
		}

		if (!_isShortInit)
		{
			_isShortInit = true;
			return;
		}

		var price = candle.ClosePrice;

		if (EnableLong && longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_longEntryPrice = price;
			if (UseStopLossLong)
			_longStopPrice = price * (1 - StopLossLong / 100m);
			_trailLongActive = false;
			_trailLongActivationPrice = price * (1 + TrailLongEnter / 100m);
			_trailLongHighest = 0;
		}
		else if (EnableShort && shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_shortEntryPrice = price;
			if (UseStopLossShort)
			_shortStopPrice = price * (1 + StopLossShort / 100m);
			_trailShortActive = false;
			_trailShortActivationPrice = price * (1 - TrailShortEnter / 100m);
			_trailShortLowest = 0;
		}

		if (Position > 0)
		{
			if (UseStopLossLong && price < _longStopPrice)
			{
				SellMarket(Position);
			}

			if (UseTrailingLong)
			{
				if (!_trailLongActive && price > _trailLongActivationPrice)
				{
					_trailLongActive = true;
					_trailLongHighest = price;
				}
				if (_trailLongActive)
				{
					_trailLongHighest = Math.Max(_trailLongHighest, price);
					_trailLongStopPrice = _trailLongHighest * (1 - TrailLongOffset / 100m);
					if (price < _trailLongStopPrice)
					{
						SellMarket(Position);
					}
				}
			}

			if (CloseLongOnSignal && longClose)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (UseStopLossShort && price > _shortStopPrice)
			{
				BuyMarket(-Position);
			}

			if (UseTrailingShort)
			{
				if (!_trailShortActive && price < _trailShortActivationPrice)
				{
					_trailShortActive = true;
					_trailShortLowest = price;
				}
				if (_trailShortActive)
				{
					_trailShortLowest = Math.Min(_trailShortLowest, price);
					_trailShortStopPrice = _trailShortLowest * (1 + TrailShortOffset / 100m);
					if (price > _trailShortStopPrice)
					{
						BuyMarket(-Position);
					}
				}
			}

			if (CloseShortOnSignal && shortClose)
			{
				BuyMarket(-Position);
			}
		}
	}
}
