namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Automatic Trendlines Strategy
/// </summary>
public class AutomaticTrendlinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _leftBars;
	private readonly StrategyParam<int> _rightBars;

	private readonly List<ICandleMessage> _candles = [];
	private int _barIndex;

	private int? _resX1;
	private int? _resX2;
	private decimal? _resY1;
	private decimal? _resY2;

	private int? _supX1;
	private int? _supX2;
	private decimal? _supY1;
	private decimal? _supY2;

	private decimal? _prevClose;
	private decimal? _prevResY;
	private decimal? _prevSupY;

	public AutomaticTrendlinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_leftBars = Param(nameof(LeftBars), 100)
			.SetDisplay("Left bars", "Pivot detection: left bars.", "General");

		_rightBars = Param(nameof(RightBars), 15)
			.SetDisplay("Right bars", "Pivot detection: right bars.", "General");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int LeftBars
	{
		get => _leftBars.Value;
		set => _leftBars.Value = value;
	}

	public int RightBars
	{
		get => _rightBars.Value;
		set => _rightBars.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_barIndex = default;

		_resX1 = _resX2 = _supX1 = _supX2 = null;
		_resY1 = _resY2 = _supY1 = _supY2 = null;

		_prevClose = _prevResY = _prevSupY = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		_candles.Add(candle);
		var maxCount = LeftBars + RightBars + 1;

		if (_candles.Count > maxCount)
			_candles.RemoveAt(0);

		if (_candles.Count == maxCount)
		{
			var pivotIndex = LeftBars;
			var pivotCandle = _candles[pivotIndex];

			var isHigh = true;
			var isLow = true;

			for (var i = 0; i < maxCount; i++)
			{
				if (i == pivotIndex)
					continue;

				if (_candles[i].High >= pivotCandle.High)
					isHigh = false;

				if (_candles[i].Low <= pivotCandle.Low)
					isLow = false;
			}

			var pivotBarIndex = _barIndex - RightBars - 1;

			if (isHigh)
			{
				_resX2 = _resX1;
				_resY2 = _resY1;
				_resX1 = pivotBarIndex;
				_resY1 = pivotCandle.High;
			}

			if (isLow)
			{
				_supX2 = _supX1;
				_supY2 = _supY1;
				_supX1 = pivotBarIndex;
				_supY1 = pivotCandle.Low;
			}
		}

		decimal? resY = null;

		if (_resX1.HasValue && _resX2.HasValue && _resY1.HasValue && _resY2.HasValue && _resX2 != _resX1)
		{
			var m = (_resY2.Value - _resY1.Value) / (_resX2.Value - _resX1.Value);
			var b = _resY1.Value - m * _resX1.Value;
			resY = m * _barIndex + b;
		}

		decimal? supY = null;

		if (_supX1.HasValue && _supX2.HasValue && _supY1.HasValue && _supY2.HasValue && _supX2 != _supX1)
		{
			var m = (_supY2.Value - _supY1.Value) / (_supX2.Value - _supX1.Value);
			var b = _supY1.Value - m * _supX1.Value;
			supY = m * _barIndex + b;
		}

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (_prevClose is decimal pc)
			{
				if (resY is decimal r && _prevResY is decimal pr && pc <= pr && candle.ClosePrice > r && Position <= 0)
				{
					BuyMarket();
				}
				else if (supY is decimal s && _prevSupY is decimal ps && pc >= ps && candle.ClosePrice < s && Position >= 0)
				{
					SellMarket();
				}
			}
		}

		_prevClose = candle.ClosePrice;
		_prevResY = resY;
		_prevSupY = supY;
	}
}
