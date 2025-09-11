using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based template strategy with risk managed position sizing and configurable stops.
/// </summary>
public class RBasedStrategyTemplateStrategy : Strategy
{
	public enum StopLossTypes
	{
		Fixed,
		Atr,
		Percentage,
		Ticks
	}

	private readonly StrategyParam<decimal> _riskPerTradePercent;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<StopLossTypes> _stopLossType;
	private readonly StrategyParam<decimal> _slValue;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _tpRValue;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _entryPrice;
	private decimal _slPrice;
	private decimal _tpPrice;

	public decimal RiskPerTradePercent { get => _riskPerTradePercent.Value; set => _riskPerTradePercent.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public int OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }
	public StopLossTypes StopLossType { get => _stopLossType.Value; set => _stopLossType.Value = value; }
	public decimal SlValue { get => _slValue.Value; set => _slValue.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal TpRValue { get => _tpRValue.Value; set => _tpRValue.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RBasedStrategyTemplateStrategy()
	{
		_riskPerTradePercent = Param(nameof(RiskPerTradePercent), 1m)
		.SetDisplay("Risk per trade (%)", "Percent of capital risked", "Risk Management")
		.SetGreaterThanZero();

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "RSI calculation period", "RSI Settings")
		.SetGreaterThanZero();

		_oversoldLevel = Param(nameof(OversoldLevel), 30)
		.SetDisplay("Oversold Level", "RSI oversold threshold", "RSI Settings");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70)
		.SetDisplay("Overbought Level", "RSI overbought threshold", "RSI Settings");

		_stopLossType = Param(nameof(StopLossType), StopLossTypes.Fixed)
		.SetDisplay("Stop Loss Type", "Calculation method for stop loss", "Stop Loss & Take Profit");

		_slValue = Param(nameof(SlValue), 100m)
		.SetDisplay("SL Value", "Stop loss value", "Stop Loss & Take Profit")
		.SetGreaterThanZero();

		_atrLength = Param(nameof(AtrLength), 14)
		.SetDisplay("ATR Length", "ATR calculation period", "Stop Loss & Take Profit")
		.SetGreaterThanZero();

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetDisplay("ATR Multiplier", "ATR multiplier", "Stop Loss & Take Profit")
		.SetGreaterThanZero();

		_tpRValue = Param(nameof(TpRValue), 2m)
		.SetDisplay("TP R Value", "Risk to reward multiplier", "Stop Loss & Take Profit")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0m;
		_entryPrice = 0m;
		_slPrice = 0m;
		_tpPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, atr, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _slPrice)
			{
				SellMarket(Position);
			}
			else if (candle.HighPrice >= _tpPrice)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _slPrice)
			{
				BuyMarket(-Position);
			}
			else if (candle.LowPrice <= _tpPrice)
			{
				BuyMarket(-Position);
			}
		}

		var longCondition = _prevRsi >= OversoldLevel && rsiValue < OversoldLevel;
		var shortCondition = _prevRsi <= OverboughtLevel && rsiValue > OverboughtLevel;

		if (longCondition && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_slPrice = CalculateStop(_entryPrice, true, atrValue);
			_tpPrice = CalculateTake(_entryPrice, _slPrice, true);
			var volume = CalculatePositionSize(_entryPrice, _slPrice);
			BuyMarket(volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_slPrice = CalculateStop(_entryPrice, false, atrValue);
			_tpPrice = CalculateTake(_entryPrice, _slPrice, false);
			var volume = CalculatePositionSize(_entryPrice, _slPrice);
			SellMarket(volume + Math.Abs(Position));
		}

		_prevRsi = rsiValue;
	}

	private decimal CalculateStop(decimal entry, bool isLong, decimal atrValue)
	{
		return StopLossType switch
		{
			StopLossTypes.Fixed => isLong ? entry - SlValue : entry + SlValue,
			StopLossTypes.Atr => isLong ? entry - atrValue * AtrMultiplier : entry + atrValue * AtrMultiplier,
			StopLossTypes.Percentage => isLong ? entry - entry * SlValue / 100m : entry + entry * SlValue / 100m,
			StopLossTypes.Ticks => isLong ? entry - SlValue * (Security.PriceStep ?? 1m) : entry + SlValue * (Security.PriceStep ?? 1m),
			_ => entry
		};
	}

	private decimal CalculateTake(decimal entry, decimal stop, bool isLong)
	{
		var risk = Math.Abs(entry - stop);
		return isLong ? entry + risk * TpRValue : entry - risk * TpRValue;
	}

	private decimal CalculatePositionSize(decimal entry, decimal stop)
	{
		var slDistance = Math.Abs(entry - stop);
		if (slDistance <= 0m)
		return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * (RiskPerTradePercent / 100m);
		return riskAmount / slDistance;
	}
}
