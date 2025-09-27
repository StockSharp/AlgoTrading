using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal ZigZag strategy translated from the "Fractal ZigZag Expert" MetaTrader 4 advisor.
/// The logic confirms Bill Williams style fractals and uses the last confirmed extremum to decide trade direction.
/// A bullish trend (value 2) opens long positions, while a bearish trend (value 1) opens shorts.
/// </summary>
public class FractalZigZagStrategy : Strategy
{
	private readonly StrategyParam<int> _level;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialStopPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleInfo> _window = new();

	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private DateTimeOffset? _lastUpTime;
	private DateTimeOffset? _lastDownTime;
	private DateTimeOffset? _lastFractalTime;
	private FractalTypes? _lastFractalTypes;
	private int _trend = 2;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingStopPrice;

	private enum FractalTypes
	{
		High,
		Low
	}

	private sealed class CandleInfo
	{
		public CandleInfo(DateTimeOffset time, decimal high, decimal low)
		{
			Time = time;
			High = high;
			Low = low;
		}

		public DateTimeOffset Time { get; }
		public decimal High { get; }
		public decimal Low { get; }
	}

	/// <summary>
	/// Number of candles on each side required to confirm a fractal extremum.
	/// </summary>
	public int Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial protective stop distance in price points.
	/// </summary>
	public decimal InitialStopPoints
	{
		get => _initialStopPoints.Value;
		set => _initialStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points. Zero disables trailing.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Order volume that mirrors the Lots parameter from the MQL expert.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Candle type used for fractal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FractalZigZagStrategy"/>.
	/// </summary>
	public FractalZigZagStrategy()
	{
		_level = Param(nameof(Level), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fractal Depth", "Number of candles around the extremum", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(2, 6, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 25m)
		.SetDisplay("Take Profit (points)", "Distance to the profit target", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 60m, 10m);

		_initialStopPoints = Param(nameof(InitialStopPoints), 20m)
		.SetDisplay("Initial Stop (points)", "Initial protective stop distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 50m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
		.SetDisplay("Trailing Stop (points)", "Trailing stop offset", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 30m, 5m);

		_lots = Param(nameof(Lots), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Volume of each market order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for analysis", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_window.Clear();
		_lastUpFractal = null;
		_lastDownFractal = null;
		_lastUpTime = null;
		_lastDownTime = null;
		_lastFractalTime = null;
		_lastFractalTypes = null;
		_trend = 2;

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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

		UpdateFractalWindow(candle);
		EvaluateFractals();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position == 0)
		{
			TryEnterPosition(candle);
		}
		else if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else
		{
			ManageShortPosition(candle);
		}
	}

	private void UpdateFractalWindow(ICandleMessage candle)
	{
		var depth = Math.Max(1, Level);
		var windowSize = depth * 2 + 1;

		_window.Add(new CandleInfo(candle.OpenTime, candle.HighPrice, candle.LowPrice));

		while (_window.Count > windowSize)
		_window.RemoveAt(0);
	}

	private void EvaluateFractals()
	{
		var depth = Math.Max(1, Level);
		var windowSize = depth * 2 + 1;

		if (_window.Count < windowSize)
		return;

		var centerIndex = _window.Count - 1 - depth;
		if (centerIndex < 0 || centerIndex >= _window.Count)
		return;

		var center = _window[centerIndex];

		var isHigh = true;
		var isLow = true;

		for (var i = 0; i < _window.Count; i++)
		{
			if (i == centerIndex)
			continue;

			var info = _window[i];

			if (info.High >= center.High)
			isHigh = false;

			if (info.Low <= center.Low)
			isLow = false;

			if (!isHigh && !isLow)
			break;
		}

		if (isHigh && (_lastUpTime is null || center.Time > _lastUpTime.Value))
		RegisterFractal(FractalTypes.High, center);

		if (isLow && (_lastDownTime is null || center.Time > _lastDownTime.Value))
		RegisterFractal(FractalTypes.Low, center);
	}

	private void RegisterFractal(FractalTypes type, CandleInfo info)
	{
		if (type == FractalTypes.High)
		{
			_lastUpFractal = info.High;
			_lastUpTime = info.Time;
		}
		else
		{
			_lastDownFractal = info.Low;
			_lastDownTime = info.Time;
		}

		if (_lastFractalTime is null || info.Time >= _lastFractalTime.Value)
		{
			_lastFractalTime = info.Time;
			_lastFractalTypes = type;
			_trend = type == FractalTypes.High ? 1 : 2;
		}
	}

	private void TryEnterPosition(ICandleMessage candle)
	{
		if (_lastFractalTypes is null)
		return;

		var volume = Lots;
		if (volume <= 0m)
		return;

		if (_trend == 2)
		{
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			ConfigureRisk(true, candle.ClosePrice);
		}
		else if (_trend == 1)
		{
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			ConfigureRisk(false, candle.ClosePrice);
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0)
		{
			ResetPositionState();
			return;
		}

		if (_takeProfitPrice is decimal target && candle.HighPrice >= target)
		{
			ClosePosition();
			return;
		}

		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			ClosePosition();
			return;
		}

		if (TrailingStopPoints <= 0m)
		return;

		var step = GetPointValue();
		var distance = TrailingStopPoints * step;
		var activationPrice = _entryPrice + distance;

		if (_trailingStopPrice is null)
		{
			if (candle.ClosePrice >= activationPrice)
			{
				var candidate = candle.ClosePrice - distance;
				if (_stopPrice is decimal initial && candidate < initial)
				candidate = initial;

				_trailingStopPrice = candidate;
			}
		}
		else
		{
			var candidate = candle.ClosePrice - distance;
			if (_stopPrice is decimal initial && candidate < initial)
			candidate = initial;

			if (candidate > _trailingStopPrice.Value)
			_trailingStopPrice = candidate;

			if (candle.LowPrice <= _trailingStopPrice.Value)
			ClosePosition();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0)
		{
			ResetPositionState();
			return;
		}

		if (_takeProfitPrice is decimal target && candle.LowPrice <= target)
		{
			ClosePosition();
			return;
		}

		if (_stopPrice is decimal stop && candle.HighPrice >= stop)
		{
			ClosePosition();
			return;
		}

		if (TrailingStopPoints <= 0m)
		return;

		var step = GetPointValue();
		var distance = TrailingStopPoints * step;
		var activationPrice = _entryPrice - distance;

		if (_trailingStopPrice is null)
		{
			if (candle.ClosePrice <= activationPrice)
			{
				var candidate = candle.ClosePrice + distance;
				if (_stopPrice is decimal initial && candidate > initial)
				candidate = initial;

				_trailingStopPrice = candidate;
			}
		}
		else
		{
			var candidate = candle.ClosePrice + distance;
			if (_stopPrice is decimal initial && candidate > initial)
			candidate = initial;

			if (candidate < _trailingStopPrice.Value)
			_trailingStopPrice = candidate;

			if (candle.HighPrice >= _trailingStopPrice.Value)
			ClosePosition();
		}
	}

	private void ConfigureRisk(bool isLong, decimal price)
	{
		var step = GetPointValue();

		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;

		if (InitialStopPoints > 0m)
		{
			var offset = InitialStopPoints * step;
			_stopPrice = isLong ? price - offset : price + offset;
		}

		if (TakeProfitPoints > 0m)
		{
			var offset = TakeProfitPoints * step;
			_takeProfitPrice = isLong ? price + offset : price - offset;
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}

		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep;
		return step.HasValue && step.Value > 0m ? step.Value : 1m;
	}
}
