using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FTMO Rules Monitor strategy.
/// A bullish candle goes long and a bearish candle goes short, reversing an opposite position. The volume risks RiskPercent of
/// AccountSize over a stop AtrMultiplier ATRs from the entry, and that stop closes the position. The FTMO challenge rules are
/// watched against AccountSize: a 5% daily loss halts trading for the day, a 10% total loss ends it, and reaching the 10% profit
/// target after at least 4 trading days completes the challenge; ending or completing closes the position.
/// </summary>
public class FtmoRulesMonitorStrategy : Strategy
{
	private const decimal _maxDailyLossPercent = 5m;
	private const decimal _maxTotalLossPercent = 10m;
	private const decimal _profitTargetPercent = 10m;
	private const int _minTradingDays = 4;

	private readonly StrategyParam<decimal> _accountSize;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _dayStartPnL;
	private DateTime _lastTradingDay;
	private int _tradingDays;
	private bool _challengeOver;
	private decimal _stopPrice;

	/// <summary>
	/// Challenge account size.
	/// </summary>
	public decimal AccountSize
	{
		get => _accountSize.Value;
		set => _accountSize.Value = value;
	}

	/// <summary>
	/// Percent of the account risked per trade.
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
	/// Stop distance in ATRs.
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
	/// Constructor.
	/// </summary>
	public FtmoRulesMonitorStrategy()
	{
		_accountSize = Param(nameof(AccountSize), 10000m)
			.SetGreaterThanZero()
			.SetDisplay("Account Size", "Challenge account size", "Challenge");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percent of the account risked per trade", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Stop distance in ATRs", "Risk");

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
		_currentDay = default;
		_dayStartPnL = 0m;
		_lastTradingDay = default;
		_tradingDays = 0;
		_challengeOver = false;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_challengeOver)
			return;

		var day = candle.OpenTime.Date;
		if (_currentDay != day)
		{
			_currentDay = day;
			_dayStartPnL = PnL;
		}

		var totalLossHit = PnL <= -AccountSize * _maxTotalLossPercent / 100m;
		var targetReached = PnL >= AccountSize * _profitTargetPercent / 100m && _tradingDays >= _minTradingDays;

		if (totalLossHit || targetReached)
		{
			_challengeOver = true;
			ClosePosition();
			return;
		}

		if (PnL - _dayStartPnL <= -AccountSize * _maxDailyLossPercent / 100m)
		{
			ClosePosition();
			return;
		}

		if (Position > 0 && candle.LowPrice <= _stopPrice)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && candle.HighPrice >= _stopPrice)
		{
			BuyMarket(-Position);
			return;
		}

		var stopDistance = atr * AtrMultiplier;
		if (stopDistance <= 0)
			return;

		var bullish = candle.ClosePrice > candle.OpenPrice;
		var bearish = candle.ClosePrice < candle.OpenPrice;

		if (!bullish && !bearish)
			return;

		if ((bullish && Position > 0) || (bearish && Position < 0))
			return;

		var volume = RoundVolume(AccountSize * RiskPercent / 100m / stopDistance);
		if (volume <= 0)
			return;

		if (bullish)
		{
			BuyMarket(volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - stopDistance;
		}
		else
		{
			SellMarket(volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + stopDistance;
		}

		if (_lastTradingDay != day)
		{
			_lastTradingDay = day;
			_tradingDays++;
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0m;
		return step > 0 ? Math.Floor(volume / step) * step : volume;
	}
}
