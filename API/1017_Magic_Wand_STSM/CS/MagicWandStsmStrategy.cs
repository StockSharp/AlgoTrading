using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MagicWandStsmStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _cooldownBars;
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
	private int _barsFromTrade;

	public int SupertrendPeriod { get => _supertrendPeriod.Value; set => _supertrendPeriod.Value = value; }
	public decimal SupertrendMultiplier { get => _supertrendMultiplier.Value; set => _supertrendMultiplier.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MagicWandStsmStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10);
		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m);
		_maLength = Param(nameof(MaLength), 50);
		_riskReward = Param(nameof(RiskReward), 3m);
		_cooldownBars = Param(nameof(CooldownBars), 80);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame());
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

		_atr = null;
		_sma = null;
		_prevHighest = 0m;
		_prevLowest = 0m;
		_prevSupertrend = 0m;
		_prevClose = 0m;
		_isFirst = true;
		_stop = 0m;
		_take = 0m;
		_barsFromTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = SupertrendPeriod };
		_sma = new SimpleMovingAverage { Length = MaLength };

		_prevHighest = _prevLowest = _prevSupertrend = _prevClose = 0m;
		_stop = _take = 0m;
		_isFirst = true;
		_barsFromTrade = CooldownBars;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_atr, _sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed || !_sma.IsFormed)
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
		_barsFromTrade++;
		var canEnter = _barsFromTrade >= CooldownBars;

		if (Position == 0 && canEnter)
		{
			if (isUpTrend && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_stop = supertrend;
				_take = candle.ClosePrice + (candle.ClosePrice - _stop) * RiskReward;
				_barsFromTrade = 0;
			}
			else if (!isUpTrend && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_stop = supertrend;
				_take = candle.ClosePrice - (_stop - candle.ClosePrice) * RiskReward;
				_barsFromTrade = 0;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stop || candle.ClosePrice >= _take)
			{
				SellMarket();
				_barsFromTrade = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stop || candle.ClosePrice <= _take)
			{
				BuyMarket();
				_barsFromTrade = 0;
			}
		}

		_prevHighest = currentUpper;
		_prevLowest = currentLower;
		_prevSupertrend = supertrend;
		_prevClose = candle.ClosePrice;
	}
}
