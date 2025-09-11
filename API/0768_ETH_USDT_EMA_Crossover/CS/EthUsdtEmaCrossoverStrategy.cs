using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ETH/USDT EMA crossover strategy with RSI, ATR and volume filters.
/// </summary>
public class EthUsdtEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<int> _ema20Length;
	private readonly StrategyParam<int> _ema50Length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma20;
	private decimal _prevEma50;
	private bool _isInitialized;

	/// <summary>
	/// EMA 200 period.
	/// </summary>
	public int Ema200Length
	{
		get => _ema200Length.Value;
		set => _ema200Length.Value = value;
	}

	/// <summary>
	/// EMA 20 period.
	/// </summary>
	public int Ema20Length
	{
		get => _ema20Length.Value;
		set => _ema20Length.Value = value;
	}

	/// <summary>
	/// EMA 50 period.
	/// </summary>
	public int Ema50Length
	{
		get => _ema50Length.Value;
		set => _ema50Length.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EthUsdtEmaCrossoverStrategy()
	{
		_ema200Length = Param(nameof(Ema200Length), 200)
							.SetGreaterThanZero()
							.SetDisplay("EMA200 Length",
										"Period of the 200 EMA", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(100, 400, 50);

		_ema20Length = Param(nameof(Ema20Length), 20)
						   .SetGreaterThanZero()
						   .SetDisplay("EMA20 Length", "Period of the 20 EMA",
									   "Indicators")
						   .SetCanOptimize(true)
						   .SetOptimize(5, 40, 5);

		_ema50Length = Param(nameof(Ema50Length), 50)
						   .SetGreaterThanZero()
						   .SetDisplay("EMA50 Length", "Period of the 50 EMA",
									   "Indicators")
						   .SetCanOptimize(true)
						   .SetOptimize(20, 100, 10);

		_rsiLength =
			Param(nameof(RsiLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "Period of the RSI", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

		_atrLength =
			Param(nameof(AtrLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Length", "Period of the ATR", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevEma20 = 0m;
		_prevEma50 = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema200 = new ExponentialMovingAverage { Length = Ema200Length };
		var ema20 = new ExponentialMovingAverage { Length = Ema20Length };
		var ema50 = new ExponentialMovingAverage { Length = Ema50Length };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var atrSma = new SimpleMovingAverage { Length = 10 };
		var volumeSma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema200, ema20, ema50, rsi, atr,
				  (candle, ema200Val, ema20Val, ema50Val, rsiVal, atrVal) =>
				  {
					  var atrAvg =
						  atrSma
							  .Process(atrVal, candle.ServerTime,
									   candle.State == CandleStates.Finished)
							  .ToDecimal();
					  var volumeAvg =
						  volumeSma
							  .Process(candle.TotalVolume, candle.ServerTime,
									   candle.State == CandleStates.Finished)
							  .ToDecimal();

					  ProcessCandle(candle, ema200Val, ema20Val, ema50Val,
									rsiVal, atrVal, atrAvg, volumeAvg);
				  })
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema200);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema50);
			DrawIndicator(area, rsi);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema200,
							   decimal ema20, decimal ema50, decimal rsi,
							   decimal atr, decimal atrAvg, decimal volumeAvg)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevEma20 = ema20;
			_prevEma50 = ema50;
			_isInitialized = true;
			return;
		}

		var trendFilter = candle.ClosePrice > ema200;
		var rsiFilterLong = rsi > 30m;
		var rsiFilterShort = rsi < 70m;
		var volatilityFilter = atr > atrAvg;
		var volumeFilter = candle.TotalVolume > volumeAvg;

		var crossedAbove = _prevEma20 <= _prevEma50 && ema20 > ema50;
		var crossedBelow = _prevEma20 >= _prevEma50 && ema20 < ema50;

		var longCondition = crossedAbove && trendFilter && rsiFilterLong &&
							volatilityFilter && volumeFilter;
		var shortCondition = crossedBelow && candle.ClosePrice < ema200 &&
							 rsiFilterShort && volatilityFilter && volumeFilter;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevEma20 = ema20;
		_prevEma50 = ema50;
	}
}
