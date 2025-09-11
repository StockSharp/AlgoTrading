using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using SMA of highs and lows with significant slope and dynamic take profit and stop loss.
/// </summary>
public class SmaSlopeDynamicTpSlStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _initialTpPercent;
	private readonly StrategyParam<decimal> _trailingSlPercent;
	private readonly StrategyParam<decimal> _slopeThresholdPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smaHigh = null!;
	private SimpleMovingAverage _smaLow = null!;

	private readonly decimal[] _smaHighHist = new decimal[6];
	private readonly decimal[] _smaLowHist = new decimal[6];
	private int _histIndex;
	private int _histCount;

	private decimal _prevClose;
	private decimal? _stop;
	private decimal? _takeProfit;

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	/// <summary>
	/// Initial take profit percent.
	/// </summary>
	public decimal InitialTakeProfitPercent { get => _initialTpPercent.Value; set => _initialTpPercent.Value = value; }

	/// <summary>
	/// Trailing stop loss percent.
	/// </summary>
	public decimal TrailingStopPercent { get => _trailingSlPercent.Value; set => _trailingSlPercent.Value = value; }

	/// <summary>
	/// Minimum slope threshold in percent.
	/// </summary>
	public decimal SlopeThresholdPercent { get => _slopeThresholdPercent.Value; set => _slopeThresholdPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SmaSlopeDynamicTpSlStrategy"/> class.
	/// </summary>
	public SmaSlopeDynamicTpSlStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Period for SMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_initialTpPercent = Param(nameof(InitialTakeProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Initial TP %", "Initial take profit percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_trailingSlPercent = Param(nameof(TrailingStopPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing SL %", "Trailing stop loss percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_slopeThresholdPercent = Param(nameof(SlopeThresholdPercent), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Slope Threshold %", "Minimum slope in percent", "Strategy Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
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
		_histIndex = 0;
		_histCount = 0;
		_prevClose = 0m;
		_stop = null;
		_takeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smaHigh = new SimpleMovingAverage { Length = SmaPeriod };
		_smaLow = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smaHigh);
			DrawIndicator(area, _smaLow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smaHighVal = _smaHigh.Process(candle.HighPrice, candle.OpenTime, true).ToNullableDecimal();
		var smaLowVal = _smaLow.Process(candle.LowPrice, candle.OpenTime, true).ToNullableDecimal();

		if (smaHighVal is not decimal smaHigh || smaLowVal is not decimal smaLow)
			return;

		UpdateHistory(smaHigh, smaLow);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var slopePos = SlopePositive();
		var slopeNeg = SlopeNegative();

		var buyCondition = candle.ClosePrice > smaHigh && candle.LowPrice < smaHigh && _prevClose < smaHigh && slopePos;
		var sellCondition = candle.ClosePrice < smaLow && candle.HighPrice > smaLow && _prevClose > smaLow && slopeNeg;

		if (Position > 0 && sellCondition)
		{
			SellMarket(Position);
			ResetTrade();
		}
		else if (Position < 0 && buyCondition)
		{
			BuyMarket(-Position);
			ResetTrade();
		}

		if (buyCondition && Position == 0)
		{
			BuyMarket(Volume);
			_takeProfit = candle.ClosePrice * (1 + InitialTakeProfitPercent / 100m);
			_stop = candle.ClosePrice * (1 - TrailingStopPercent / 100m);
		}
		else if (sellCondition && Position == 0)
		{
			SellMarket(Volume);
			_takeProfit = candle.ClosePrice * (1 - InitialTakeProfitPercent / 100m);
			_stop = candle.ClosePrice * (1 + TrailingStopPercent / 100m);
		}

		if (Position > 0)
		{
			_stop = candle.ClosePrice * (1 - TrailingStopPercent / 100m);

			if (candle.LowPrice <= _stop || (_takeProfit.HasValue && candle.HighPrice >= _takeProfit.Value))
			{
				SellMarket(Position);
				ResetTrade();
			}
		}
		else if (Position < 0)
		{
			_stop = candle.ClosePrice * (1 + TrailingStopPercent / 100m);

			if (candle.HighPrice >= _stop || (_takeProfit.HasValue && candle.LowPrice <= _takeProfit.Value))
			{
				BuyMarket(-Position);
				ResetTrade();
			}
		}

		_prevClose = candle.ClosePrice;
	}

	private void UpdateHistory(decimal smaHigh, decimal smaLow)
	{
		_smaHighHist[_histIndex] = smaHigh;
		_smaLowHist[_histIndex] = smaLow;
		_histIndex = (_histIndex + 1) % _smaHighHist.Length;
		if (_histCount < _smaHighHist.Length)
			_histCount++;
	}

	private bool SlopePositive()
	{
		if (_histCount < 6)
			return false;

		var latestIdx = (_histIndex - 1 + 6) % 6;
		var prev1Idx = (_histIndex - 2 + 6) % 6;
		var prev5Idx = _histIndex;

		var curr = _smaHighHist[latestIdx];
		var prev1 = _smaHighHist[prev1Idx];
		var prev5 = _smaHighHist[prev5Idx];

		return prev5 != 0m && curr > prev1 && Math.Abs(curr - prev5) / prev5 > SlopeThresholdPercent / 100m;
	}

	private bool SlopeNegative()
	{
		if (_histCount < 6)
			return false;

		var latestIdx = (_histIndex - 1 + 6) % 6;
		var prev1Idx = (_histIndex - 2 + 6) % 6;
		var prev5Idx = _histIndex;

		var curr = _smaLowHist[latestIdx];
		var prev1 = _smaLowHist[prev1Idx];
		var prev5 = _smaLowHist[prev5Idx];

		return prev5 != 0m && curr < prev1 && Math.Abs(curr - prev5) / prev5 > SlopeThresholdPercent / 100m;
	}

	private void ResetTrade()
	{
		_stop = null;
		_takeProfit = null;
	}
}
