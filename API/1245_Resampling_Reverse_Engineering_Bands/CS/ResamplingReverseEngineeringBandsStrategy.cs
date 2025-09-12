using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that builds reverse-engineered RSI bands using resampled prices.
/// </summary>
public class ResamplingReverseEngineeringBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _highThreshold;
	private readonly StrategyParam<decimal> _lowThreshold;
	private readonly StrategyParam<int> _sampleLength;
	private readonly StrategyParam<int> _sampleOffset;
	private readonly StrategyParam<DataType> _candleType;

	private int _barCount;
	private decimal _avgGain;
	private decimal _avgLoss;
	private decimal? _prevPrice;
	private decimal _highBand;
	private decimal _lowBand;
	private decimal _midBand;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// High RSI threshold.
	/// </summary>
	public decimal HighThreshold
	{
		get => _highThreshold.Value;
		set => _highThreshold.Value = value;
	}

	/// <summary>
	/// Low RSI threshold.
	/// </summary>
	public decimal LowThreshold
	{
		get => _lowThreshold.Value;
		set => _lowThreshold.Value = value;
	}

	/// <summary>
	/// Bars per sample.
	/// </summary>
	public int SampleLength
	{
		get => _sampleLength.Value;
		set => _sampleLength.Value = value;
	}

	/// <summary>
	/// Sample offset.
	/// </summary>
	public int SampleOffset
	{
		get => _sampleOffset.Value;
		set => _sampleOffset.Value = value;
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
	/// Initializes a new instance of the <see cref="ResamplingReverseEngineeringBandsStrategy"/>.
	/// </summary>
	public ResamplingReverseEngineeringBandsStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI averaging length", "Indicators")
			.SetGreaterThanZero();

		_highThreshold = Param(nameof(HighThreshold), 70m)
			.SetDisplay("RSI High", "High RSI threshold", "Strategy");

		_lowThreshold = Param(nameof(LowThreshold), 30m)
			.SetDisplay("RSI Low", "Low RSI threshold", "Strategy");

		_sampleLength = Param(nameof(SampleLength), 1)
			.SetDisplay("Sample Length", "Bars per sample", "Resampling")
			.SetGreaterThanZero();

		_sampleOffset = Param(nameof(SampleOffset), 0)
			.SetDisplay("Sample Offset", "Offset in bars for sampling", "Resampling");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_barCount = 0;
		_avgGain = 0m;
		_avgLoss = 0m;
		_prevPrice = null;
		_highBand = _lowBand = _midBand = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		var period = RsiPeriod;
		var isSample = (_barCount - SampleOffset) % SampleLength == 0;
		if (isSample)
		{
			var price = candle.ClosePrice;

			if (_prevPrice != null)
			{
				var change = price - _prevPrice.Value;
				var gain = change > 0m ? change : 0m;
				var loss = change < 0m ? -change : 0m;

				_avgGain = (_avgGain * (period - 1) + gain) / period;
				_avgLoss = (_avgLoss * (period - 1) + loss) / period;

				if (_avgLoss > 0m)
				{
					var rs = _avgGain / _avgLoss;
					var rsi = 100m * (1m - 1m / (1m + rs));

					_highBand = ReverseRsi(price, period, _avgGain, _avgLoss, HighThreshold, rsi);
					_lowBand = ReverseRsi(price, period, _avgGain, _avgLoss, LowThreshold, rsi);
					_midBand = ReverseRsi(price, period, _avgGain, _avgLoss, 50m, rsi);
				}
			}

			_prevPrice = price;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (close > _highBand && Position <= 0)
			SellMarket();
		else if (close < _lowBand && Position >= 0)
			BuyMarket();
	}

	private static decimal ReverseRsi(decimal price, int period, decimal avgGain, decimal avgLoss, decimal level, decimal rsi)
	{
		var gainProj = rsi < level
			? ((level * (period - 1) * avgLoss) / (100m - level)) - (period - 1) * avgGain
			: 0m;

		var lossProj = rsi > level
			? (((100m - level) * (period - 1) * avgGain) / level) - (period - 1) * avgLoss
			: 0m;

		return price + gainProj - lossProj;
	}
}
