using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Extreme Strength Reversal strategy converted from MQL.
/// Enters counter-trend trades when price pierces Bollinger Bands and RSI shows an extreme reading.
/// Uses percent-based risk sizing with fixed stop-loss and take-profit distances expressed in pips.
/// </summary>
public class ExtremeStrengthReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands? _bollinger;
	private RelativeStrengthIndex? _rsi;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _entryPrice;

	/// <summary>
	/// Risk percentage applied to portfolio equity for sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Bollinger Bands lookback period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold for RSI.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Oversold threshold for RSI.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ExtremeStrengthReversalStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_stopLossPips = Param(nameof(StopLossPips), 150)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50, 250, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 400, 20);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of candles used for Bollinger Bands.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.25m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of candles used for RSI.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 80m)
			.SetDisplay("RSI Overbought", "RSI level that marks extreme overbought conditions.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 20m)
			.SetDisplay("RSI Oversold", "RSI level that marks extreme oversold conditions.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe used for analysis.", "General");
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
		ResetTradeState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_bollinger is null || _rsi is null)
			return;

		if (!_bollinger.IsFormed || !_rsi.IsFormed)
			return;

		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		var closePrice = candle.ClosePrice;
		var openPrice = candle.OpenPrice;

		var bullishReversal = rsiValue < RsiOversold && rsiValue > 0m && candle.LowPrice < lowerBand && closePrice > openPrice;
		if (bullishReversal)
		{
			TryEnterLong(closePrice);
			return;
		}

		var bearishReversal = rsiValue > RsiOverbought && candle.HighPrice > upperBand && closePrice < openPrice;
		if (bearishReversal)
			TryEnterShort(closePrice);
	}

	private void TryEnterLong(decimal closePrice)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		if (Position < 0m)
			BuyMarket(-Position);

		BuyMarket(volume);
		_entryPrice = closePrice;
		_stopLossPrice = StopLossPips > 0 ? closePrice - GetPipOffset(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0 ? closePrice + GetPipOffset(TakeProfitPips) : null;
	}

	private void TryEnterShort(decimal closePrice)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		if (Position > 0m)
			SellMarket(Position);

		SellMarket(volume);
		_entryPrice = closePrice;
		_stopLossPrice = StopLossPips > 0 ? closePrice + GetPipOffset(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0 ? closePrice - GetPipOffset(TakeProfitPips) : null;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(Position);
				ResetTradeState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetTradeState();
			}
		}
		else if (Position < 0m)
		{
			var shortPosition = -Position;

			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(shortPosition);
				ResetTradeState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(shortPosition);
				ResetTradeState();
			}
		}
		else if (_stopLossPrice.HasValue || _takeProfitPrice.HasValue || _entryPrice.HasValue)
		{
			ResetTradeState();
		}
	}

	private decimal CalculateOrderVolume()
	{
		if (Volume > 0m)
			return Volume;

		if (RiskPercent <= 0m)
			return 0m;

		var stopDistance = GetStopDistance();
		if (stopDistance <= 0m)
			return 0m;

		var portfolio = Portfolio;
		if (portfolio is null)
			return 0m;

		var equity = portfolio.CurrentValue ?? portfolio.CurrentBalance ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		decimal perUnitRisk;
		if (priceStep > 0m && stepPrice > 0m)
		{
			perUnitRisk = stopDistance / priceStep * stepPrice;
		}
		else
		{
			perUnitRisk = stopDistance;
		}

		if (perUnitRisk <= 0m)
			return 0m;

		var rawVolume = riskAmount / perUnitRisk;
		if (rawVolume <= 0m)
			return 0m;

		rawVolume = NormalizeVolume(rawVolume);
		return rawVolume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var minVolume = Security?.VolumeMin ?? 0m;
		var maxVolume = Security?.VolumeMax ?? 0m;
		var step = Security?.VolumeStep ?? 0m;

		if (step > 0m && volume > 0m)
		{
			var steps = decimal.Floor(volume / step);
			if (steps <= 0m)
				steps = 1m;

			volume = steps * step;
		}

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return Math.Max(volume, 0m);
	}

	private decimal GetStopDistance()
	{
		if (StopLossPips <= 0)
			return 0m;

		return GetPipOffset(StopLossPips);
	}

	private decimal GetPipOffset(int pips)
	{
		var pipSize = GetPipSize();
		if (pipSize <= 0m)
			return 0m;

		return pips * pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		var decimals = Security?.Decimals;
		if (decimals.HasValue && decimals.Value > 0)
		{
			var value = Math.Pow(10, -decimals.Value);
			return Convert.ToDecimal(value);
		}

		return 0.0001m;
	}

	private void ResetTradeState()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
	}
}
