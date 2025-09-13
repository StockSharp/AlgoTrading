using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger breakout strategy with RSI filter and trade direction control.
/// </summary>
public class BollingerBreakoutDirectionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiMidline;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _riskRewardRatio;
       private readonly StrategyParam<Sides?> _direction;

	private BollingerBands _bollinger = null!;
	private RelativeStrengthIndex _rsi = null!;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Trade direction.
	/// </summary>
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI midline value.
	/// </summary>
	public decimal RsiMidline { get => _rsiMidline.Value; set => _rsiMidline.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Risk/reward ratio.
	/// </summary>
	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }

	/// <summary>
	/// Trade direction filter.
	/// </summary>
	public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBreakoutDirectionStrategy"/> class.
	/// </summary>
	public BollingerBreakoutDirectionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger period", "Parameters");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Parameters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Parameters");

		_rsiMidline = Param(nameof(RsiMidline), 50m)
			.SetDisplay("RSI Midline", "RSI threshold", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk reward ratio", "Risk");

               _direction = Param(nameof(Direction), (Sides?)null)
                       .SetDisplay("Trade Direction", "Allowed side", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_rsi.IsFormed)
			return;
		var longAllowed = Direction is null or Sides.Buy;
		var shortAllowed = Direction is null or Sides.Sell;

		var longSignal = longAllowed && candle.ClosePrice > upperBand && rsiValue > RsiMidline && Position <= 0;
		var shortSignal = shortAllowed && candle.ClosePrice < lowerBand && rsiValue < RsiMidline && Position >= 0;

		if (longSignal)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1 - StopLossPercent / 100m);
			_takePrice = _entryPrice * (1 + (StopLossPercent / 100m * RiskRewardRatio));
			BuyMarket();
		}
		else if (shortSignal)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1 + StopLossPercent / 100m);
			_takePrice = _entryPrice * (1 - (StopLossPercent / 100m * RiskRewardRatio));
			SellMarket();
		}

		if (Position > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takePrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _takePrice)
				BuyMarket();
		}
	}
}
