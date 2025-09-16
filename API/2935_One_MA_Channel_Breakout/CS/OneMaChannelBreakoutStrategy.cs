using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// One moving average channel breakout strategy.
/// Tracks the distance between price and a shifted moving average channel and trades breakouts.
/// </summary>
public class OneMaChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _maBarShift;
	private readonly StrategyParam<int> _priceBarShift;
	private readonly StrategyParam<decimal> _channelHighPips;
	private readonly StrategyParam<decimal> _channelLowPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _movingAverage = null!;
	private decimal[] _maBuffer = Array.Empty<decimal>();
	private CandleSnapshot[] _priceBuffer = Array.Empty<CandleSnapshot>();
	private int _bufferCount;
	private int _bufferSize;
	private int _maxShift;
	private decimal _pipValue;

	/// <summary>
	/// Moving average calculation period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Type of moving average used in calculations.
	/// </summary>
	public MaMethod MaMethodParam
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source for moving average input.
	/// </summary>
	public AppliedPrice AppliedPriceType
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Bar index used to read the moving average value.
	/// </summary>
	public int MaBarShift
	{
		get => _maBarShift.Value;
		set => _maBarShift.Value = value;
	}

	/// <summary>
	/// Bar index used to read candle prices.
	/// </summary>
	public int PriceBarShift
	{
		get => _priceBarShift.Value;
		set => _priceBarShift.Value = value;
	}

	/// <summary>
	/// Upper channel distance in pips above the moving average.
	/// </summary>
	public decimal ChannelHighPips
	{
		get => _channelHighPips.Value;
		set => _channelHighPips.Value = value;
	}

	/// <summary>
	/// Lower channel distance in pips below the moving average.
	/// </summary>
	public decimal ChannelLowPips
	{
		get => _channelLowPips.Value;
		set => _channelLowPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Order size in strategy volume units.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OneMaChannelBreakoutStrategy"/> class.
	/// </summary>
	public OneMaChannelBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 44)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_maShift = Param(nameof(MaShift), 4)
			.SetDisplay("MA Shift", "Horizontal displacement in bars", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_maMethod = Param(nameof(MaMethodParam), MaMethod.Ema)
			.SetDisplay("MA Method", "Moving average calculation method", "Indicator");

		_appliedPrice = Param(nameof(AppliedPriceType), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source for MA", "Indicator");

		_maBarShift = Param(nameof(MaBarShift), 0)
			.SetDisplay("MA Bar", "Bar index for MA reading", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_priceBarShift = Param(nameof(PriceBarShift), 0)
			.SetDisplay("Price Bar", "Bar index for candle data", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_channelHighPips = Param(nameof(ChannelHighPips), 14m)
			.SetGreaterThanZero()
			.SetDisplay("Upper Channel (pips)", "Channel ceiling distance", "Channel")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 1m);

		_channelLowPips = Param(nameof(ChannelLowPips), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Lower Channel (pips)", "Channel floor distance", "Channel")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_stopLossPips = Param(nameof(StopLossPips), 100m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Executed order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_bufferCount = 0;
		_maBuffer = Array.Empty<decimal>();
		_priceBuffer = Array.Empty<CandleSnapshot>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Synchronize strategy order volume with external parameter.
		Volume = TradeVolume;

		_movingAverage = CreateMovingAverage(MaMethodParam, MaPeriod);

		// Prepare circular buffers to access historical MA and candle data.
		var maxShift = Math.Max(MaBarShift + MaShift, PriceBarShift);
		_bufferSize = Math.Max(maxShift + 1, 1);
		_maxShift = maxShift;
		_maBuffer = new decimal[_bufferSize];
		_priceBuffer = new CandleSnapshot[_bufferSize];
		_bufferCount = 0;

		var priceStep = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;
		var pipMultiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
		_pipValue = priceStep * pipMultiplier;

		// Translate pip-based settings into absolute price distances.
		var stopLossDistance = StopLossPips * _pipValue;
		var takeProfitDistance = TakeProfitPips * _pipValue;

		StartProtection(
			stopLoss: StopLossPips > 0 ? new Unit(stopLossDistance, UnitTypes.Absolute) : null,
			takeProfit: TakeProfitPips > 0 ? new Unit(takeProfitDistance, UnitTypes.Absolute) : null);

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

		var inputPrice = GetAppliedPrice(candle);
		var maValue = _movingAverage.Process(inputPrice).ToDecimal();

		AddToBuffers(candle, maValue);

		if (!_movingAverage.IsFormed)
			return;

		if (_bufferCount <= _maxShift)
			return;

		// Retrieve historical references according to configured shifts.
		var maIndex = MaBarShift + MaShift;
		var priceIndex = PriceBarShift;
		var maReference = GetMaValue(maIndex);
		var priceSnapshot = GetPriceSnapshot(priceIndex);

		var upperChannel = maReference + ChannelHighPips * _pipValue;
		var lowerChannel = maReference - ChannelLowPips * _pipValue;

		var bullishBreakout = priceSnapshot.Low > maReference && priceSnapshot.Low < upperChannel && priceSnapshot.Open > upperChannel;
		var bearishBreakout = priceSnapshot.High < maReference && priceSnapshot.High > lowerChannel && priceSnapshot.Open < lowerChannel;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume + Math.Abs(Position);

		if (bullishBreakout && Position <= 0)
		{
			// Enter long after bullish breakout above the channel ceiling.
			BuyMarket(volume);
		}
		else if (bearishBreakout && Position >= 0)
		{
			// Enter short after bearish breakout below the channel floor.
			SellMarket(volume);
		}
	}

	private void AddToBuffers(ICandleMessage candle, decimal maValue)
	{
		// Store the latest candle and MA value in a fixed-size sliding window.
		var snapshot = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);

		if (_bufferCount < _bufferSize)
		{
			_maBuffer[_bufferCount] = maValue;
			_priceBuffer[_bufferCount] = snapshot;
			_bufferCount++;
		}
		else if (_bufferSize > 0)
		{
			Array.Copy(_maBuffer, 1, _maBuffer, 0, _bufferSize - 1);
			Array.Copy(_priceBuffer, 1, _priceBuffer, 0, _bufferSize - 1);
			_maBuffer[^1] = maValue;
			_priceBuffer[^1] = snapshot;
		}
	}

	private decimal GetMaValue(int shift)
	{
		// Access MA history using reverse indexing where shift 0 equals the latest value.
		var index = _bufferCount - 1 - shift;
		if (index < 0)
			throw new InvalidOperationException("Insufficient MA history for requested shift.");
		return _maBuffer[index];
	}

	private CandleSnapshot GetPriceSnapshot(int shift)
	{
		// Access candle history using the same indexing convention as the MA buffer.
		var index = _bufferCount - 1 - shift;
		if (index < 0)
			throw new InvalidOperationException("Insufficient price history for requested shift.");
		return _priceBuffer[index];
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int length)
	{
		return method switch
		{
			MaMethod.Sma => new SimpleMovingAverage { Length = length },
			MaMethod.Smma => new SmoothedMovingAverage { Length = length },
			MaMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPriceType switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	public enum MaMethod
	{
		Sma = 0,
		Ema = 1,
		Smma = 2,
		Lwma = 3
	}

	public enum AppliedPrice
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}
}
