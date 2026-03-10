using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ard Order Management Stochastic: RSI overbought/oversold reversal
/// with EMA trend filter and ATR-based trailing stops.
/// </summary>
public class ArdOrderManagementStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;

	private decimal _prevRsi;
	private decimal _entryPrice;
	private decimal _trailStop;

	public ArdOrderManagementStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA trend filter period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for stops.", "Indicators");

		_buyThreshold = Param(nameof(BuyThreshold), 45m)
			.SetDisplay("Buy Threshold", "RSI oversold level for buy.", "Signals");

		_sellThreshold = Param(nameof(SellThreshold), 55m)
			.SetDisplay("Sell Threshold", "RSI overbought level for sell.", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0;
		_entryPrice = 0;
		_trailStop = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_entryPrice = 0;
		_trailStop = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0 || atrVal <= 0)
		{
			_prevRsi = rsiVal;
			return;
		}

		var close = candle.ClosePrice;

		// Trailing stop management
		if (Position > 0)
		{
			var newTrail = close - atrVal * 1.5m;
			if (newTrail > _trailStop)
				_trailStop = newTrail;

			if (close <= _trailStop || close >= _entryPrice + atrVal * 3m)
			{
				SellMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}
		else if (Position < 0)
		{
			var newTrail = close + atrVal * 1.5m;
			if (_trailStop == 0 || newTrail < _trailStop)
				_trailStop = newTrail;

			if (close >= _trailStop || close <= _entryPrice - atrVal * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}

		// Entry: RSI crosses threshold with EMA trend confirmation
		if (Position == 0)
		{
			if (rsiVal < BuyThreshold && _prevRsi >= BuyThreshold && close > emaVal)
			{
				_entryPrice = close;
				_trailStop = close - atrVal * 2m;
				BuyMarket();
			}
			else if (rsiVal > SellThreshold && _prevRsi <= SellThreshold && close < emaVal)
			{
				_entryPrice = close;
				_trailStop = close + atrVal * 2m;
				SellMarket();
			}
		}

		_prevRsi = rsiVal;
	}
}
