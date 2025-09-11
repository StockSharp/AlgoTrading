using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on equilibrium candles and trend pattern.
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

	private bool _isBullTrend;
	private bool _isBearTrend;
	private int _bullCandleCount;
	private int _bearCandleCount;
	private decimal _entryPrice;

	public int EquilibriumLength
	{
		get => _equilibriumLength.Value;
		set => _equilibriumLength.Value = value;
	}

	public int CandlesForTrend
	{
		get => _candlesForTrend.Value;
		set => _candlesForTrend.Value = value;
	}

	public int MaxPullbackCandles
	{
		get => _maxPullbackCandles.Value;
		set => _maxPullbackCandles.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	public bool UseTpSl
	{
		get => _useTpSl.Value;
		set => _useTpSl.Value = value;
	}

	public bool UseBigCandleExit
	{
		get => _useBigCandleExit.Value;
		set => _useBigCandleExit.Value = value;
	}

	public decimal BigCandleMultiplier
	{
		get => _bigCandleMultiplier.Value;
		set => _bigCandleMultiplier.Value = value;
	}

	public bool UseReverse
	{
		get => _useReverse.Value;
		set => _useReverse.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EquilibriumCandlesPatternStrategy()
	{
		_equilibriumLength = Param(nameof(EquilibriumLength), 9)
			.SetDisplay("Equilibrium Length", "Lookback for equilibrium baseline", "General");

		_candlesForTrend = Param(nameof(CandlesForTrend), 7)
			.SetDisplay("Candles For Trend", "Consecutive candles to define trend", "General");

		_maxPullbackCandles = Param(nameof(MaxPullbackCandles), 2)
			.SetDisplay("Max Pullback Candles", "Opposite candles before exit", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR calculation period", "General");

		_stopMultiplier = Param(nameof(StopMultiplier), 2m)
			.SetDisplay("Stop Multiplier", "ATR multiplier for stop and target", "General");

		_useTpSl = Param(nameof(UseTpSl), true)
			.SetDisplay("Use TP/SL", "Enable stop loss and take profit", "General");

		_useBigCandleExit = Param(nameof(UseBigCandleExit), true)
			.SetDisplay("Big Candle Exit", "Close on big candle outside equilibrium", "General");

		_bigCandleMultiplier = Param(nameof(BigCandleMultiplier), 1m)
			.SetDisplay("Big Candle Multiplier", "ATR multiplier for big candle", "General");

		_useReverse = Param(nameof(UseReverse), false)
			.SetDisplay("Use Reverse", "Reverse trading direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_isBullTrend = false;
		_isBearTrend = false;
		_bullCandleCount = 0;
		_bearCandleCount = 0;
		_entryPrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels { Length = EquilibriumLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(donchian, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var equilibrium = middleBand;
		var equTop = equilibrium + atrValue * BigCandleMultiplier;
		var equBottom = equilibrium - atrValue * BigCandleMultiplier;

		var bullPrev = _isBullTrend;
		var bearPrev = _isBearTrend;

		if (candle.ClosePrice > equilibrium)
		{
			_bullCandleCount++;
			_bearCandleCount = 0;
			_isBearTrend = false;
		}
		else if (candle.ClosePrice < equilibrium)
		{
			_bearCandleCount++;
			_bullCandleCount = 0;
			_isBullTrend = false;
		}

		if (_bullCandleCount >= CandlesForTrend)
		{
			_isBullTrend = true;
			_bullCandleCount = 0;
		}
		if (_bearCandleCount >= CandlesForTrend)
		{
			_isBearTrend = true;
			_bearCandleCount = 0;
		}

		if (bullPrev && candle.ClosePrice < equilibrium)
		{
			if (UseReverse)
				SellMarket();
			else
				BuyMarket();

			_entryPrice = candle.ClosePrice;
			_isBullTrend = false;
		}
		else if (bearPrev && candle.ClosePrice > equilibrium)
		{
			if (UseReverse)
				BuyMarket();
			else
				SellMarket();

			_entryPrice = candle.ClosePrice;
			_isBearTrend = false;
		}

		if (Position > 0)
		{
			if (UseTpSl)
			{
				var stop = _entryPrice - atrValue * StopMultiplier;
				var take = _entryPrice + atrValue * StopMultiplier;
				if (candle.ClosePrice <= stop || candle.ClosePrice >= take)
					ClosePosition();
			}

			if (_bearCandleCount >= MaxPullbackCandles)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (UseTpSl)
			{
				var stop = _entryPrice + atrValue * StopMultiplier;
				var take = _entryPrice - atrValue * StopMultiplier;
				if (candle.ClosePrice >= stop || candle.ClosePrice <= take)
					ClosePosition();
			}

			if (_bullCandleCount >= MaxPullbackCandles)
				ClosePosition();
		}

		if (UseBigCandleExit && Position != 0)
		{
			if (candle.ClosePrice > equTop || candle.ClosePrice < equBottom)
				ClosePosition();
		}
	}
}

