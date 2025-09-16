using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on two SMA crossovers with ATR stop loss.
/// Buys when the fast SMA rises above the slow SMA and sells on opposite cross.
/// A stop loss is placed one ATR away from the entry price.
/// </summary>
public class CrossMAStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _stopPrice;
	private bool _isFastBelowSlow;
	private bool _isInitialized;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for stop calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CrossMAStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA Period", "Period of the fast SMA", "Parameters");

		_slowPeriod = Param(nameof(SlowPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA Period", "Period of the slow SMA", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Period of ATR for stop calculation", "Risk");

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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

		_stopPrice = null;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SimpleMovingAverage
		{
			Length = FastPeriod
		};

		var slowSma = new SimpleMovingAverage
		{
			Length = SlowPeriod
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(fastSma, slowSma, atr, ProcessCandle)
		.Start();

		StartProtection();
	}

	/// <summary>
	/// Process candle and indicator values.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
			_isFastBelowSlow = fast < slow;
			_isInitialized = true;
			return;
		}

		if (Position > 0 && _stopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Position);
			_stopPrice = null;
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(-Position);
			_stopPrice = null;
		}

		var fastBelowSlow = fast < slow;

		if (_isFastBelowSlow && !fastBelowSlow && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_stopPrice = candle.ClosePrice - atr;
		}
		else if (!_isFastBelowSlow && fastBelowSlow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_stopPrice = candle.ClosePrice + atr;
		}

		_isFastBelowSlow = fastBelowSlow;
	}
}
