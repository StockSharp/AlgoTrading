using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the VR-BUCH MetaTrader expert advisor logic.
/// Combines two configurable moving averages with a price filter to generate entries.
/// Closes opposite positions before opening new ones, mirroring the original MQL implementation.
/// </summary>
public class VrBuchStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<CandlePrice> _fastPrice;
	private readonly StrategyParam<MovingAverageMethod> _fastMethod;

	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<CandlePrice> _slowPrice;
	private readonly StrategyParam<MovingAverageMethod> _slowMethod;

	private readonly StrategyParam<CandlePrice> _signalPrice;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _fastMa = null!;
	private MovingAverage _slowMa = null!;

	private decimal[] _fastBuffer = Array.Empty<decimal>();
	private int _fastIndex;
	private int _fastCount;

	private decimal[] _slowBuffer = Array.Empty<decimal>();
	private int _slowIndex;
	private int _slowCount;

	/// <summary>
	/// Initializes a new instance of <see cref="VrBuchStrategy"/>.
	/// </summary>
	public VrBuchStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 33)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast moving average period", "Fast MA");

		_fastShift = Param(nameof(FastShift), 3)
			.SetRange(0, 1000)
			.SetDisplay("Fast Shift", "Number of candles to shift the fast MA value", "Fast MA");

		_fastPrice = Param(nameof(FastPrice), CandlePrice.Weighted)
			.SetDisplay("Fast Price", "Price source for the fast MA", "Fast MA");

		_fastMethod = Param(nameof(FastMethod), MovingAverageMethod.Simple)
			.SetDisplay("Fast Method", "Smoothing method for the fast MA", "Fast MA");

		_slowPeriod = Param(nameof(SlowPeriod), 90)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow moving average period", "Slow MA");

		_slowShift = Param(nameof(SlowShift), 1)
			.SetRange(0, 1000)
			.SetDisplay("Slow Shift", "Number of candles to shift the slow MA value", "Slow MA");

		_slowPrice = Param(nameof(SlowPrice), CandlePrice.Weighted)
			.SetDisplay("Slow Price", "Price source for the slow MA", "Slow MA");

		_slowMethod = Param(nameof(SlowMethod), MovingAverageMethod.Simple)
			.SetDisplay("Slow Method", "Smoothing method for the slow MA", "Slow MA");

		_signalPrice = Param(nameof(SignalPrice), CandlePrice.Close)
			.SetDisplay("Signal Price", "Price source used for confirmations", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");

		Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume used for entries", "Trading");
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the fast moving average.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Price source for the fast moving average.
	/// </summary>
	public CandlePrice FastPrice
	{
		get => _fastPrice.Value;
		set => _fastPrice.Value = value;
	}

	/// <summary>
	/// Smoothing method for the fast moving average.
	/// </summary>
	public MovingAverageMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the slow moving average.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Price source for the slow moving average.
	/// </summary>
	public CandlePrice SlowPrice
	{
		get => _slowPrice.Value;
		set => _slowPrice.Value = value;
	}

	/// <summary>
	/// Smoothing method for the slow moving average.
	/// </summary>
	public MovingAverageMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	/// <summary>
	/// Price source used for the price confirmation.
	/// </summary>
	public CandlePrice SignalPrice
	{
		get => _signalPrice.Value;
		set => _signalPrice.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_fastBuffer = Array.Empty<decimal>();
		_fastIndex = 0;
		_fastCount = 0;

		_slowBuffer = Array.Empty<decimal>();
		_slowIndex = 0;
		_slowCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(FastMethod, FastPeriod, FastPrice);
		_slowMa = CreateMovingAverage(SlowMethod, SlowPeriod, SlowPrice);

		_fastBuffer = new decimal[Math.Max(1, FastShift + 1)];
		_fastIndex = 0;
		_fastCount = 0;

		_slowBuffer = new decimal[Math.Max(1, SlowShift + 1)];
		_slowIndex = 0;
		_slowCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to accumulate enough history.
		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		var fastReady = UpdateShiftedValue(fastValue, _fastBuffer, ref _fastIndex, ref _fastCount, FastShift, out var fastShifted);
		var slowReady = UpdateShiftedValue(slowValue, _slowBuffer, ref _slowIndex, ref _slowCount, SlowShift, out var slowShifted);

		if (!fastReady || !slowReady)
			return;

		var price = GetPrice(candle, SignalPrice);

		var buySignal = fastShifted > slowShifted && price > fastShifted;
		var sellSignal = fastShifted < slowShifted && price < fastShifted;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (buySignal)
			{
				// Close existing short positions before looking for new longs.
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				return;
			}

			if (sellSignal)
			{
				// Close existing long positions before looking for new shorts.
				if (Position > 0)
					SellMarket(Position);

				return;
			}

			return;
		}

		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume);
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume);
		}
	}

	private static bool UpdateShiftedValue(decimal currentValue, decimal[] buffer, ref int index, ref int count, int shift, out decimal shiftedValue)
	{
		buffer[index] = currentValue;

		if (count < buffer.Length)
			count++;

		index++;
		if (index >= buffer.Length)
			index = 0;

		if (count < buffer.Length)
		{
			shiftedValue = 0m;
			return false;
		}

		var offsetIndex = index - 1 - shift;
		while (offsetIndex < 0)
			offsetIndex += buffer.Length;

		shiftedValue = buffer[offsetIndex];
		return true;
	}

	private static MovingAverage CreateMovingAverage(MovingAverageMethod method, int period, CandlePrice price)
	{
		var length = Math.Max(1, period);

		MovingAverage indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};

		indicator.CandlePrice = price;

		return indicator;
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice price)
	{
		return price switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}

/// <summary>
/// Moving average smoothing methods mirroring the MetaTrader options.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed (RMA/SMMA) moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Weighted
}
