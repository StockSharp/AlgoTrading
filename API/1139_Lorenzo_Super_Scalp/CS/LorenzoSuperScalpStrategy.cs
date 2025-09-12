using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI, Bollinger Bands and MACD based scalping strategy.
/// </summary>
public class LorenzoSuperScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _minBarsBetweenTrades;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private BollingerBands _bollinger;
	private MovingAverageConvergenceDivergence _macd;

	private bool? _lastSignalBuy;
	private int _lastTradeBar;
	private int _barIndex;
	private decimal _prevMacd;
	private decimal _prevSignal;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Minimum bars between trades.
	/// </summary>
	public int MinBarsBetweenTrades
	{
		get => _minBarsBetweenTrades.Value;
		set => _minBarsBetweenTrades.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public LorenzoSuperScalpStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands width", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_minBarsBetweenTrades = Param(nameof(MinBarsBetweenTrades), 15)
			.SetGreaterThanZero()
			.SetDisplay("Min Bars", "Minimum bars between trades", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_lastTradeBar = -1;
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
		_lastSignalBuy = null;
		_lastTradeBar = -1;
		_barIndex = 0;
		_prevMacd = 0m;
		_prevSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _bollinger, _macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, _rsi);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, _macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal _, decimal upperBand, decimal lowerBand, decimal macdValue, decimal signalValue, decimal _2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_bollinger.IsFormed || !_macd.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		var macdCrossUp = _prevMacd <= _prevSignal && macdValue > signalValue;
		var macdCrossDown = _prevMacd >= _prevSignal && macdValue < signalValue;

		var buySignal = rsiValue < 45m && candle.ClosePrice < lowerBand * 1.02m && macdCrossUp;
		var sellSignal = rsiValue > 55m && candle.ClosePrice > upperBand * 0.98m && macdCrossDown;

		var timeElapsed = _lastTradeBar == -1 || (_barIndex - _lastTradeBar) >= MinBarsBetweenTrades;
		var canBuy = buySignal && (!_lastSignalBuy.HasValue || !_lastSignalBuy.Value) && timeElapsed;
		var canSell = sellSignal && _lastSignalBuy.HasValue && _lastSignalBuy.Value && timeElapsed;

		if (canBuy && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_lastSignalBuy = true;
			_lastTradeBar = _barIndex;
		}
		else if (canSell && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_lastSignalBuy = false;
			_lastTradeBar = _barIndex;
		}

		_prevMacd = macdValue;
		_prevSignal = signalValue;
	}
}
