using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that tracks FTMO challenge rules.
/// </summary>
public class FtmoRulesMonitorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accountSize;
	private readonly StrategyParam<bool> _isChallengePhase;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dailyPnL;
	private decimal _maxDailyDrawdown;
	private decimal _peakPnL;
	private decimal _maxDrawdown;
	private int _currentDay;
	private int _tradingDays;
	private int _consecutiveTradingDays;
	private int _maxConsecutiveDays;

	/// <summary>
	/// Account size in dollars.
	/// </summary>
	public decimal AccountSize
	{
		get => _accountSize.Value;
		set => _accountSize.Value = value;
	}

	/// <summary>
	/// Challenge phase flag.
	/// </summary>
	public bool IsChallengePhase
	{
		get => _isChallengePhase.Value;
		set => _isChallengePhase.Value = value;
	}

	/// <summary>
	/// Risk percent per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
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
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initializes a new instance of the <see cref="FtmoRulesMonitorStrategy"/>.
	/// </summary>
	public FtmoRulesMonitorStrategy()
	{
		_accountSize = Param(nameof(AccountSize), 10000m).SetDisplay("Account Size ($)", "FTMO account size", "FTMO");

		_isChallengePhase =
			Param(nameof(IsChallengePhase), true).SetDisplay("Is Challenge Phase?", "Challenge phase flag", "FTMO");

		_riskPercent = Param(nameof(RiskPercent), 1m)
						   .SetRange(0.1m, 10m)
						   .SetDisplay("Risk Percent", "Risk percent per trade", "Strategy")
						   .SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
						 .SetRange(5, 50)
						 .SetDisplay("ATR Period", "ATR calculation period", "Indicators")
						 .SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
							 .SetRange(0.5m, 5m)
							 .SetDisplay("ATR Multiplier", "ATR multiplier for stop calculation", "Strategy")
							 .SetCanOptimize(true);

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

		_dailyPnL = 0m;
		_maxDailyDrawdown = 0m;
		_peakPnL = 0m;
		_maxDrawdown = 0m;
		_currentDay = 0;
		_tradingDays = 0;
		_consecutiveTradingDays = 0;
		_maxConsecutiveDays = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Day;
		if (day != _currentDay)
		{
			_currentDay = day;
			_dailyPnL = PnL;
			_maxDailyDrawdown = 0m;
			_tradingDays++;
			_consecutiveTradingDays++;
			if (_consecutiveTradingDays > _maxConsecutiveDays)
				_maxConsecutiveDays = _consecutiveTradingDays;
		}

		var stopLoss = AtrMultiplier * atr;

		if (Position == 0)
		{
			var volume = Volume;
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket(volume);
			}
			else if (candle.ClosePrice < candle.OpenPrice)
			{
				SellMarket(volume);
			}
		}

		var realizedPnL = PnL;
		var dayPnL = realizedPnL - _dailyPnL;
		if (dayPnL < _maxDailyDrawdown)
			_maxDailyDrawdown = dayPnL;

		if (realizedPnL > _peakPnL)
			_peakPnL = realizedPnL;

		var drawdown = realizedPnL - _peakPnL;
		if (drawdown < _maxDrawdown)
			_maxDrawdown = drawdown;

		var isDailyLossOk = _maxDailyDrawdown > GetMaxDailyLoss(AccountSize);
		var isTotalLossOk = _maxDrawdown > GetMaxTotalLoss(AccountSize);
		var isProfitTargetMet = realizedPnL >= GetProfitTarget(AccountSize);
		var isMinTradingDaysMet = _tradingDays >= 4;

		if (isProfitTargetMet && isMinTradingDaysMet && isDailyLossOk && isTotalLossOk)
		{
			CloseAll("Challenge Complete");
		}
	}

	private static decimal GetMaxDailyLoss(decimal size) => size switch {
		10000m => -500m,
		25000m => -1250m,
		50000m => -2500m,
		100000m => -5000m,
		_ => -10000m,
	};

	private static decimal GetMaxTotalLoss(decimal size) => size switch {
		10000m => -1000m,
		25000m => -2500m,
		50000m => -5000m,
		100000m => -10000m,
		_ => -20000m,
	};

	private static decimal GetProfitTarget(decimal size) => size switch {
		10000m => 1000m,
		25000m => 2500m,
		50000m => 5000m,
		100000m => 10000m,
		_ => 20000m,
	};
}
