using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 5SMA-25SMA crossover with 75SMA trend filter and ADX confirmation.
/// Closes the position on sudden price moves.
/// </summary>
public class GcStrategyWithTrendFilterAndSuddenMoveProfitTakingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _suddenMovePercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Trend SMA length.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// ADX calculation length.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// Minimum ADX value to allow trades.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Sudden move percentage (e.g. 0.006 = 0.6%).
	/// </summary>
	public decimal SuddenMovePercent
	{
		get => _suddenMovePercent.Value;
		set => _suddenMovePercent.Value = value;
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
	/// Initializes parameters.
	/// </summary>
	public GcStrategyWithTrendFilterAndSuddenMoveProfitTakingStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
						  .SetDisplay("Fast SMA", "Period of the fast moving average", "Indicators")
						  .SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 25)
						  .SetDisplay("Slow SMA", "Period of the slow moving average", "Indicators")
						  .SetGreaterThanZero();

		_trendLength = Param(nameof(TrendLength), 75)
						   .SetDisplay("Trend SMA", "Period of the trend moving average", "Indicators")
						   .SetGreaterThanZero();

		_adxLength = Param(nameof(AdxLength), 14)
						 .SetDisplay("ADX Length", "Period of ADX indicator", "Indicators")
						 .SetGreaterThanZero();

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
							.SetDisplay("ADX Threshold", "Minimum ADX value to trade", "Filters")
							.SetGreaterThanZero();

		_suddenMovePercent =
			Param(nameof(SuddenMovePercent), 0.006m)
				.SetDisplay("Sudden Move %", "Exit when move exceeds this fraction of previous close", "Risk")
				.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var smaFast = new SMA { Length = FastLength };
		var smaSlow = new SMA { Length = SlowLength };
		var smaTrend = new SMA { Length = TrendLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(smaFast, smaSlow, smaTrend, adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, smaFast);
			DrawIndicator(area, smaSlow);
			DrawIndicator(area, smaTrend);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastVal, IIndicatorValue slowVal,
							   IIndicatorValue trendVal, IIndicatorValue adxVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		if (adxTyped.MovingAverage is not decimal adxMa)
			return;

		var fast = fastVal.ToDecimal();
		var slow = slowVal.ToDecimal();
		var trend = trendVal.ToDecimal();

		if (_prevClose != 0m)
		{
			var move = Math.Abs(candle.ClosePrice - _prevClose) / _prevClose;
			if (move > SuddenMovePercent && Position != 0)
			{
				ClosePosition();
				_prevClose = candle.ClosePrice;
				return;
			}
		}

		var longCond = fast > slow && candle.ClosePrice > trend && adxMa > AdxThreshold;
		var shortCond = fast < slow && candle.ClosePrice < trend && adxMa > AdxThreshold;

		if (longCond && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCond && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && shortCond)
			SellMarket(Position);
		else if (Position < 0 && longCond)
			BuyMarket(-Position);

		_prevClose = candle.ClosePrice;
	}
}
