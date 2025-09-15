using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ASCTrendND-inspired strategy using SMA, RSI and ATR-based trailing stop.
/// </summary>
public class AscTrendNdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private SimpleMovingAverage _sma;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	private decimal? _stopPrice;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// ATR period for volatility estimate.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for trailing stop distance.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AscTrendNdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of source candles", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Length of simple moving average", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of relative strength index", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Length of average true range", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop trailing", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_sma = new SimpleMovingAverage { Length = SmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (price > smaValue && rsiValue > 50m)
			{
				_stopPrice = price - atrValue * AtrMultiplier;
				BuyMarket();
			}
			else if (price < smaValue && rsiValue < 50m)
			{
				_stopPrice = price + atrValue * AtrMultiplier;
				SellMarket();
			}
			return;
		}

		if (_stopPrice is null)
			return;

		if (Position > 0)
		{
			var newStop = price - atrValue * AtrMultiplier;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (price <= _stopPrice)
				SellMarket();
		}
		else
		{
			var newStop = price + atrValue * AtrMultiplier;
			if (newStop < _stopPrice)
				_stopPrice = newStop;

			if (price >= _stopPrice)
				BuyMarket();
		}
	}
}
