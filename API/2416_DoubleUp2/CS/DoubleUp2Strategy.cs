using System;
using System.Linq;
using System.Collections.Generic;
using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DoubleUp2 strategy combining CCI and MACD with volume doubling (martingale).
/// </summary>
public class DoubleUp2Strategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _martingaleStep;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }

	/// <summary>
	/// Threshold for CCI and MACD signals.
	/// </summary>
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DoubleUp2Strategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 8)
			.SetDisplay("CCI Period", "Averaging period for CCI", "Indicators")
			.SetOptimize(4, 20, 1);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 13)
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
			.SetOptimize(5, 20, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 33)
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
			.SetOptimize(20, 50, 1);

		_threshold = Param(nameof(Threshold), 230m)
			.SetDisplay("Threshold", "CCI and MACD extreme level", "Strategy")
			.SetOptimize(50m, 300m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_martingaleStep = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var macd = new MovingAverageConvergenceDivergence(
			new ExponentialMovingAverage { Length = MacdSlowPeriod },
			new ExponentialMovingAverage { Length = MacdFastPeriod });

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security?.PriceStep ?? 1m;

		// Short entry condition
		if (cciValue > Threshold && macdValue > Threshold)
		{
			if (Position > 0)
			{
				var profit = candle.ClosePrice - _entryPrice;
				_martingaleStep = profit > 0m ? 0 : _martingaleStep + 1;
			}

			SellMarket();
			_entryPrice = candle.ClosePrice;
			return;
		}

		// Long entry condition
		if (cciValue < -Threshold && macdValue < -Threshold)
		{
			if (Position < 0)
			{
				var profit = _entryPrice - candle.ClosePrice;
				_martingaleStep = profit > 0m ? 0 : _martingaleStep + 1;
			}

			BuyMarket();
			_entryPrice = candle.ClosePrice;
			return;
		}

		// Exit profitable long position
		if (Position > 0 && candle.ClosePrice - _entryPrice > 120m * step)
		{
			SellMarket();
			_martingaleStep += 2;
			return;
		}

		// Exit profitable short position
		if (Position < 0 && _entryPrice - candle.ClosePrice > 120m * step)
		{
			BuyMarket();
			_martingaleStep += 2;
		}
	}
}
