using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with higher timeframe confirmation.
/// Long entries only.
/// </summary>
public class Ema1055200LongOnlyMtfStrategy : Strategy
{
	private readonly StrategyParam<int> _ema10Length;
	private readonly StrategyParam<int> _ema55Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<int> _ema500Length;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _ema10;
	private EMA _ema55;
	private EMA _ema200;
	private EMA _ema500;
	private EMA _ema1d55;
	private EMA _ema1d200;
	private EMA _ema1w55;
	private EMA _ema1w200;

	private decimal _prevEma10;
	private decimal _prevEma55;
	private decimal _prevEma200;
	private decimal _prevEma500;

	private decimal _ema1d55Value;
	private decimal _ema1d200Value;
	private decimal _ema1w55Value;
	private decimal _ema1w200Value;

	private decimal _entryPrice;
	private decimal _stopPrice;

	private bool _initialized;
	private bool _dailyReady;
	private bool _weeklyReady;

	private readonly DataType _dailyType = TimeSpan.FromDays(1).TimeFrame();
	private readonly DataType _weeklyType = TimeSpan.FromDays(7).TimeFrame();

	/// <summary>
	/// EMA 10 length.
	/// </summary>
	public int Ema10Length
	{
		get => _ema10Length.Value;
		set => _ema10Length.Value = value;
	}

	/// <summary>
	/// EMA 55 length.
	/// </summary>
	public int Ema55Length
	{
		get => _ema55Length.Value;
		set => _ema55Length.Value = value;
	}

	/// <summary>
	/// EMA 200 length.
	/// </summary>
	public int Ema200Length
	{
		get => _ema200Length.Value;
		set => _ema200Length.Value = value;
	}

	/// <summary>
	/// EMA 500 length.
	/// </summary>
	public int Ema500Length
	{
		get => _ema500Length.Value;
		set => _ema500Length.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Main candle timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Ema1055200LongOnlyMtfStrategy()
	{
		_ema10Length = Param(nameof(Ema10Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA 10 Length", "Fast EMA length", "General");

		_ema55Length = Param(nameof(Ema55Length), 55)
			.SetGreaterThanZero()
			.SetDisplay("EMA 55 Length", "Medium EMA length", "General");

		_ema200Length = Param(nameof(Ema200Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA 200 Length", "Slow EMA length", "General");

		_ema500Length = Param(nameof(Ema500Length), 500)
			.SetGreaterThanZero()
			.SetDisplay("EMA 500 Length", "Very slow EMA length", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Main timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, _dailyType);
		yield return (Security, _weeklyType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevEma10 = _prevEma55 = _prevEma200 = _prevEma500 = 0m;
		_ema1d55Value = _ema1d200Value = _ema1w55Value = _ema1w200Value = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_initialized = false;
		_dailyReady = false;
		_weeklyReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_ema10 = new EMA { Length = Ema10Length };
		_ema55 = new EMA { Length = Ema55Length };
		_ema200 = new EMA { Length = Ema200Length };
		_ema500 = new EMA { Length = Ema500Length };
		_ema1d55 = new EMA { Length = Ema55Length };
		_ema1d200 = new EMA { Length = Ema200Length };
		_ema1w55 = new EMA { Length = Ema55Length };
		_ema1w200 = new EMA { Length = Ema200Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema10, _ema55, _ema200, _ema500, ProcessCandle)
			.Start();

		SubscribeCandles(_dailyType)
			.Bind(_ema1d55, _ema1d200, (candle, e55, e200) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (_ema1d55.IsFormed && _ema1d200.IsFormed)
					_dailyReady = true;

				_ema1d55Value = e55;
				_ema1d200Value = e200;
			})
			.Start();

		SubscribeCandles(_weeklyType)
			.Bind(_ema1w55, _ema1w200, (candle, e55, e200) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (_ema1w55.IsFormed && _ema1w200.IsFormed)
					_weeklyReady = true;

				_ema1w55Value = e55;
				_ema1w200Value = e200;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema10);
			DrawIndicator(area, _ema55);
			DrawIndicator(area, _ema200);
			DrawIndicator(area, _ema500);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema10Value, decimal ema55Value, decimal ema200Value, decimal ema500Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			if (_ema10.IsFormed && _ema55.IsFormed && _ema200.IsFormed && _ema500.IsFormed)
			{
				_prevEma10 = ema10Value;
				_prevEma55 = ema55Value;
				_prevEma200 = ema200Value;
				_prevEma500 = ema500Value;
				_initialized = true;
			}

			return;
		}

		if (!_dailyReady || !_weeklyReady)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var cross10over55 = _prevEma10 <= _prevEma55 && ema10Value > ema55Value && candle.HighPrice > ema55Value;
		var cross55over200 = _prevEma55 <= _prevEma200 && ema55Value > ema200Value;
		var cross10over500 = _prevEma10 <= _prevEma500 && ema10Value > ema500Value;
		var baseCondition = cross10over55 || cross55over200 || cross10over500;

		var htfConfirmation = _ema1d55Value > _ema1d200Value && _ema1w55Value > _ema1w200Value;

		var cross10under200 = _prevEma10 >= _prevEma200 && ema10Value < ema200Value;
		var cross10under500 = _prevEma10 >= _prevEma500 && ema10Value < ema500Value;
		var longExit = Position > 0 && (cross10under200 || cross10under500);

		if (baseCondition && htfConfirmation && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1m - StopLossPercent / 100m);
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (longExit)
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_stopPrice = 0m;
		}
		else if (Position > 0 && candle.LowPrice <= _stopPrice)
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_stopPrice = 0m;
		}

		_prevEma10 = ema10Value;
		_prevEma55 = ema55Value;
		_prevEma200 = ema200Value;
		_prevEma500 = ema500Value;
	}
}