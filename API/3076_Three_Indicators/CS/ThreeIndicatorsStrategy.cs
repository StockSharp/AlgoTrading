namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;
using StockSharp.Algo.Candles;

/// <summary>
/// Conversion of the "Three indicators" MQL5 expert into a StockSharp strategy.
/// Combines MACD, Stochastic Oscillator, and RSI filters evaluated on new candles.
/// </summary>
public class ThreeIndicatorsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<IndicatorAppliedPrice> _macdPriceType;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<IndicatorAppliedPrice> _rsiPriceType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private StochasticOscillator _stochastic = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal? _previousOpen;
	private decimal? _previousMacdMain;

	public ThreeIndicatorsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used to evaluate indicator signals.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Order volume for entries and reversals.", "Risk");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("MACD fast EMA", "Fast exponential moving average length inside MACD.", "MACD");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 53)
			.SetGreaterThanZero()
			.SetDisplay("MACD slow EMA", "Slow exponential moving average length inside MACD.", "MACD");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD signal EMA", "Signal smoothing length for the MACD line.", "MACD");

		_macdPriceType = Param(nameof(MacdPriceType), IndicatorAppliedPrice.Close)
			.SetDisplay("MACD price", "Applied price used when feeding data into MACD.", "MACD");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K calculation length.", "Stochastic");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D smoothing length.", "Stochastic");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 82)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic slowing", "Additional smoothing applied to %K.", "Stochastic");

		_rsiPeriod = Param(nameof(RsiPeriod), 86)
			.SetGreaterThanZero()
			.SetDisplay("RSI period", "Averaging length for the RSI filter.", "RSI");

		_rsiPriceType = Param(nameof(RsiPriceType), IndicatorAppliedPrice.Close)
			.SetDisplay("RSI price", "Applied price used by the RSI filter.", "RSI");
	}

	/// <summary>
	/// Candle subscription type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume used when opening or reversing positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Fast EMA period of the MACD calculation.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the MACD calculation.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period applied to the MACD line.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Applied price used for feeding MACD.
	/// </summary>
	public IndicatorAppliedPrice MacdPriceType
	{
		get => _macdPriceType.Value;
		set => _macdPriceType.Value = value;
	}

	/// <summary>
	/// Lookback length for the %K component of Stochastic.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Averaging period for the %D component of Stochastic.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to the %K line.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Averaging length for the RSI filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Applied price used for the RSI calculation.
	/// </summary>
	public IndicatorAppliedPrice RsiPriceType
	{
		get => _rsiPriceType.Value;
		set => _rsiPriceType.Value = value;
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
		_previousOpen = null;
		_previousMacdMain = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod }
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdInput = GetAppliedPrice(candle, MacdPriceType);
		var rsiInput = GetAppliedPrice(candle, RsiPriceType);

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)_macd.Process(macdInput, candle.CloseTime, true);
		if (!macdValue.IsFinal || macdValue.Macd is not decimal macdMain)
		{
			UpdateCaches(candle.OpenPrice, null);
			return;
		}

		var rsiValue = _rsi.Process(rsiInput, candle.CloseTime, true);
		if (!rsiValue.IsFinal)
		{
			UpdateCaches(candle.OpenPrice, macdMain);
			return;
		}
		var rsi = rsiValue.GetValue<decimal>();

		if (!stochValue.IsFinal)
		{
			UpdateCaches(candle.OpenPrice, macdMain);
			return;
		}

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.D is not decimal stochasticSignal)
		{
			UpdateCaches(candle.OpenPrice, macdMain);
			return;
		}

		if (_previousOpen is null || _previousMacdMain is null)
		{
			UpdateCaches(candle.OpenPrice, macdMain);
			return;
		}

		var candleSignal = ComputeCandleDirection(candle.OpenPrice, _previousOpen.Value);
		var macdSignal = ComputeMacdDirection(macdMain, _previousMacdMain.Value);
		var stochasticSignalDirection = ComputeOscillatorDirection(stochasticSignal);
		var rsiSignalDirection = ComputeOscillatorDirection(rsi);

		var longSignal = candleSignal >= 0 && macdSignal >= 0 && stochasticSignalDirection >= 0 && rsiSignalDirection >= 0;
		var shortSignal = candleSignal <= 0 && macdSignal <= 0 && stochasticSignalDirection <= 0 && rsiSignalDirection <= 0;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateCaches(candle.OpenPrice, macdMain);
			return;
		}

		var tradeVolume = TradeVolume;
		if (tradeVolume <= 0m)
		{
			UpdateCaches(candle.OpenPrice, macdMain);
			return;
		}

		var currentPosition = Position;

		if (currentPosition > 0m)
		{
			if (shortSignal)
			{
				var totalVolume = currentPosition + tradeVolume;
				SellMarket(totalVolume);
			}
		}
		else if (currentPosition < 0m)
		{
			if (longSignal)
			{
				var totalVolume = Math.Abs(currentPosition) + tradeVolume;
				BuyMarket(totalVolume);
			}
		}
		else
		{
			if (longSignal)
			{
				BuyMarket(tradeVolume);
			}
			else if (shortSignal)
			{
				SellMarket(tradeVolume);
			}
		}

		UpdateCaches(candle.OpenPrice, macdMain);
	}

	private void UpdateCaches(decimal openPrice, decimal? macdMain)
	{
		_previousOpen = openPrice;
		if (macdMain.HasValue)
			_previousMacdMain = macdMain;
	}

	private static int ComputeCandleDirection(decimal currentOpen, decimal previousOpen)
	{
		if (currentOpen > previousOpen)
			return 1;
		if (currentOpen < previousOpen)
			return -1;
		return 0;
	}

	private static int ComputeMacdDirection(decimal currentMacd, decimal previousMacd)
	{
		var delta = currentMacd - previousMacd;
		if (delta < 0m)
			return 1;
		if (delta > 0m)
			return -1;
		return 0;
	}

	private static int ComputeOscillatorDirection(decimal value)
	{
		if (value < 50m)
			return 1;
		if (value > 50m)
			return -1;
		return 0;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, IndicatorAppliedPrice priceType)
	{
		return priceType switch
		{
			IndicatorAppliedPrice.Open => candle.OpenPrice,
			IndicatorAppliedPrice.High => candle.HighPrice,
			IndicatorAppliedPrice.Low => candle.LowPrice,
			IndicatorAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			IndicatorAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			IndicatorAppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

public enum IndicatorAppliedPrice
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}

