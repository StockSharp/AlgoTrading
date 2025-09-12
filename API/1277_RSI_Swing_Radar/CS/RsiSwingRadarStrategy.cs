using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI SwingRadar strategy.
/// Buys when RSI crosses above its SMA after being oversold.
/// Uses ATR-based stop-loss and risk-reward target.
/// </summary>
public class RsiSwingRadarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _rsiMa;
	private AverageTrueRange _atr;

	private decimal _prevRsi;
	private decimal _prevDiff;
	private decimal _tradeStop;
	private decimal _tradeTarget;

	/// <summary>
	/// Reward-to-risk ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="RsiSwingRadarStrategy"/>.
	/// </summary>
	public RsiSwingRadarStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Risk:Reward", "Reward to risk ratio", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

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

		_prevRsi = 0m;
		_prevDiff = 0m;
		_tradeStop = 0m;
		_tradeTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = 14 };
		_rsiMa = new SimpleMovingAverage { Length = 14 };
		_atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _atr, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		var rsiArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}
		if (rsiArea != null)
		{
			DrawIndicator(rsiArea, _rsi);
			DrawIndicator(rsiArea, _rsiMa);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiMaValue = _rsiMa.Process(new DecimalIndicatorValue(_rsiMa, rsi)).ToDecimal();

		if (!_rsi.IsFormed || !_rsiMa.IsFormed || !_atr.IsFormed)
		{
			_prevRsi = rsi;
			_prevDiff = rsi - rsiMaValue;
			return;
		}

		var diff = rsi - rsiMaValue;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			_prevDiff = diff;
			return;
		}

		var crossOver = _prevDiff <= 0m && diff > 0m;

		if (Position == 0 && crossOver && _prevRsi < RsiOversold)
		{
			_tradeStop = candle.LowPrice - atr * AtrMultiplier;
			var stopSize = candle.ClosePrice - _tradeStop;
			_tradeTarget = candle.ClosePrice + stopSize * RiskReward;

			BuyMarket();
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _tradeStop || candle.HighPrice >= _tradeTarget)
			{
				SellMarket(Math.Abs(Position));
				_tradeStop = 0m;
				_tradeTarget = 0m;
			}
		}

		_prevRsi = rsi;
		_prevDiff = diff;
	}
}
