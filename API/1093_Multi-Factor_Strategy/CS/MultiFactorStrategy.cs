using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-factor strategy combining MACD, RSI and trend filters.
/// Buys when MACD crosses above signal with bullish trend conditions.
/// Sells when MACD crosses below signal with bearish trend conditions.
/// Uses ATR-based stop loss and take profit.
/// </summary>
public class MultiFactorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _profitAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private RelativeStrengthIndex _rsi = null!;
	private AverageTrueRange _atr = null!;
	private SMA _sma50 = null!;
	private SMA _sma200 = null!;

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal StopAtrMultiplier
	{
		get => _stopAtrMultiplier.Value;
		set => _stopAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal ProfitAtrMultiplier
	{
		get => _profitAtrMultiplier.Value;
		set => _profitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiFactorStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA length", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 24, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA length", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal EMA length", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "ATR")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop ATR Mult", "ATR multiplier for stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_profitAtrMultiplier = Param(nameof(ProfitAtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Profit ATR Mult", "ATR multiplier for take profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		_rsi = new() { Length = RsiLength };
		_atr = new() { Length = AtrLength };
		_sma50 = new() { Length = 50 };
		_sma200 = new() { Length = 200 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _rsi, _atr, _sma50, _sma200, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma50);
			DrawIndicator(area, _sma200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue atrValue, IIndicatorValue sma50Value, IIndicatorValue sma200Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_rsi.IsFormed || !_atr.IsFormed || !_sma50.IsFormed || !_sma200.IsFormed)
			return;

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdData.Macd;
		var signalLine = macdData.Signal;
		var rsi = rsiValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var sma50 = sma50Value.ToDecimal();
		var sma200 = sma200Value.ToDecimal();

		var longCondition = macdLine > signalLine && rsi < 70m && candle.ClosePrice > sma50 && sma50 > sma200;
		var shortCondition = macdLine < signalLine && rsi > 30m && candle.ClosePrice < sma50 && sma50 < sma200;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}

		if (Position > 0)
		{
			var stop = _entryPrice - StopAtrMultiplier * atr;
			var target = _entryPrice + ProfitAtrMultiplier * atr;
			if (candle.LowPrice <= stop || candle.HighPrice >= target)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + StopAtrMultiplier * atr;
			var target = _entryPrice - ProfitAtrMultiplier * atr;
			if (candle.HighPrice >= stop || candle.LowPrice <= target)
				BuyMarket(Math.Abs(Position));
		}
	}
}
