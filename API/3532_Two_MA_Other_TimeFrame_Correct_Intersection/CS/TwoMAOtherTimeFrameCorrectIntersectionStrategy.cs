using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual moving average crossover strategy.
/// Converted from the "Two MA Other TimeFrame Correct Intersection" MQL5 expert advisor.
/// Uses fast and slow SMA on a single timeframe with crossover signals.
/// </summary>
public class TwoMAOtherTimeFrameCorrectIntersectionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TwoMAOtherTimeFrameCorrectIntersectionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for signal evaluation", "General");

		_fastLength = Param(nameof(FastLength), 10)
		.SetDisplay("Fast MA Length", "Number of bars for the fast moving average", "Indicators")
		.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 30)
		.SetDisplay("Slow MA Length", "Number of bars for the slow moving average", "Indicators")
		.SetGreaterThanZero();
	}

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars used by the fast moving average.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Number of bars used by the slow moving average.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		_fastMa = new SimpleMovingAverage { Length = FastLength };
		_slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _slowMa, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Fast crosses above slow => buy
		if (_prevFast < _prevSlow && fastValue > slowValue)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		// Fast crosses below slow => sell
		else if (_prevFast > _prevSlow && fastValue < slowValue)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
