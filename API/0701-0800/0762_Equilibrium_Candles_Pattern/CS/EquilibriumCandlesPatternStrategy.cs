using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Equilibrium candles pattern strategy.
/// The equilibrium is the midpoint of the highest high and lowest low over EquilibriumLength candles. At least CandlesForTrend
/// consecutive closes above it form a bullish trend; the first MaxPullbackCandles closes back below it are a pullback that goes
/// long. A bearish trend of closes below it followed by closes back above goes short. UseReverse swaps the directions and an
/// opposite signal reverses the position. With UseTpSl the stop and target lie StopMultiplier ATR from the entry, and with
/// UseBigCandleExit a candle body larger than BigCandleMultiplier ATR closes the position.
/// </summary>
public class EquilibriumCandlesPatternStrategy : Strategy
{
	private readonly StrategyParam<int> _equilibriumLength;
	private readonly StrategyParam<int> _candlesForTrend;
	private readonly StrategyParam<int> _maxPullbackCandles;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<bool> _useTpSl;
	private readonly StrategyParam<bool> _useBigCandleExit;
	private readonly StrategyParam<decimal> _bigCandleMultiplier;
	private readonly StrategyParam<bool> _useReverse;
	private readonly StrategyParam<DataType> _candleType;

	private int _aboveCount;
	private int _belowCount;
	private int _bullTrendLength;
	private int _bearTrendLength;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Lookback of the equilibrium.
	/// </summary>
	public int EquilibriumLength
	{
		get => _equilibriumLength.Value;
		set => _equilibriumLength.Value = value;
	}

	/// <summary>
	/// Consecutive closes on one side of the equilibrium that form a trend.
	/// </summary>
	public int CandlesForTrend
	{
		get => _candlesForTrend.Value;
		set => _candlesForTrend.Value = value;
	}

	/// <summary>
	/// Maximum pullback candles that still give an entry.
	/// </summary>
	public int MaxPullbackCandles
	{
		get => _maxPullbackCandles.Value;
		set => _maxPullbackCandles.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop and target distance in ATR.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// Use the ATR stop and target.
	/// </summary>
	public bool UseTpSl
	{
		get => _useTpSl.Value;
		set => _useTpSl.Value = value;
	}

	/// <summary>
	/// Close the position on a big candle.
	/// </summary>
	public bool UseBigCandleExit
	{
		get => _useBigCandleExit.Value;
		set => _useBigCandleExit.Value = value;
	}

	/// <summary>
	/// Body size in ATR that makes a big candle.
	/// </summary>
	public decimal BigCandleMultiplier
	{
		get => _bigCandleMultiplier.Value;
		set => _bigCandleMultiplier.Value = value;
	}

	/// <summary>
	/// Trade in the opposite direction.
	/// </summary>
	public bool UseReverse
	{
		get => _useReverse.Value;
		set => _useReverse.Value = value;
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
	/// Constructor.
	/// </summary>
	public EquilibriumCandlesPatternStrategy()
	{
		_equilibriumLength = Param(nameof(EquilibriumLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Equilibrium Length", "Lookback of the equilibrium", "Pattern");

		_candlesForTrend = Param(nameof(CandlesForTrend), 7)
			.SetGreaterThanZero()
			.SetDisplay("Candles For Trend", "Consecutive closes that form a trend", "Pattern");

		_maxPullbackCandles = Param(nameof(MaxPullbackCandles), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Pullback Candles", "Maximum pullback candles that still give an entry", "Pattern");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_stopMultiplier = Param(nameof(StopMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Multiplier", "Stop and target distance in ATR", "Risk");

		_useTpSl = Param(nameof(UseTpSl), true)
			.SetDisplay("Use TP/SL", "Use the ATR stop and target", "Risk");

		_useBigCandleExit = Param(nameof(UseBigCandleExit), true)
			.SetDisplay("Big Candle Exit", "Close the position on a big candle", "Risk");

		_bigCandleMultiplier = Param(nameof(BigCandleMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Big Candle Multiplier", "Body size in ATR that makes a big candle", "Risk");

		_useReverse = Param(nameof(UseReverse), false)
			.SetDisplay("Reverse", "Trade in the opposite direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_aboveCount = 0;
		_belowCount = 0;
		_bullTrendLength = 0;
		_bearTrendLength = 0;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var highest = new Highest { Length = EquilibriumLength };
		var lowest = new Lowest { Length = EquilibriumLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var equilibrium = (highest + lowest) / 2m;
		var close = candle.ClosePrice;

		var bullishPullback = false;
		var bearishPullback = false;

		if (close > equilibrium)
		{
			// The bearish trend that preceded this pullback.
			if (_belowCount > 0)
				_bearTrendLength = _belowCount;

			_aboveCount++;
			_belowCount = 0;
			bearishPullback = _bearTrendLength >= CandlesForTrend && _aboveCount <= MaxPullbackCandles;
			if (_aboveCount > MaxPullbackCandles)
				_bearTrendLength = 0;
		}
		else if (close < equilibrium)
		{
			if (_aboveCount > 0)
				_bullTrendLength = _aboveCount;

			_belowCount++;
			_aboveCount = 0;
			bullishPullback = _bullTrendLength >= CandlesForTrend && _belowCount <= MaxPullbackCandles;
			if (_belowCount > MaxPullbackCandles)
				_bullTrendLength = 0;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 && CheckExit(candle, atr))
			return;

		var goLong = UseReverse ? bearishPullback : bullishPullback;
		var goShort = UseReverse ? bullishPullback : bearishPullback;

		if (goLong && Position <= 0)
		{
			_stopPrice = close - atr * StopMultiplier;
			_takePrice = close + atr * StopMultiplier;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (goShort && Position >= 0)
		{
			_stopPrice = close + atr * StopMultiplier;
			_takePrice = close - atr * StopMultiplier;
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private bool CheckExit(ICandleMessage candle, decimal atr)
	{
		var bigCandle = UseBigCandleExit && Math.Abs(candle.ClosePrice - candle.OpenPrice) > atr * BigCandleMultiplier;

		if (Position > 0)
		{
			if (bigCandle || (UseTpSl && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)))
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (bigCandle || (UseTpSl && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)))
		{
			BuyMarket(-Position);
			return true;
		}

		return false;
	}
}
