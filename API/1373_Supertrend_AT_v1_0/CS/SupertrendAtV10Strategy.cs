using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class SupertrendAtV10Strategy : Strategy
{
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<decimal> _commissionPercent;
	private readonly StrategyParam<bool> _longEnabled;
	private readonly StrategyParam<bool> _shortEnabled;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _supertrend;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _prevLongStop;
	private decimal _prevShortStop;
	private int _prevDirection;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public int SupertrendLength { get => _supertrendLength.Value; set => _supertrendLength.Value = value; }
	public decimal SupertrendMultiplier { get => _supertrendMultiplier.Value; set => _supertrendMultiplier.Value = value; }
	public decimal RiskPerTrade { get => _riskPerTrade.Value; set => _riskPerTrade.Value = value; }
	public decimal RewardRatio { get => _rewardRatio.Value; set => _rewardRatio.Value = value; }
	public decimal CommissionPercent { get => _commissionPercent.Value; set => _commissionPercent.Value = value; }
	public bool LongEnabled { get => _longEnabled.Value; set => _longEnabled.Value = value; }
	public bool ShortEnabled { get => _shortEnabled.Value; set => _shortEnabled.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SupertrendAtV10Strategy()
	{
		_supertrendLength = Param(nameof(SupertrendLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "ATR period for Supertrend", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_riskPerTrade = Param(nameof(RiskPerTrade), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade (%)", "Risk percent per trade", "Risk");

		_rewardRatio = Param(nameof(RewardRatio), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Reward Ratio", "Take profit to stop loss ratio", "Risk");

		_commissionPercent = Param(nameof(CommissionPercent), 0.05m)
			.SetDisplay("Commission (%)", "Commission percent per trade", "Risk");

		_longEnabled = Param(nameof(LongEnabled), true)
			.SetDisplay("Enable Long", "Allow long positions", "General");

		_shortEnabled = Param(nameof(ShortEnabled), false)
			.SetDisplay("Enable Short", "Allow short positions", "General");

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

		_supertrend = null;
		_longStop = _shortStop = _prevLongStop = _prevShortStop = 0m;
		_prevDirection = 0;
		_entryPrice = _stopPrice = _targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_supertrend = new SuperTrend
		{
			Length = SupertrendLength,
			Multiplier = SupertrendMultiplier
		};

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_supertrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var direction = candle.ClosePrice > supertrendValue ? -1 : 1;

		if (direction > 0)
		{
			if (_longStop == 0m || candle.LowPrice < _longStop)
				_longStop = candle.LowPrice;
		}
		else
		{
			_prevLongStop = _longStop;
			_longStop = 0m;
		}

		if (direction < 0)
		{
			if (_shortStop == 0m || candle.HighPrice > _shortStop)
				_shortStop = candle.HighPrice;
		}
		else
		{
			_prevShortStop = _shortStop;
			_shortStop = 0m;
		}

		var commission = CommissionPercent / 100m;
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * (RiskPerTrade / 100m);

		if (direction < 0 && _prevDirection > 0 && Position == 0 && LongEnabled)
		{
			var stop = _prevLongStop;
			var risk = (candle.ClosePrice - stop) + (candle.ClosePrice + stop) * commission;
			if (stop > 0m && risk > 0m)
			{
				var volume = riskAmount / risk;
				if (volume > 0m)
				{
				_entryPrice = candle.ClosePrice;
				_stopPrice = stop;
				_targetPrice = (riskAmount * RewardRatio / volume + _entryPrice * (1 + commission)) / (1 - commission);
				BuyMarket(volume);
				SellLimit(volume, _targetPrice);
				SellStop(volume, _stopPrice);
				}
			}
		}
		else if (direction > 0 && _prevDirection < 0 && Position == 0 && ShortEnabled)
		{
			var stop = _prevShortStop;
			var risk = (stop - candle.ClosePrice) + (candle.ClosePrice + stop) * commission;
			if (stop > 0m && risk > 0m)
			{
				var volume = riskAmount / risk;
				if (volume > 0m)
				{
				_entryPrice = candle.ClosePrice;
				_stopPrice = stop;
				_targetPrice = (_entryPrice * (1 - commission) - riskAmount * RewardRatio / volume) / (1 + commission);
				SellMarket(volume);
				BuyLimit(volume, _targetPrice);
				BuyStop(volume, _stopPrice);
				}
			}
		}

		_prevDirection = direction;
	}
}
