using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades using swing high/low pivots with limit orders and fixed targets.
/// </summary>
public class SwingHighLowPivotsLvStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _stopTicks;
	private readonly StrategyParam<decimal> _tpTicks;
	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private decimal[] _openBuffer = Array.Empty<decimal>();
	private decimal[] _closeBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal? _longLimit;
	private decimal? _shortLimit;
	private decimal _longSL;
	private decimal _longTP;
	private decimal _shortSL;
	private decimal _shortTP;

	/// <summary>
	/// Pivot length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Stop distance.
	/// </summary>
	public decimal StopTicks
	{
		get => _stopTicks.Value;
		set => _stopTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _tpTicks.Value;
		set => _tpTicks.Value = value;
	}

	/// <summary>
	/// Use moving average filter.
	/// </summary>
	public bool UseMa
	{
		get => _useMa.Value;
		set => _useMa.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	/// Initializes a new instance of <see cref="SwingHighLowPivotsLvStrategy"/>.
	/// </summary>
	public SwingHighLowPivotsLvStrategy()
	{
		_length = Param(nameof(Length), 23)
			.SetGreaterThanZero()
			.SetDisplay("Limit", "Lookback for pivots", "Trade");

		_stopTicks = Param(nameof(StopTicks), 300000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop distance", "Risk");

		_tpTicks = Param(nameof(TakeProfitTicks), 150000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target", "Risk");

		_useMa = Param(nameof(UseMa), true)
			.SetDisplay("Use MA", "Enable moving average filter", "Trend");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_ema = null!;
		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_openBuffer = Array.Empty<decimal>();
		_closeBuffer = Array.Empty<decimal>();
		_bufferCount = 0;
		_longLimit = null;
		_shortLimit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		_ema.Process(candle.ClosePrice);

		UpdateBuffers(candle);

		var size = Length * 2 + 1;
		if (_bufferCount == size)
		{
			var pivotIndex = Length;
			var ph = _highBuffer[pivotIndex];
			var pl = _lowBuffer[pivotIndex];
			var isHigh = true;
			var isLow = true;

			for (var i = 0; i < size; i++)
			{
				if (i == pivotIndex)
					continue;
				if (ph <= _highBuffer[i])
					isHigh = false;
				if (pl >= _lowBuffer[i])
					isLow = false;
			}

			var maOkLong = !UseMa || (_ema.IsFormed && _closeBuffer[pivotIndex] > _ema.GetCurrentValue<decimal>());
			var maOkShort = !UseMa || (_ema.IsFormed && _closeBuffer[pivotIndex] < _ema.GetCurrentValue<decimal>());

			if (isLow && Position == 0 && maOkLong)
			{
				_longLimit = Math.Min(_openBuffer[pivotIndex], _closeBuffer[pivotIndex]);
				_longSL = _longLimit.Value - StopTicks;
				_longTP = _longLimit.Value + TakeProfitTicks;
			}
			if (isHigh && Position == 0 && maOkShort)
			{
				_shortLimit = Math.Max(_openBuffer[pivotIndex], _closeBuffer[pivotIndex]);
				_shortSL = _shortLimit.Value + StopTicks;
				_shortTP = _shortLimit.Value - TakeProfitTicks;
			}
		}

		if (_longLimit.HasValue && Position <= 0 && candle.HighPrice >= _longLimit.Value)
		{
			BuyMarket();
			_longLimit = null;
		}
		if (_shortLimit.HasValue && Position >= 0 && candle.LowPrice <= _shortLimit.Value)
		{
			SellMarket();
			_shortLimit = null;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _longSL || candle.HighPrice >= _longTP)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortSL || candle.LowPrice <= _shortTP)
				BuyMarket();
		}
	}

	private void UpdateBuffers(ICandleMessage candle)
	{
		var size = Length * 2 + 1;
		if (_highBuffer.Length != size)
		{
			_highBuffer = new decimal[size];
			_lowBuffer = new decimal[size];
			_openBuffer = new decimal[size];
			_closeBuffer = new decimal[size];
			_bufferCount = 0;
		}

		if (_bufferCount < size)
		{
			_highBuffer[_bufferCount] = candle.HighPrice;
			_lowBuffer[_bufferCount] = candle.LowPrice;
			_openBuffer[_bufferCount] = candle.OpenPrice;
			_closeBuffer[_bufferCount] = candle.ClosePrice;
			_bufferCount++;
		}
		else
		{
			for (var i = 0; i < size - 1; i++)
			{
				_highBuffer[i] = _highBuffer[i + 1];
				_lowBuffer[i] = _lowBuffer[i + 1];
				_openBuffer[i] = _openBuffer[i + 1];
				_closeBuffer[i] = _closeBuffer[i + 1];
			}
			_highBuffer[^1] = candle.HighPrice;
			_lowBuffer[^1] = candle.LowPrice;
			_openBuffer[^1] = candle.OpenPrice;
			_closeBuffer[^1] = candle.ClosePrice;
		}
	}
}
