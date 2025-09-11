
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with additional filters.
/// Goes long when the 100 EMA crosses above the 200 EMA and the 9 EMA is above the 50 EMA.
/// Goes short when the 100 EMA crosses below the 200 EMA and the 9 EMA is below the 50 EMA.
/// Exits long when the 100 EMA crosses below the 50 EMA and exits short when the 100 EMA crosses above the 50 EMA.
/// </summary>
public class EmaCrossoverFiltersStrategy : Strategy
{
	private readonly StrategyParam<int> _ema9Length;
	private readonly StrategyParam<int> _ema50Length;
	private readonly StrategyParam<int> _ema100Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _ema9;
	private EMA _ema50;
	private EMA _ema100;
	private EMA _ema200;

	private decimal _prevEma50;
	private decimal _prevEma100;
	private decimal _prevEma200;
	private bool _initialized;

	/// <summary>
	/// Length for the 9 EMA filter.
	/// </summary>
	public int Ema9Length
	{
		get => _ema9Length.Value;
		set => _ema9Length.Value = value;
	}

	/// <summary>
	/// Length for the 50 EMA filter.
	/// </summary>
	public int Ema50Length
	{
		get => _ema50Length.Value;
		set => _ema50Length.Value = value;
	}

	/// <summary>
	/// Length for the 100 EMA used in crossover.
	/// </summary>
	public int Ema100Length
	{
		get => _ema100Length.Value;
		set => _ema100Length.Value = value;
	}

	/// <summary>
	/// Length for the the 200 EMA used in crossover.
	/// </summary>
	public int Ema200Length
	{
		get => _ema200Length.Value;
		set => _ema200Length.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public EmaCrossoverFiltersStrategy()
	{
		_ema9Length = Param(nameof(Ema9Length), 9)
			.SetGreaterThanZero()
			.SetDisplay("EMA 9 Length", "Period for the 9 EMA", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_ema50Length = Param(nameof(Ema50Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA 50 Length", "Period for the 50 EMA", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(30, 70, 5);

		_ema100Length = Param(nameof(Ema100Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA 100 Length", "Period for the 100 EMA", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(80, 150, 10);

		_ema200Length = Param(nameof(Ema200Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA 200 Length", "Period for the 200 EMA", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(150, 300, 10);

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

		_prevEma50 = 0m;
		_prevEma100 = 0m;
		_prevEma200 = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema9 = new EMA { Length = Ema9Length };
		_ema50 = new EMA { Length = Ema50Length };
		_ema100 = new EMA { Length = Ema100Length };
		_ema200 = new EMA { Length = Ema200Length };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_ema9, _ema50, _ema100, _ema200, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema9);
			DrawIndicator(area, _ema50);
			DrawIndicator(area, _ema100);
			DrawIndicator(area, _ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema9Value, decimal ema50Value, decimal ema100Value, decimal ema200Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema9.IsFormed || !_ema50.IsFormed || !_ema100.IsFormed || !_ema200.IsFormed)
			return;

		if (!_initialized)
		{
			_prevEma50 = ema50Value;
			_prevEma100 = ema100Value;
			_prevEma200 = ema200Value;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longCondition = _prevEma100 <= _prevEma200 && ema100Value > ema200Value && ema9Value > ema50Value;
		var shortCondition = _prevEma100 >= _prevEma200 && ema100Value < ema200Value && ema9Value < ema50Value;
		var longExit = _prevEma100 >= _prevEma50 && ema100Value < ema50Value;
		var shortExit = _prevEma100 <= _prevEma50 && ema100Value > ema50Value;

		if (longCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (longExit && Position > 0)
			SellMarket(Math.Abs(Position));
		else if (shortExit && Position < 0)
			BuyMarket(Math.Abs(Position));

		_prevEma50 = ema50Value;
		_prevEma100 = ema100Value;
		_prevEma200 = ema200Value;
	}
}
