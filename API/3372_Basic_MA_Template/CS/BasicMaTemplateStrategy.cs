using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Basic moving average template converted from MQL4 strategy 27964.
/// Generates signals when the previous candle crosses the selected moving average.
/// Applies take-profit and stop-loss in pip distances similar to the original robot.
/// </summary>
public class BasicMaTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingAveragePeriod;
	private readonly StrategyParam<int> _movingAverageShift;
	private readonly StrategyParam<MovingAverageMethod> _movingAverageMethod;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	private readonly Queue<decimal> _shiftBuffer = new();

	private decimal? _previousOpen;
	private decimal? _previousClose;
	private decimal _pipSize;

	/// <summary>
	/// Candle type used to build the signal series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average lookback length.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _movingAveragePeriod.Value;
		set => _movingAveragePeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the moving average values.
	/// </summary>
	public int MovingAverageShift
	{
		get => _movingAverageShift.Value;
		set => _movingAverageShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation mode.
	/// </summary>
	public MovingAverageMethod MovingAverageMethod
	{
		get => _movingAverageMethod.Value;
		set => _movingAverageMethod.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BasicMaTemplateStrategy"/>.
	/// </summary>
	public BasicMaTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for signal generation", "General");

		_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 49)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Number of bars for the moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 5);

		_movingAverageShift = Param(nameof(MovingAverageShift), 0)
			.SetDisplay("MA Shift", "Forward shift applied to the moving average", "Indicator");

		_movingAverageMethod = Param(nameof(MovingAverageMethod), MovingAverageMethod.Simple)
			.SetDisplay("MA Method", "Moving average calculation mode", "Indicator");

		_takeProfitPips = Param(nameof(TakeProfitPips), 38.5m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 48.5m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 90m, 5m);
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
		_shiftBuffer.Clear();
		_previousOpen = null;
		_previousClose = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var movingAverage = CreateMovingAverage(MovingAverageMethod, MovingAveragePeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(movingAverage, ProcessCandle)
			.Start();

		_pipSize = CalculatePipSize();

		Unit? takeProfit = null;
		Unit? stopLoss = null;

		if (_pipSize > 0m)
		{
			if (TakeProfitPips > 0m)
				takeProfit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);

			if (StopLossPips > 0m)
				stopLoss = new Unit(StopLossPips * _pipSize, UnitTypes.Absolute);
		}

		StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, movingAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal movingAverageValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var shiftedValue = GetShiftedAverage(movingAverageValue);
		if (shiftedValue is null)
		{
			UpdatePreviousCandle(candle);
			return;
		}

		if (_previousOpen is decimal prevOpen && _previousClose is decimal prevClose)
		{
			if (Position == 0)
			{
				if (prevOpen > shiftedValue && prevClose < shiftedValue)
					SellMarket();
				else if (prevOpen < shiftedValue && prevClose > shiftedValue)
					BuyMarket();
			}
		}

		UpdatePreviousCandle(candle);
	}

	private decimal? GetShiftedAverage(decimal movingAverageValue)
	{
		var shift = MovingAverageShift;

		if (shift <= 0)
			return movingAverageValue;

		_shiftBuffer.Enqueue(movingAverageValue);

		while (_shiftBuffer.Count > shift + 1)
			_shiftBuffer.Dequeue();

		if (_shiftBuffer.Count <= shift)
			return null;

		return _shiftBuffer.Peek();
	}

	private void UpdatePreviousCandle(ICandleMessage candle)
	{
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security is null)
			return 0m;

		var step = security.Step;
		if (step <= 0m)
			return 0m;

		var decimals = security.Decimals;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;

		return step * multiplier;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int period)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SMA { Length = period },
			MovingAverageMethod.Exponential => new EMA { Length = period },
			MovingAverageMethod.Smoothed => new SMMA { Length = period },
			MovingAverageMethod.LinearWeighted => new WMA { Length = period },
			_ => new SMA { Length = period }
		};
	}

	/// <summary>
	/// Available moving average methods corresponding to the original MQL inputs.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>
		/// Simple moving average (MODE_SMA in MetaTrader).
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average (MODE_EMA in MetaTrader).
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average (MODE_SMMA in MetaTrader).
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average (MODE_LWMA in MetaTrader).
		/// </summary>
		LinearWeighted
	}
}
