using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gann Fan strategy: WMA crossover with MACD confirmation.
/// Buys when fast WMA above slow WMA and MACD bullish.
/// Sells when fast WMA below slow WMA and MACD bearish.
/// </summary>
public class GannFanStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevMacd;
	private decimal? _prevSignal;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public GannFanStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted MA length", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted MA length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;
		_prevMacd = null;
		_prevSignal = null;

		var fastWma = new WeightedMovingAverage { Length = FastMaLength };
		var slowWma = new WeightedMovingAverage { Length = SlowMaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastWma, slowWma, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		var maUptrend = fast > slow;
		var maDowntrend = fast < slow;

		// MACD crossover
		if (_prevFast.HasValue && _prevSlow.HasValue && _prevMacd.HasValue && _prevSignal.HasValue)
		{
			var maCross = _prevFast.Value <= _prevSlow.Value && fast > slow;
			var macdBull = macdLine > signalLine;

			if (maCross && macdBull && Position <= 0)
			{
				BuyMarket();
			}

			var maCrossDown = _prevFast.Value >= _prevSlow.Value && fast < slow;
			var macdBear = macdLine < signalLine;

			if (maCrossDown && macdBear && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}
}
