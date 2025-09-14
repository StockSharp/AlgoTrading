using System;

using StockSharp.BusinessEntities;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed candle color changes.
/// The difference between close and open prices is smoothed with a moving average.
/// A long position opens when the smoothed value turns from positive to negative (color 1 after 0).
/// A short position opens on the opposite transition.
/// </summary>
public class CandlesSmoothedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MaMethod> _maMethod;

	private IIndicator _ma;
	private int? _prevColor;

	/// <summary>
	/// Candle time frame.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Moving average smoothing method.
	/// </summary>
	public MaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public CandlesSmoothedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of processed candles", "General");

		_maLength = Param(nameof(MaLength), 30)
			.SetDisplay("MA Length", "Moving average smoothing length", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_maMethod = Param(nameof(MaMethod), MaMethod.Weighted)
			.SetDisplay("MA Method", "Smoothing algorithm for candle difference", "Indicator");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = MaMethod switch
		{
			MaMethod.Simple => new SMA { Length = MaLength },
			MaMethod.Exponential => new EMA { Length = MaLength },
			MaMethod.Smma => new SMMA { Length = MaLength },
			_ => new WeightedMovingAverage { Length = MaLength },
		};

		_prevColor = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(candle => ProcessCandle(candle)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diff = candle.ClosePrice - candle.OpenPrice;
		var maValue = _ma.Process(candle.OpenTime, diff);

		if (!maValue.IsFinal || !maValue.TryGetValue(out var smoothedDiff))
			return;

		var color = smoothedDiff > 0m ? 0 : 1;

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		var volume = Volume + Math.Abs(Position);

		if (color == 1 && _prevColor == 0)
		{
			if (Position < 0)
				ClosePosition();
			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (color == 0 && _prevColor == 1)
		{
			if (Position > 0)
				ClosePosition();
			if (Position >= 0)
				SellMarket(volume);
		}

		_prevColor = color;
	}
}

/// <summary>
/// Moving average types for smoothing.
/// </summary>
public enum MaMethod
{
	/// <summary>
	/// Simple Moving Average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential Moving Average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed Moving Average (RMA).
	/// </summary>
	Smma,

	/// <summary>
	/// Weighted Moving Average.
	/// </summary>
	Weighted
}
