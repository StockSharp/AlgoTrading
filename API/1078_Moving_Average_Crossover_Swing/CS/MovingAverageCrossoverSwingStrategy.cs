using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with optional slow MA and MACD confirmation.
/// Uses ATR-based stop loss and take profit.
/// </summary>
public class MovingAverageCrossoverSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _fastExitPeriod;
	private readonly StrategyParam<int> _mediumExitPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrTakeMultiplier;
	private readonly StrategyParam<bool> _enableSlow;
	private readonly StrategyParam<bool> _enableMacd;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _enableCrossExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMedium;
	private decimal _prevFastExit;
	private decimal _prevMediumExit;
	private decimal _entryAtr;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	/// <summary>
	/// Fast exit EMA period.
	/// </summary>
	public int FastExitPeriod { get => _fastExitPeriod.Value; set => _fastExitPeriod.Value = value; }

	/// <summary>
	/// Medium exit EMA period.
	/// </summary>
	public int MediumExitPeriod { get => _mediumExitPeriod.Value; set => _mediumExitPeriod.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Stop multiplier for ATR.
	/// </summary>
	public decimal AtrStopMultiplier { get => _atrStopMultiplier.Value; set => _atrStopMultiplier.Value = value; }

	/// <summary>
	/// Take multiplier for ATR.
	/// </summary>
	public decimal AtrTakeMultiplier { get => _atrTakeMultiplier.Value; set => _atrTakeMultiplier.Value = value; }

	/// <summary>
	/// Enable slow EMA confirmation.
	/// </summary>
	public bool EnableSlow { get => _enableSlow.Value; set => _enableSlow.Value = value; }

	/// <summary>
	/// Enable MACD confirmation.
	/// </summary>
	public bool EnableMacd { get => _enableMacd.Value; set => _enableMacd.Value = value; }

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	/// <summary>
	/// Enable exit on MA cross.
	/// </summary>
	public bool EnableCrossExit { get => _enableCrossExit.Value; set => _enableCrossExit.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageCrossoverSwingStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast Period", "Fast EMA length", "General")
			.SetCanOptimize(true);
		_mediumPeriod = Param(nameof(MediumPeriod), 10)
			.SetDisplay("Medium Period", "Medium EMA length", "General")
			.SetCanOptimize(true);
		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetDisplay("Slow Period", "Slow EMA length", "General")
			.SetCanOptimize(true);
		_fastExitPeriod = Param(nameof(FastExitPeriod), 5)
			.SetDisplay("Fast Exit Period", "Fast exit EMA length", "Exit")
			.SetCanOptimize(true);
		_mediumExitPeriod = Param(nameof(MediumExitPeriod), 10)
			.SetDisplay("Medium Exit Period", "Medium exit EMA length", "Exit")
			.SetCanOptimize(true);
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR calculation length", "Risk")
			.SetCanOptimize(true);
		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1.4m)
			.SetDisplay("ATR Stop", "ATR stop multiplier", "Risk")
			.SetCanOptimize(true);
		_atrTakeMultiplier = Param(nameof(AtrTakeMultiplier), 3.2m)
			.SetDisplay("ATR Take", "ATR take multiplier", "Risk")
			.SetCanOptimize(true);
		_enableSlow = Param(nameof(EnableSlow), true)
			.SetDisplay("Use Slow", "Enable slow EMA filter", "General");
		_enableMacd = Param(nameof(EnableMacd), true)
			.SetDisplay("Use MACD", "Enable MACD filter", "General");
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Long", "Allow long trades", "General");
		_enableShort = Param(nameof(EnableShort), false)
			.SetDisplay("Short", "Allow short trades", "General");
		_enableCrossExit = Param(nameof(EnableCrossExit), true)
			.SetDisplay("Cross Exit", "Exit on MA cross", "Exit");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var mediumEma = new ExponentialMovingAverage { Length = MediumPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var fastExitEma = new ExponentialMovingAverage { Length = FastExitPeriod };
		var mediumExitEma = new ExponentialMovingAverage { Length = MediumExitPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, mediumEma, slowEma, fastExitEma, mediumExitEma, atr, macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal medium, decimal slow, decimal fastExit, decimal mediumExit, decimal atr, decimal macd, decimal signal, decimal hist)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var longCross = _prevFast <= _prevMedium && fast > medium;
		var shortCross = _prevFast >= _prevMedium && fast < medium;
		var longExitCross = _prevFastExit >= _prevMediumExit && fastExit < mediumExit;
		var shortExitCross = _prevFastExit <= _prevMediumExit && fastExit > mediumExit;

		var longOk = EnableLong && longCross && (!EnableSlow || candle.ClosePrice > slow) && (!EnableMacd || hist > 0m);
		var shortOk = EnableShort && shortCross && (!EnableSlow || candle.ClosePrice < slow) && (!EnableMacd || hist < 0m);

		if (longOk && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryAtr = atr;
		}
		else if (shortOk && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryAtr = atr;
		}
		else
		{
			if (Position > 0)
			{
				var stop = PositionPrice - _entryAtr * AtrStopMultiplier;
				var take = PositionPrice + _entryAtr * AtrTakeMultiplier;

				if ((EnableCrossExit && longExitCross) || candle.LowPrice <= stop || candle.HighPrice >= take)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				var stop = PositionPrice + _entryAtr * AtrStopMultiplier;
				var take = PositionPrice - _entryAtr * AtrTakeMultiplier;

				if ((EnableCrossExit && shortExitCross) || candle.HighPrice >= stop || candle.LowPrice <= take)
					BuyMarket(Math.Abs(Position));
			}
		}

		_prevFast = fast;
		_prevMedium = medium;
		_prevFastExit = fastExit;
		_prevMediumExit = mediumExit;
	}
}
