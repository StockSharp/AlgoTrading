using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weighted moving average crossover strategy with SSL trend confirmation.
/// </summary>
public class MarketSlayerStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _confirmationTrendValue;
	private readonly StrategyParam<bool> _takeProfitEnabled;
	private readonly StrategyParam<decimal> _takeProfitValue;
	private readonly StrategyParam<bool> _stopLossEnabled;
	private readonly StrategyParam<decimal> _stopLossValue;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;

	private decimal? _prevShort;
	private decimal? _prevLong;
	private readonly WeightedMovingAverage _trendWmaHigh = new();
	private readonly WeightedMovingAverage _trendWmaLow = new();
	private int _trendHlv;
	private bool _isTrendBullish;

	/// <summary>
	/// Length for short WMA.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Length for long WMA.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Period for trend confirmation WMA.
	/// </summary>
	public int ConfirmationTrendValue
	{
		get => _confirmationTrendValue.Value;
		set => _confirmationTrendValue.Value = value;
	}

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool TakeProfitEnabled
	{
		get => _takeProfitEnabled.Value;
		set => _takeProfitEnabled.Value = value;
	}

	/// <summary>
	/// Take profit value in points.
	/// </summary>
	public decimal TakeProfitValue
	{
		get => _takeProfitValue.Value;
		set => _takeProfitValue.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool StopLossEnabled
	{
		get => _stopLossEnabled.Value;
		set => _stopLossEnabled.Value = value;
	}

	/// <summary>
	/// Stop loss value in points.
	/// </summary>
	public decimal StopLossValue
	{
		get => _stopLossValue.Value;
		set => _stopLossValue.Value = value;
	}

	/// <summary>
	/// Candle type for main timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type for trend timeframe.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketSlayerStrategy"/>
	/// class.
	/// </summary>
	public MarketSlayerStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 10)
						   .SetGreaterThanZero()
						   .SetDisplay("Short WMA Length")
						   .SetCanOptimize(true);

		_longLength = Param(nameof(LongLength), 20)
						  .SetGreaterThanZero()
						  .SetDisplay("Long WMA Length")
						  .SetCanOptimize(true);

		_confirmationTrendValue = Param(nameof(ConfirmationTrendValue), 2)
									  .SetGreaterThanZero()
									  .SetDisplay("Trend WMA Length")
									  .SetCanOptimize(true);

		_takeProfitEnabled = Param(nameof(TakeProfitEnabled), false)
								 .SetDisplay("Enable Take Profit");

		_takeProfitValue = Param(nameof(TakeProfitValue), 20m)
							   .SetGreaterOrEqualZero()
							   .SetDisplay("Take Profit")
							   .SetCanOptimize(true);

		_stopLossEnabled = Param(nameof(StopLossEnabled), false)
							   .SetDisplay("Enable Stop Loss");

		_stopLossValue = Param(nameof(StopLossValue), 50m)
							 .SetGreaterOrEqualZero()
							 .SetDisplay("Stop Loss")
							 .SetCanOptimize(true);

		_candleType = Param(nameof(CandleType),
							TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(240).TimeFrame())
							   .SetDisplay("Trend Candle Type");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TrendCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevShort = _prevLong = null;
		_trendHlv = 0;
		_isTrendBullish = false;
		_trendWmaHigh.Length = ConfirmationTrendValue;
		_trendWmaLow.Length = ConfirmationTrendValue;
		_trendWmaHigh.Reset();
		_trendWmaLow.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortWma = new WeightedMovingAverage { Length = ShortLength };
		var longWma = new WeightedMovingAverage { Length = LongLength };

		_trendWmaHigh.Length = ConfirmationTrendValue;
		_trendWmaLow.Length = ConfirmationTrendValue;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(shortWma, longWma, ProcessCandle).Start();

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription.Bind(ProcessTrend).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortWma);
			DrawIndicator(area, longWma);
			DrawOwnTrades(area);
		}

		if (TakeProfitEnabled || StopLossEnabled)
		{
			StartProtection(takeProfit: TakeProfitEnabled
								? new Unit(TakeProfitValue, UnitTypes.Absolute)
								: null,
							stopLoss: StopLossEnabled
								? new Unit(StopLossValue, UnitTypes.Absolute)
								: null);
		}
	}

	private void ProcessTrend(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highVal = _trendWmaHigh.Process(new DecimalIndicatorValue(
			_trendWmaHigh, candle.HighPrice, candle.OpenTime));
		var lowVal = _trendWmaLow.Process(new DecimalIndicatorValue(
			_trendWmaLow, candle.LowPrice, candle.OpenTime));

		if (!highVal.IsFinal || !lowVal.IsFinal)
			return;

		var high = highVal.GetValue<decimal>();
		var low = lowVal.GetValue<decimal>();

		if (candle.ClosePrice > high)
			_trendHlv = 1;
		else if (candle.ClosePrice < low)
			_trendHlv = -1;

		var sslDown = _trendHlv < 0 ? high : low;
		var sslUp = _trendHlv < 0 ? low : high;
		_isTrendBullish = sslUp > sslDown;
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortWma,
							   decimal longWma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevShort.HasValue && _prevLong.HasValue)
		{
			var crossUp = _prevShort <= _prevLong && shortWma > longWma;
			var crossDown = _prevShort >= _prevLong && shortWma < longWma;

			if (crossUp && _isTrendBullish && Position <= 0)
				BuyMarket();

			if (crossDown && !_isTrendBullish && Position >= 0)
				SellMarket();

			if (Position > 0 && !_isTrendBullish)
				ClosePosition();

			if (Position < 0 && _isTrendBullish)
				ClosePosition();
		}

		_prevShort = shortWma;
		_prevLong = longWma;
	}
}
