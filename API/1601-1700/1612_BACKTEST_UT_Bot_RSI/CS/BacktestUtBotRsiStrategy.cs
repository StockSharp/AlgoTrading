using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// UT Bot trend strategy combined with EMA filter.
/// Opens long on trend reversal up below EMA and
/// short on trend reversal down above EMA.
/// Uses StandardDeviation instead of ATR for volatility measurement.
/// </summary>
public class BacktestUtBotRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _trail;
	private int _dir;
	private int _prevDir;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BacktestUtBotRsiStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend filter", "Parameters");

		_stdLength = Param(nameof(StdLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "StdDev period", "Parameters");

		_factor = Param(nameof(Factor), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("UT Bot Factor", "Volatility multiplier", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_trail = null;
		_dir = 0;
		_prevDir = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = StdLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdValue <= 0)
			return;

		var upperBand = candle.ClosePrice + Factor * stdValue;
		var lowerBand = candle.ClosePrice - Factor * stdValue;

		if (_trail is null)
		{
			_trail = lowerBand;
			_dir = 0;
		}
		else if (candle.ClosePrice > _trail)
		{
			_trail = Math.Max(_trail.Value, lowerBand);
			_dir = 1;
		}
		else if (candle.ClosePrice < _trail)
		{
			_trail = Math.Min(_trail.Value, upperBand);
			_dir = -1;
		}

		var trendUp = _dir == 1 && _prevDir == -1;
		var trendDown = _dir == -1 && _prevDir == 1;

		if (trendUp && candle.ClosePrice < emaValue && Position <= 0)
			BuyMarket();
		else if (trendDown && candle.ClosePrice > emaValue && Position >= 0)
			SellMarket();

		_prevDir = _dir;
	}
}
