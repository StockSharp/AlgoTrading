namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Utility strategy that reconstructs higher timeframe candles from lower timeframe data.
/// </summary>
public class HtfCandlesLibStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;

	private DateTimeOffset _currentStart;
	private decimal _open;
	private decimal _high;
	private decimal _low;
	private decimal _close;
	private long _openIndex;
	private long _highIndex;
	private long _lowIndex;
	private long _closeIndex;

	/// <summary>
	/// Lower timeframe candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HtfCandlesLibStrategy"/>.
	/// </summary>
	public HtfCandlesLibStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("LTF Candle", "Lower timeframe candle type", "General");
		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("HTF Candle", "Higher timeframe candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var frame = (TimeSpan)HigherCandleType.Arg;

		if (_currentStart == default || candle.OpenTime >= _currentStart + frame)
		{
			if (_currentStart != default)
				LogCurrent();

			StartNew(candle);
			return;
		}

		if (candle.HighPrice > _high)
		{
			_high = candle.HighPrice;
			_highIndex = candle.OpenTime.ToUnixTimeMilliseconds();
		}

		if (candle.LowPrice < _low)
		{
			_low = candle.LowPrice;
			_lowIndex = candle.OpenTime.ToUnixTimeMilliseconds();
		}

		_close = candle.ClosePrice;
		_closeIndex = candle.OpenTime.ToUnixTimeMilliseconds();
	}

	private void StartNew(ICandleMessage candle)
	{
		_currentStart = candle.OpenTime;
		_open = candle.OpenPrice;
		_high = candle.HighPrice;
		_low = candle.LowPrice;
		_close = candle.ClosePrice;
		_openIndex = candle.OpenTime.ToUnixTimeMilliseconds();
		_highIndex = _openIndex;
		_lowIndex = _openIndex;
		_closeIndex = _openIndex;
	}

	private void LogCurrent()
	{
		Log.Info("O:{0} H:{1} L:{2} C:{3} ({4},{5},{6},{7})", _open, _high, _low, _close, _openIndex, _highIndex, _lowIndex, _closeIndex);
	}
}
