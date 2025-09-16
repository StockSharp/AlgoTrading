using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades Bollinger Bands breakouts with a moving average trend filter.
/// </summary>
public class BreakthroughBbStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private BollingerBands _bollingerBands;

	private decimal? _closeLag0;
	private decimal? _closeLag1;
	private decimal? _closeLag2;
	private decimal? _closeLag3;

	private decimal? _maLag0;
	private decimal? _maLag1;
	private decimal? _maLag2;
	private decimal? _maLag3;

	/// <summary>
	/// Moving average period that defines the long term trend.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands lookback period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width measured in standard deviations.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BreakthroughBbStrategy"/>.
	/// </summary>
	public BreakthroughBbStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Simple moving average length", "Parameters")
			.SetCanOptimize(true);

		_bandsPeriod = Param(nameof(BandsPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Bollinger Bands lookback", "Parameters")
			.SetCanOptimize(true);

		_deviation = Param(nameof(Deviation), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Bollinger Bands width in deviations", "Parameters")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for each trade", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series processed by the strategy", "General");
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

		_sma = default;
		_bollingerBands = default;

		_closeLag0 = null;
		_closeLag1 = null;
		_closeLag2 = null;
		_closeLag3 = null;

		_maLag0 = null;
		_maLag1 = null;
		_maLag2 = null;
		_maLag3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };
		_bollingerBands = new BollingerBands
		{
			Length = BandsPeriod,
			Width = Deviation
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_sma, _bollingerBands, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var maPrev4 = _maLag2;
		var closePrev4 = _closeLag2;

		if (_sma is null || _bollingerBands is null)
		{
			UpdateHistory(close, smaValue);
			return;
		}

		if (!_sma.IsFormed || !_bollingerBands.IsFormed)
		{
			UpdateHistory(close, smaValue);
			return;
		}

		if (Position > 0 && close < middleBand)
		{
			SellMarket(Math.Abs(Position));
			UpdateHistory(close, smaValue);
			return;
		}

		if (Position < 0 && close > middleBand)
		{
			BuyMarket(Math.Abs(Position));
			UpdateHistory(close, smaValue);
			return;
		}

		if (maPrev4 is null || closePrev4 is null)
		{
			UpdateHistory(close, smaValue);
			return;
		}

		if (Position == 0)
		{
			if (closePrev4.Value < upperBand && close > upperBand && smaValue > maPrev4.Value)
			{
				BuyMarket(Volume);
				UpdateHistory(close, smaValue);
				return;
			}

			if (closePrev4.Value > lowerBand && close < lowerBand && smaValue < maPrev4.Value)
			{
				SellMarket(Volume);
				UpdateHistory(close, smaValue);
				return;
			}
		}

		UpdateHistory(close, smaValue);
	}

	private void UpdateHistory(decimal close, decimal maValue)
	{
		_maLag3 = _maLag2;
		_maLag2 = _maLag1;
		_maLag1 = _maLag0;
		_maLag0 = maValue;

		_closeLag3 = _closeLag2;
		_closeLag2 = _closeLag1;
		_closeLag1 = _closeLag0;
		_closeLag0 = close;
	}
}
