using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price and RSI divergence using simple pivot detection.
/// </summary>
public class DivergenceStrategy : Strategy
{
	private readonly StrategyParam<Direction> _direction;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHighPrice;
	private decimal? _prevHighRsi;
	private decimal? _lastHighPrice;
	private decimal? _lastHighRsi;

	private decimal? _prevLowPrice;
	private decimal? _prevLowRsi;
	private decimal? _lastLowPrice;
	private decimal? _lastLowRsi;

	/// <summary>
	/// Trade direction.
	/// </summary>
	public Direction TradeDirection
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Risk reward ratio.
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
	/// Trade direction options.
	/// </summary>
	public enum Direction
	{
		Long,
		Short,
		Both,
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DivergenceStrategy"/>.
	/// </summary>
	public DivergenceStrategy()
	{
		_direction = Param(nameof(TradeDirection), Direction.Both)
			.SetDisplay("Direction", "Trade direction", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("Risk/Reward", "Take profit as multiple of stop loss", "Risk Management")
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
		_prevHighPrice = _prevHighRsi = _lastHighPrice = _lastHighRsi = null;
		_prevLowPrice = _prevLowRsi = _lastLowPrice = _lastLowRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var takeProfit = StopLossPercent * RiskReward;

		StartProtection(
			takeProfit: new Unit(takeProfit, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var rsi = rsiValue.ToDecimal();

		if (_lastHighPrice == null || price > _lastHighPrice)
		{
			_prevHighPrice = _lastHighPrice;
			_prevHighRsi = _lastHighRsi;
			_lastHighPrice = price;
			_lastHighRsi = rsi;

			if (_prevHighPrice != null && _prevHighRsi != null && _lastHighPrice > _prevHighPrice && _lastHighRsi < _prevHighRsi)
			{
				if ((TradeDirection == Direction.Short || TradeDirection == Direction.Both) && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					LogInfo($"Bearish divergence detected: price {_prevHighPrice}->{_lastHighPrice}, RSI {_prevHighRsi}->{_lastHighRsi}");
				}
			}
		}

		if (_lastLowPrice == null || price < _lastLowPrice)
		{
			_prevLowPrice = _lastLowPrice;
			_prevLowRsi = _lastLowRsi;
			_lastLowPrice = price;
			_lastLowRsi = rsi;

			if (_prevLowPrice != null && _prevLowRsi != null && _lastLowPrice < _prevLowPrice && _lastLowRsi > _prevLowRsi)
			{
				if ((TradeDirection == Direction.Long || TradeDirection == Direction.Both) && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					LogInfo($"Bullish divergence detected: price {_prevLowPrice}->{_lastLowPrice}, RSI {_prevLowRsi}->{_lastLowRsi}");
				}
			}
		}

		if (Position > 0 && rsi > 70)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long: RSI overbought at {rsi}");
		}
		else if (Position < 0 && rsi < 30)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: RSI oversold at {rsi}");
		}
	}
}
