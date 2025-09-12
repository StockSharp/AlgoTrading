using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VoVix DEVMA strategy.
/// Trades volatility regime shifts using deviation moving averages
/// and manages risk with ATR-based exits.
/// </summary>
public class VoVixDevmaStrategy : Strategy
{
	private readonly StrategyParam<int> _devLookback;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useAtrStops;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atrFast;
	private AverageTrueRange _atrSlow;
	private StandardDeviation _atrFastStd;
	private StandardDeviation _srcStd;
	private SimpleMovingAverage _fastDevMa;
	private SimpleMovingAverage _slowDevMa;

	private bool _initialized;
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _profitTarget;

	/// <summary>
	/// Deviation lookback period.
	/// </summary>
	public int DevLookback
	{
		get => _devLookback.Value;
		set => _devLookback.Value = value;
	}

	/// <summary>
	/// Fast DEVMA and ATR length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow DEVMA and ATR length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Use ATR-based stop-loss and take-profit.
	/// </summary>
	public bool UseAtrStops
	{
		get => _useAtrStops.Value;
		set => _useAtrStops.Value = value;
	}

	/// <summary>
	/// ATR stop-loss multiplier.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// ATR take-profit multiplier.
	/// </summary>
	public decimal AtrProfitMultiplier
	{
		get => _atrProfitMultiplier.Value;
		set => _atrProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public VoVixDevmaStrategy()
	{
		_devLookback = Param(nameof(DevLookback), 59)
			.SetRange(15, 100)
			.SetDisplay("Deviation Lookback", "Lookback for deviation calculation", "DEVMA")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 10);

		_fastLength = Param(nameof(FastLength), 20)
			.SetRange(10, 50)
			.SetDisplay("Fast Length", "Fast DEVMA and ATR length", "DEVMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_slowLength = Param(nameof(SlowLength), 60)
			.SetRange(30, 100)
			.SetDisplay("Slow Length", "Slow DEVMA and ATR length", "DEVMA")
			.SetCanOptimize(true)
			.SetOptimize(40, 100, 10);

		_useAtrStops = Param(nameof(UseAtrStops), true)
			.SetDisplay("Use ATR Stops", "Use ATR-based exits", "Risk");

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR SL Mult", "ATR stop-loss multiplier", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_atrProfitMultiplier = Param(nameof(AtrProfitMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "ATR take-profit multiplier", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_initialized = false;
		_prevFast = 0m;
		_prevSlow = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_profitTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atrFast = new AverageTrueRange { Length = FastLength };
		_atrSlow = new AverageTrueRange { Length = SlowLength };
		_atrFastStd = new StandardDeviation { Length = DevLookback };
		_srcStd = new StandardDeviation { Length = DevLookback };
		_fastDevMa = new SimpleMovingAverage { Length = FastLength };
		_slowDevMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atrFast, _atrSlow, _atrFastStd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastDevMa);
			DrawIndicator(area, _slowDevMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrFastValue, decimal atrSlowValue, decimal atrFastStdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_atrFast.IsFormed || !_atrSlow.IsFormed || !_atrFastStd.IsFormed)
			return;

		if (atrFastStdValue == 0m)
			return;

		var src = (atrFastValue - atrSlowValue) / atrFastStdValue;
		var devValue = _srcStd.Process(src);
		if (!devValue.IsFinal)
			return;

		var dev = devValue.GetValue<decimal>();
		var fastValue = _fastDevMa.Process(dev);
		var slowValue = _slowDevMa.Process(dev);
		if (!fastValue.IsFinal || !slowValue.IsFinal)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var bullCross = _prevFast <= _prevSlow && fast > slow;
		var bearCross = _prevFast >= _prevSlow && fast < slow;

		if (bullCross && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;

			if (UseAtrStops)
			{
				_stopPrice = candle.ClosePrice - atrFastValue * AtrStopMultiplier;
				_profitTarget = candle.ClosePrice + atrFastValue * AtrProfitMultiplier;
			}
		}
		else if (bearCross && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;

			if (UseAtrStops)
			{
				_stopPrice = candle.ClosePrice + atrFastValue * AtrStopMultiplier;
				_profitTarget = candle.ClosePrice - atrFastValue * AtrProfitMultiplier;
			}
		}
		else if (UseAtrStops && Position != 0)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _profitTarget)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _profitTarget)
					BuyMarket(Math.Abs(Position));
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
