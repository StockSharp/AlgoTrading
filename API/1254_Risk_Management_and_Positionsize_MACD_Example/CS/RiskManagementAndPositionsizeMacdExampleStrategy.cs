using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with position sizing based on current equity.
/// Uses higher timeframe MACD and moving average trend filter.
/// </summary>
public class RiskManagementAndPositionsizeMacdExampleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialBalance;
	private readonly StrategyParam<bool> _leverageEquity;
	private readonly StrategyParam<decimal> _marginFactor;
	private readonly StrategyParam<decimal> _quantity;
	private readonly StrategyParam<MovingAverageTypeEnum> _macdMaType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _signalMaLength;
	private readonly StrategyParam<TimeSpan> _macdTimeFrame;
	private readonly StrategyParam<MovingAverageTypeEnum> _trendMaType;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<TimeSpan> _trendTimeFrame;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _macdSmooth;
	private SimpleMovingAverage? _signalSmooth;
	private SimpleMovingAverage? _trendSmooth;

	private decimal _macdValue;
	private decimal _signalValue;
	private bool _macdReady;

	private decimal _trendValue;
	private decimal _prevTrendValue;
	private bool _trendReady;

	/// <summary>
	/// Starting capital.
	/// </summary>
	public decimal InitialBalance
	{
		get => _initialBalance.Value;
		set => _initialBalance.Value = value;
	}

	/// <summary>
	/// Use equity based quantity.
	/// </summary>
	public bool LeverageEquity
	{
		get => _leverageEquity.Value;
		set => _leverageEquity.Value = value;
	}

	/// <summary>
	/// Additional equity percentage for sizing.
	/// </summary>
	public decimal MarginFactor
	{
		get => _marginFactor.Value;
		set => _marginFactor.Value = value;
	}

	/// <summary>
	/// Fixed contracts quantity.
	/// </summary>
	public decimal Quantity
	{
		get => _quantity.Value;
		set => _quantity.Value = value;
	}

	/// <summary>
	/// Moving average type for MACD.
	/// </summary>
	public MovingAverageTypeEnum MacdMaType
	{
		get => _macdMaType.Value;
		set => _macdMaType.Value = value;
	}

	/// <summary>
	/// Fast MA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow MA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Signal MA length.
	/// </summary>
	public int SignalMaLength
	{
		get => _signalMaLength.Value;
		set => _signalMaLength.Value = value;
	}

	/// <summary>
	/// MACD higher timeframe.
	/// </summary>
	public TimeSpan MacdTimeFrame
	{
		get => _macdTimeFrame.Value;
		set => _macdTimeFrame.Value = value;
	}

	/// <summary>
	/// Moving average type for trend filter.
	/// </summary>
	public MovingAverageTypeEnum TrendMaType
	{
		get => _trendMaType.Value;
		set => _trendMaType.Value = value;
	}

	/// <summary>
	/// Trend moving average length.
	/// </summary>
	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
	}

	/// <summary>
	/// Trend higher timeframe.
	/// </summary>
	public TimeSpan TrendTimeFrame
	{
		get => _trendTimeFrame.Value;
		set => _trendTimeFrame.Value = value;
	}

	/// <summary>
	/// Base candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public RiskManagementAndPositionsizeMacdExampleStrategy()
	{
		_initialBalance = Param(nameof(InitialBalance), 10000m)
							  .SetDisplay("Initial Balance", "Starting capital", "Risk Management")
							  .SetCanOptimize(true)
							  .SetOptimize(1000m, 20000m, 1000m);

		_leverageEquity = Param(nameof(LeverageEquity), true)
							  .SetDisplay("Qty based on equity %", "Use equity for position size", "Risk Management");

		_marginFactor = Param(nameof(MarginFactor), -0.5m)
							.SetDisplay("Margin Factor", "Extra equity percentage for size", "Risk Management")
							.SetCanOptimize(true)
							.SetOptimize(-0.5m, 1m, 0.5m);

		_quantity = Param(nameof(Quantity), 3.5m)
						.SetDisplay("Quantity Contracts", "Fixed contracts quantity", "Risk Management")
						.SetCanOptimize(true)
						.SetOptimize(1m, 5m, 1m);

		_macdMaType = Param(nameof(MacdMaType), MovingAverageTypeEnum.EMA)
						  .SetDisplay("MACD MA Type", "Moving average type for MACD", "MACD Settings");

		_fastMaLength = Param(nameof(FastMaLength), 11)
							.SetDisplay("Fast MA Length", "Fast moving average length", "MACD Settings")
							.SetCanOptimize(true)
							.SetOptimize(5, 15, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 26)
							.SetDisplay("Slow MA Length", "Slow moving average length", "MACD Settings")
							.SetCanOptimize(true)
							.SetOptimize(20, 30, 2);

		_signalMaLength = Param(nameof(SignalMaLength), 9)
							  .SetDisplay("Signal MA Length", "Signal moving average length", "MACD Settings")
							  .SetCanOptimize(true)
							  .SetOptimize(5, 15, 1);

		_macdTimeFrame = Param(nameof(MacdTimeFrame), TimeSpan.FromMinutes(30))
							 .SetDisplay("MACD Higher Time Frame", "Time frame for MACD", "MACD Settings");

		_trendMaType = Param(nameof(TrendMaType), MovingAverageTypeEnum.EMA)
						   .SetDisplay("Trend MA Type", "Moving average type for trend", "Trend Settings");

		_trendMaLength = Param(nameof(TrendMaLength), 55)
							 .SetDisplay("Trend MA Length", "Trend moving average length", "Trend Settings")
							 .SetCanOptimize(true)
							 .SetOptimize(30, 80, 5);

		_trendTimeFrame = Param(nameof(TrendTimeFrame), TimeSpan.FromDays(1))
							  .SetDisplay("Trend Higher Time Frame", "Time frame for trend filter", "Trend Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Base candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, MacdTimeFrame.TimeFrame()), (Security, TrendTimeFrame.TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macdValue = 0m;
		_signalValue = 0m;
		_macdReady = false;
		_trendValue = 0m;
		_prevTrendValue = 0m;
		_trendReady = false;
		_macdSmooth = null;
		_signalSmooth = null;
		_trendSmooth = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var baseTf = (TimeSpan)CandleType.Arg;
		var macdSmoothLength = Math.Max(1, (int)(MacdTimeFrame.TotalMinutes / baseTf.TotalMinutes));
		var trendSmoothLength = Math.Max(1, (int)(TrendTimeFrame.TotalMinutes / baseTf.TotalMinutes));

		_macdSmooth = new SimpleMovingAverage { Length = macdSmoothLength };
		_signalSmooth = new SimpleMovingAverage { Length = macdSmoothLength };
		_trendSmooth = new SimpleMovingAverage { Length = trendSmoothLength };

		var macd = new MovingAverageConvergenceDivergence { ShortMa = CreateMa(MacdMaType, FastMaLength),
															LongMa = CreateMa(MacdMaType, SlowMaLength),
															SignalMa = CreateMa(MacdMaType, SignalMaLength) };

		var macdSub = SubscribeCandles(MacdTimeFrame.TimeFrame());
		macdSub.Bind(macd, OnMacd).Start();

		var trendMa = CreateMa(TrendMaType, TrendMaLength);
		var trendSub = SubscribeCandles(TrendTimeFrame.TimeFrame());
		trendSub.Bind(trendMa, OnTrend).Start();

		var baseSub = SubscribeCandles(CandleType);
		baseSub.Bind(ProcessBase).Start();

		StartProtection();
	}

	private void OnMacd(ICandleMessage candle, decimal macd, decimal signal, decimal hist)
	{
		if (candle.State != CandleStates.Finished || _macdSmooth == null || _signalSmooth == null)
			return;

		var macdVal = _macdSmooth.Process(macd);
		var sigVal = _signalSmooth.Process(signal);

		if (!macdVal.IsFinal || !sigVal.IsFinal)
			return;

		_macdValue = macdVal.GetValue<decimal>();
		_signalValue = sigVal.GetValue<decimal>();
		_macdReady = true;
	}

	private void OnTrend(ICandleMessage candle, decimal trend)
	{
		if (candle.State != CandleStates.Finished || _trendSmooth == null)
			return;

		var val = _trendSmooth.Process(trend);
		if (!val.IsFinal)
			return;

		_prevTrendValue = _trendValue;
		_trendValue = val.GetValue<decimal>();
		_trendReady = true;
	}

	private void ProcessBase(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macdReady || !_trendReady)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var up = _trendValue > _prevTrendValue;
		var down = _trendValue < _prevTrendValue;

		var longCondition = _macdValue > _signalValue && _macdValue < 0m;
		var shortCondition = _macdValue < _signalValue && _macdValue > 0m;

		var qty = LeverageEquity ? ComputeQty(candle.ClosePrice) : Quantity;

		if (longCondition && up && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(qty);
		}
		else if (shortCondition && down && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(qty);
		}

		if (Position > 0 && _macdValue < _signalValue)
			ClosePosition();
		else if (Position < 0 && _macdValue > _signalValue)
			ClosePosition();
	}

	private decimal ComputeQty(decimal price)
	{
		var equity = InitialBalance + PnL;
		return equity * (1 + MarginFactor) / price;
	}

	private static MovingAverage CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch { MovingAverageTypeEnum.SMA => new SimpleMovingAverage { Length = length },
							 MovingAverageTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
							 MovingAverageTypeEnum.DEMA => new DoubleExponentialMovingAverage { Length = length },
							 MovingAverageTypeEnum.TEMA => new TripleExponentialMovingAverage { Length = length },
							 MovingAverageTypeEnum.WMA => new WeightedMovingAverage { Length = length },
							 MovingAverageTypeEnum.HMA => new HullMovingAverage { Length = length },
							 _ => throw new ArgumentOutOfRangeException(nameof(type), type, null) };
	}

	public enum MovingAverageTypeEnum
	{
		SMA,
		EMA,
		DEMA,
		TEMA,
		WMA,
		HMA
	}
}
