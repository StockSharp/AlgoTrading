using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades sharp MACD reversals using recent highs and lows for risk.
/// </summary>
public class MacdPatternTraderAllStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _stopLossBars;
	private readonly StrategyParam<int> _takeProfitBars;
	private readonly StrategyParam<int> _offsetPoints;
	private readonly StrategyParam<decimal> _ratioThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _macdPrev;
	private decimal _macdPrev2;
	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }

	/// <summary>
	/// Number of bars used to calculate stop loss.
	/// </summary>
	public int StopLossBars { get => _stopLossBars.Value; set => _stopLossBars.Value = value; }

	/// <summary>
	/// Number of bars used to calculate take profit.
	/// </summary>
	public int TakeProfitBars { get => _takeProfitBars.Value; set => _takeProfitBars.Value = value; }

	/// <summary>
	/// Offset in points added to stop loss.
	/// </summary>
	public int OffsetPoints { get => _offsetPoints.Value; set => _offsetPoints.Value = value; }

	/// <summary>
	/// Minimal ratio of MACD spikes to previous value.
	/// </summary>
	public decimal RatioThreshold { get => _ratioThreshold.Value; set => _ratioThreshold.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdPatternTraderAllStrategy"/> class.
	/// </summary>
	public MacdPatternTraderAllStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 24)
			.SetDisplay("Fast EMA Period", "Period for fast EMA in MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(12, 40, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 13)
			.SetDisplay("Slow EMA Period", "Period for slow EMA in MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 26, 1);

		_stopLossBars = Param(nameof(StopLossBars), 22)
			.SetDisplay("Stop Loss Bars", "Bars to look back for stop loss", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_takeProfitBars = Param(nameof(TakeProfitBars), 32)
			.SetDisplay("Take Profit Bars", "Bars to look back for take profit", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_offsetPoints = Param(nameof(OffsetPoints), 40)
			.SetDisplay("Offset Points", "Point offset added to stop loss", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_ratioThreshold = Param(nameof(RatioThreshold), 5m)
			.SetDisplay("MACD Ratio", "Minimal ratio of surrounding MACD values", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(3m, 7m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_macdPrev = 0m;
		_macdPrev2 = 0m;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastEmaPeriod },
			LongMa = { Length = SlowEmaPeriod }
		};

		var highStop = new Highest { Length = StopLossBars };
		var lowStop = new Lowest { Length = StopLossBars };
		var highTake = new Highest { Length = TakeProfitBars };
		var lowTake = new Lowest { Length = TakeProfitBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, highStop, lowStop, highTake, lowTake, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal highStop, decimal lowStop, decimal highTake, decimal lowTake)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage existing position by fixed stop and target
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLossPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Position);
				_stopLossPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLossPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopLossPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_macdPrev2 = _macdPrev;
			_macdPrev = macdValue;
			return;
		}

		var priceStep = Security?.PriceStep ?? 1m;
		var offset = OffsetPoints * priceStep;

		var macdCurr = macdValue;
		var macdLast = _macdPrev;
		var macdLast3 = _macdPrev2;

		if (macdLast != 0m)
		{
			var ratio1 = Math.Abs(macdLast3 / macdLast);
			var ratio2 = Math.Abs(macdCurr / macdLast);

			if ((macdLast3 > 0m || macdCurr < 0m) && ratio1 >= RatioThreshold && ratio2 >= RatioThreshold && Position >= 0)
			{
				var sl = highStop + offset;
				var tp = lowTake;
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_stopLossPrice = sl;
				_takeProfitPrice = tp;
			}
			else if ((macdLast3 < 0m || macdCurr > 0m) && ratio1 >= RatioThreshold && ratio2 >= RatioThreshold && Position <= 0)
			{
				var sl = lowStop - offset;
				var tp = highTake;
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_stopLossPrice = sl;
				_takeProfitPrice = tp;
			}
		}

		_macdPrev2 = _macdPrev;
		_macdPrev = macdCurr;
	}
}
