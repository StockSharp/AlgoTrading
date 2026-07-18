using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy that buys when price is above the trend SMA and RSI crosses above the oversold level.
/// ATR-based stop loss and take profit manage the position.
/// </summary>
public class NseIndexWithEntryExitMarkersStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLoss;
	private decimal _takeProfit;
	private decimal _prevRsi;
	private bool _isRsiInitialized;
	private DateTimeOffset _lastSignal = DateTimeOffset.MinValue;

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NseIndexWithEntryExitMarkersStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 200);
		_rsiPeriod = Param(nameof(RsiPeriod), 14);
		_rsiOversold = Param(nameof(RsiOversold), 25m);
		_atrPeriod = Param(nameof(AtrPeriod), 14);
		_atrMultiplier = Param(nameof(AtrMultiplier), 4m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopLoss = 0m;
		_takeProfit = 0m;
		_prevRsi = 0m;
		_isRsiInitialized = false;
		_lastSignal = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopLoss = 0m;
		_takeProfit = 0m;
		_prevRsi = 0m;
		_isRsiInitialized = false;
		_lastSignal = DateTimeOffset.MinValue;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var cooldown = TimeSpan.FromMinutes(480);

		if (Position > 0 && candle.OpenTime - _lastSignal >= cooldown)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
			{
				SellMarket();
				_stopLoss = 0m;
				_takeProfit = 0m;
				_lastSignal = candle.OpenTime;
			}
		}

		if (!_isRsiInitialized)
		{
			_prevRsi = rsiValue;
			_isRsiInitialized = true;
			return;
		}

		var inUptrend = candle.ClosePrice > smaValue;
		var crossUp = _prevRsi <= RsiOversold && rsiValue > RsiOversold;

		if (inUptrend && crossUp && Position <= 0 && candle.OpenTime - _lastSignal >= cooldown)
		{
			BuyMarket();
			_stopLoss = candle.ClosePrice - AtrMultiplier * atrValue;
			_takeProfit = candle.ClosePrice + AtrMultiplier * atrValue;
			_lastSignal = candle.OpenTime;
		}

		_prevRsi = rsiValue;
	}
}
