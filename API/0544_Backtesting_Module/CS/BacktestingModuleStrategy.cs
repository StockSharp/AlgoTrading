using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on 50/200 SMA crossover with optional time interval.
/// </summary>
public class BacktestingModuleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Start of trading interval.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of trading interval.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BacktestingModuleStrategy"/>.
	/// </summary>
	public BacktestingModuleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Period for fast SMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_slowLength = Param(nameof(SlowLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Period for slow SMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(100, 400, 20);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long trades", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short trades", "Trading");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of trading interval", "Trading");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2050, 12, 31, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of trading interval", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevFast = 0;
		_prevSlow = 0;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SMA { Length = FastLength };
		var slowSma = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartTime || candle.OpenTime > EndTime)
		{
			if (Position != 0)
				ClosePosition();

			return;
		}

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var volume = Volume + Math.Abs(Position);

		if (_prevFast <= _prevSlow && fast > slow && Position <= 0 && AllowLong)
		{
			BuyMarket(volume);
		}
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0 && AllowShort)
		{
			SellMarket(volume);
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
