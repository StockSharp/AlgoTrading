using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend strategy with SMA200 filter and risk/reward exits.
/// Based on TradingView "magic wand stsm".
/// </summary>
public class MagicWandStsmStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _sma;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal _prevSupertrend;
	private decimal _prevClose;
	private bool _isFirst;
	private decimal _stop;
	private decimal _take;

	/// <summary>
	/// Supertrend ATR period.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend ATR multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Length of simple moving average filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Risk/reward ratio for take profit.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
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
	/// Initializes a new instance of the <see cref="MagicWandStsmStrategy"/> class.
	/// </summary>
	public MagicWandStsmStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetDisplay("Supertrend Period", "ATR period for Supertrend", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 200)
			.SetDisplay("MA Length", "Simple moving average length", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("Risk Reward", "Take profit multiplier", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevHighest = _prevLowest = _prevSupertrend = _prevClose = 0m;
		_stop = _take = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = SupertrendPeriod };
		_sma = new SimpleMovingAverage { Length = MaLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_atr, _sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upperBand = median + SupertrendMultiplier * atrValue;
		var lowerBand = median - SupertrendMultiplier * atrValue;

		if (_isFirst)
		{
			_prevHighest = upperBand;
			_prevLowest = lowerBand;
			_prevSupertrend = candle.ClosePrice <= upperBand ? upperBand : lowerBand;
			_prevClose = candle.ClosePrice;
			_isFirst = false;
			return;
		}

		var currentUpper = (upperBand < _prevHighest || _prevClose > _prevHighest) ? upperBand : _prevHighest;
		var currentLower = (lowerBand > _prevLowest || _prevClose < _prevLowest) ? lowerBand : _prevLowest;

		var supertrend = _prevSupertrend == _prevHighest
			? (candle.ClosePrice <= currentUpper ? currentUpper : currentLower)
			: (candle.ClosePrice >= currentLower ? currentLower : currentUpper);

		var isUpTrend = candle.ClosePrice > supertrend;

		if (Position == 0)
		{
			if (isUpTrend && candle.ClosePrice > smaValue)
			{
			BuyMarket();
			_stop = supertrend;
			_take = candle.ClosePrice + (candle.ClosePrice - _stop) * RiskReward;
			}
			else if (!isUpTrend && candle.ClosePrice < smaValue)
			{
			SellMarket();
			_stop = supertrend;
			_take = candle.ClosePrice - (_stop - candle.ClosePrice) * RiskReward;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stop || candle.ClosePrice >= _take)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stop || candle.ClosePrice <= _take)
			BuyMarket(-Position);
		}

		_prevHighest = currentUpper;
		_prevLowest = currentLower;
		_prevSupertrend = supertrend;
		_prevClose = candle.ClosePrice;
	}
}
