
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Engulfing & Pin Bar Breakout strategy.
/// Detects hammer/bullish engulfing or shooting star/bearish engulfing patterns
/// and trades breakouts on the next candle with risk-based position sizing.
/// </summary>
public class EngulfingPinBarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longProfitRatio;
	private readonly StrategyParam<decimal> _shortProfitRatio;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private bool _waitingForBullishEntry;
	private bool _waitingForBearishEntry;
	private decimal _signalHigh;
	private decimal _signalLow;
	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Risk/reward for long trades.
	/// </summary>
	public decimal LongProfitRatio
	{
		get => _longProfitRatio.Value;
		set => _longProfitRatio.Value = value;
	}

	/// <summary>
	/// Risk/reward for short trades.
	/// </summary>
	public decimal ShortProfitRatio
	{
		get => _shortProfitRatio.Value;
		set => _shortProfitRatio.Value = value;
	}

	/// <summary>
	/// Capital risk per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Timeframe for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EngulfingPinBarBreakoutStrategy"/>.
	/// </summary>
	public EngulfingPinBarBreakoutStrategy()
	{
		_longProfitRatio = Param(nameof(LongProfitRatio), 5m)
			.SetDisplay("Long Profit Ratio", "Risk/reward for long trades", "General")
			.SetCanOptimize(true);

		_shortProfitRatio = Param(nameof(ShortProfitRatio), 4m)
			.SetDisplay("Short Profit Ratio", "Risk/reward for short trades", "General")
			.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 0.02m)
			.SetDisplay("Risk Percent", "Capital risk per trade", "Money Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_waitingForBullishEntry = false;
		_waitingForBearishEntry = false;
		_signalHigh = 0m;
		_signalLow = 0m;
		_prevOpen = 0m;
		_prevClose = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			var entered = false;

			if (_waitingForBullishEntry && candle.HighPrice > _signalHigh)
			{
				var entryPrice = _signalHigh;
				var stopLossPrice = _signalLow;
				var riskPerUnit = entryPrice - stopLossPrice;
				var capitalToRisk = Portfolio.CurrentValue * RiskPercent;
				var positionSize = riskPerUnit > 0 ? capitalToRisk / riskPerUnit : 0m;

				if (positionSize > 0 && IsFormedAndOnlineAndAllowTrading())
				{
					BuyMarket(positionSize);
					_stopPrice = stopLossPrice;
					_takePrice = entryPrice + riskPerUnit * LongProfitRatio;
				}

				_waitingForBullishEntry = false;
				entered = true;
			}
			else if (_waitingForBearishEntry && candle.LowPrice < _signalLow)
			{
				var entryPrice = _signalLow;
				var stopLossPrice = _signalHigh;
				var riskPerUnit = stopLossPrice - entryPrice;
				var capitalToRisk = Portfolio.CurrentValue * RiskPercent;
				var positionSize = riskPerUnit > 0 ? capitalToRisk / riskPerUnit : 0m;

				if (positionSize > 0 && IsFormedAndOnlineAndAllowTrading())
				{
					SellMarket(positionSize);
					_stopPrice = stopLossPrice;
					_takePrice = entryPrice - riskPerUnit * ShortProfitRatio;
				}

				_waitingForBearishEntry = false;
				entered = true;
			}

			if (!entered)
			{
				_waitingForBullishEntry = false;
				_waitingForBearishEntry = false;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
				_takePrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				_stopPrice = 0m;
				_takePrice = 0m;
			}
		}

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var isHammer = lowerWick > bodySize * 2m && upperWick < bodySize * 0.5m;
		var isBullishEngulfing = _prevClose < _prevOpen && candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > _prevOpen && candle.OpenPrice < _prevClose;
		var isBullishSignal = isHammer || isBullishEngulfing;

		var isShootingStar = upperWick > bodySize * 2m && lowerWick < bodySize * 0.5m;
		var isBearishEngulfing = _prevClose > _prevOpen && candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < _prevOpen && candle.OpenPrice > _prevClose;
		var isBearishSignal = isShootingStar || isBearishEngulfing;

		if (isBullishSignal)
		{
			_waitingForBullishEntry = true;
			_waitingForBearishEntry = false;
			_signalHigh = candle.HighPrice;
			_signalLow = candle.LowPrice;
		}
		else if (isBearishSignal)
		{
			_waitingForBearishEntry = true;
			_waitingForBullishEntry = false;
			_signalHigh = candle.HighPrice;
			_signalLow = candle.LowPrice;
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}
