using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zero lag MACD direction change strategy.
/// Buys when the zero lag MACD decreases and sells when it increases.
/// Trades only during the configured time window and closes positions outside it.
/// </summary>
public class ZeroLagMacdCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useFreshSignal;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _killDay;
	private readonly StrategyParam<int> _killHour;
	private readonly StrategyParam<DataType> _candleType;

	private ZeroLagExponentialMovingAverage _fastZlema = null!;
	private ZeroLagExponentialMovingAverage _slowZlema = null!;

	private decimal _prevMacd;
	private decimal _prevPrevMacd;
	private bool _hasPrev;
	private bool _hasPrevPrev;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Require fresh MACD direction change before trading.
	/// </summary>
	public bool UseFreshSignal { get => _useFreshSignal.Value; set => _useFreshSignal.Value = value; }

	/// <summary>
	/// Trading window start hour (inclusive, UTC).
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// Trading window end hour (exclusive, UTC).
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	/// <summary>
	/// Day of week when all positions are force closed.
	/// 0 - Sunday, 6 - Saturday.
	/// </summary>
	public int KillDay { get => _killDay.Value; set => _killDay.Value = value; }

	/// <summary>
	/// Hour of the day when positions are force closed on <see cref="KillDay"/>.
	/// </summary>
	public int KillHour { get => _killHour.Value; set => _killHour.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ZeroLagMacdCrossoverStrategy"/>.
	/// </summary>
	public ZeroLagMacdCrossoverStrategy()
	{
		Param(nameof(Volume), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_fastLength = Param(nameof(FastLength), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA period", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_slowLength = Param(nameof(SlowLength), 34)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA period", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 2);

		_useFreshSignal = Param(nameof(UseFreshSignal), true)
		.SetDisplay("Use Fresh Signal", "Trade only on MACD direction change", "MACD");

		_startHour = Param(nameof(StartHour), 9)
		.SetDisplay("Start Hour", "Trading start hour (UTC)", "Time")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 15)
		.SetDisplay("End Hour", "Trading end hour (UTC)", "Time")
		.SetCanOptimize(true)
		.SetOptimize(1, 24, 1);

		_killDay = Param(nameof(KillDay), 5)
		.SetDisplay("Kill Day", "Week day for forced close", "Time")
		.SetCanOptimize(true)
		.SetOptimize(0, 6, 1);

		_killHour = Param(nameof(KillHour), 21)
		.SetDisplay("Kill Hour", "Hour for forced close", "Time")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevMacd = 0m;
		_prevPrevMacd = 0m;
		_hasPrev = false;
		_hasPrevPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_fastZlema = new ZeroLagExponentialMovingAverage { Length = FastLength };
		_slowZlema = new ZeroLagExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastZlema, _slowZlema, ProcessCandle)
		.Start();

		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.OpenTime;
		if (time.Hour < StartHour || time.Hour >= EndHour || ((int)time.DayOfWeek == KillDay && time.Hour == KillHour))
		{
			if (Position != 0)
			{
				if (Position > 0)
				SellMarket(Position);
				else
				BuyMarket(-Position);
			}
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var macd = 10m * (fast - slow);

		if (!_hasPrev)
		{
			_prevMacd = macd;
			_hasPrev = true;
			return;
		}

		if (!_hasPrevPrev)
		{
			_prevPrevMacd = _prevMacd;
			_prevMacd = macd;
			_hasPrevPrev = true;
			return;
		}

		var fresh = !UseFreshSignal || ((_prevMacd > _prevPrevMacd && macd < _prevMacd) || (_prevMacd < _prevPrevMacd && macd > _prevMacd));
		if (!fresh)
		{
			_prevPrevMacd = _prevMacd;
			_prevMacd = macd;
			return;
		}

		if (macd > _prevMacd)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				_prevPrevMacd = _prevMacd;
				_prevMacd = macd;
				return;
			}

			if (Position == 0)
			SellMarket(Volume);
		}
		else if (macd < _prevMacd)
		{
			if (Position < 0)
			{
				BuyMarket(-Position);
				_prevPrevMacd = _prevMacd;
				_prevMacd = macd;
				return;
			}

			if (Position == 0)
			BuyMarket(Volume);
		}

		_prevPrevMacd = _prevMacd;
		_prevMacd = macd;
	}
}
