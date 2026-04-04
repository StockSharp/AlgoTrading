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
/// Strategy that computes a Z-score of the instrument's own volatility
/// (standard deviation of close prices) and trades mean-reversion:
/// buy when Z-score drops below -threshold (low volatility, expect breakout),
/// sell when Z-score rises above +threshold (high volatility).
/// Adapted from multi-VIX Z-score approach to single instrument.
/// </summary>
public class ZScoreNormalizedVixStrategy : Strategy
{
	private readonly StrategyParam<int> _zScoreLength;
	private readonly StrategyParam<int> _volatilityLength;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _volatility;
	private SimpleMovingAverage _zMean;
	private StandardDeviation _zStd;

	private decimal _entryPrice;
	private int _barsSinceLastTrade;

	/// <summary>
	/// Lookback period for z-score calculation.
	/// </summary>
	public int ZScoreLength
	{
		get => _zScoreLength.Value;
		set => _zScoreLength.Value = value;
	}

	/// <summary>
	/// Lookback period for volatility (StdDev of close).
	/// </summary>
	public int VolatilityLength
	{
		get => _volatilityLength.Value;
		set => _volatilityLength.Value = value;
	}

	/// <summary>
	/// Z-score threshold for buy entry (negative means low vol).
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Z-score threshold for sell/exit.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Minimum bars between trades to avoid overtrading.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ZScoreNormalizedVixStrategy()
	{
		_zScoreLength = Param(nameof(ZScoreLength), 50)
			.SetDisplay("Z-Score Length", "Lookback period for z-score of volatility", "Parameters")
			.SetOptimize(20, 80, 10);

		_volatilityLength = Param(nameof(VolatilityLength), 20)
			.SetDisplay("Volatility Length", "Period for StdDev volatility measure", "Parameters")
			.SetOptimize(10, 40, 5);

		_buyThreshold = Param(nameof(BuyThreshold), -1.5m)
			.SetDisplay("Buy Threshold", "Z-score below this triggers buy", "Parameters")
			.SetOptimize(-2.5m, -0.5m, 0.25m);

		_sellThreshold = Param(nameof(SellThreshold), 1.5m)
			.SetDisplay("Sell Threshold", "Z-score above this triggers sell/exit", "Parameters")
			.SetOptimize(0.5m, 2.5m, 0.25m);

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Minimum bars between trades", "Parameters")
			.SetOptimize(50, 200, 25);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_volatility = null;
		_zMean = null;
		_zStd = null;
		_entryPrice = 0;
		_barsSinceLastTrade = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volatility = new StandardDeviation { Length = VolatilityLength };
		_zMean = new SimpleMovingAverage { Length = ZScoreLength };
		_zStd = new StandardDeviation { Length = ZScoreLength };

		_entryPrice = 0;
		_barsSinceLastTrade = 0;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Feed close price into volatility indicator
		var volResult = _volatility.Process(candle);

		if (!_volatility.IsFormed)
			return;

		var volValue = volResult.GetValue<decimal>();

		// Feed volatility value into z-score components (SMA and StdDev of volatility)
		var meanResult = _zMean.Process(new DecimalIndicatorValue(_zMean, volValue, candle.OpenTime) { IsFinal = true });
		var stdResult = _zStd.Process(new DecimalIndicatorValue(_zStd, volValue, candle.OpenTime) { IsFinal = true });

		if (!_zMean.IsFormed || !_zStd.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var mean = meanResult.GetValue<decimal>();
		var std = stdResult.GetValue<decimal>();

		if (std == 0)
			return;

		var zScore = (volValue - mean) / std;

		_barsSinceLastTrade++;

		// Low volatility (z-score below buy threshold) => buy (expect breakout)
		if (Position == 0 && zScore < BuyThreshold && _barsSinceLastTrade >= CooldownBars)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_barsSinceLastTrade = 0;
		}
		// Z-score reverts toward mean => close long
		else if (Position > 0 && zScore > 0 && _barsSinceLastTrade >= CooldownBars)
		{
			SellMarket();
			_entryPrice = 0;
			_barsSinceLastTrade = 0;
		}
		// High volatility => short (expect mean reversion in vol)
		else if (Position == 0 && zScore > SellThreshold && _barsSinceLastTrade >= CooldownBars)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_barsSinceLastTrade = 0;
		}
		// Z-score reverts toward mean => close short
		else if (Position < 0 && zScore < 0 && _barsSinceLastTrade >= CooldownBars)
		{
			BuyMarket();
			_entryPrice = 0;
			_barsSinceLastTrade = 0;
		}
	}
}
