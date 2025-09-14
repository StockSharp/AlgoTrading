
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MPM momentum strategy converted from MQL.
/// </summary>
public class MpmStrategy : Strategy
{
	private readonly StrategyParam<int> _progressiveCandles;
	private readonly StrategyParam<decimal> _progressiveSize;
	private readonly StrategyParam<decimal> _stopRatio;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _profitPerLot;
	private readonly StrategyParam<decimal> _breakEvenPerLot;
	private readonly StrategyParam<decimal> _lossPerLot;

	private int _bullCount;
	private int _bearCount;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public int ProgressiveCandles
	{
		get => _progressiveCandles.Value;
		set => _progressiveCandles.Value = value;
	}

	public decimal ProgressiveSize
	{
		get => _progressiveSize.Value;
		set => _progressiveSize.Value = value;
	}

	public decimal StopRatio
	{
		get => _stopRatio.Value;
		set => _stopRatio.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal ProfitPerLot
	{
		get => _profitPerLot.Value;
		set => _profitPerLot.Value = value;
	}

	public decimal BreakEvenPerLot
	{
		get => _breakEvenPerLot.Value;
		set => _breakEvenPerLot.Value = value;
	}

	public decimal LossPerLot
	{
		get => _lossPerLot.Value;
		set => _lossPerLot.Value = value;
	}

	public MpmStrategy()
	{
		_progressiveCandles = Param(nameof(ProgressiveCandles), 3)
			.SetDisplay("Progressive Candles", "Number of consecutive candles", "Signal");
		_progressiveSize = Param(nameof(ProgressiveSize), 0.6m)
			.SetDisplay("Progressive Size", "Minimal body size relative to ATR", "Signal");
		_stopRatio = Param(nameof(StopRatio), 0.05m)
			.SetDisplay("Stop Ratio", "Trailing stop ratio", "Risk");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Average True Range period", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_profitPerLot = Param(nameof(ProfitPerLot), 300m)
			.SetDisplay("Profit Per Lot", "Profit target per lot", "Risk");
		_breakEvenPerLot = Param(nameof(BreakEvenPerLot), 1m)
			.SetDisplay("BreakEven Per Lot", "Break even profit per lot", "Risk");
		_lossPerLot = Param(nameof(LossPerLot), 150m)
			.SetDisplay("Loss Per Lot", "Maximum loss per lot", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bullCount = 0;
		_bearCount = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

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

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		// Count consecutive bullish or bearish candles with sufficient body
		if (candle.ClosePrice > candle.OpenPrice && body >= atr * ProgressiveSize)
		{
				_bullCount++;
				_bearCount = 0;
		}
		else if (candle.ClosePrice < candle.OpenPrice && body >= atr * ProgressiveSize)
		{
				_bearCount++;
				_bullCount = 0;
		}
		else
		{
				_bullCount = 0;
				_bearCount = 0;
		}

		// Open long position after sequence of bullish candles
		if (Position <= 0 && _bullCount >= ProgressiveCandles)
		{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - atr * StopRatio;
				BuyMarket();
				return;
		}

		// Open short position after sequence of bearish candles
		if (Position >= 0 && _bearCount >= ProgressiveCandles)
		{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + atr * StopRatio;
				SellMarket();
				return;
		}

		if (Position > 0)
		{
				var profitPerLot = candle.ClosePrice - _entryPrice;
				if (profitPerLot >= ProfitPerLot || profitPerLot >= BreakEvenPerLot || profitPerLot <= -LossPerLot)
				{
					SellMarket();
					return;
				}

				var newStop = candle.ClosePrice - atr * StopRatio;
				if (newStop > _stopPrice)
					_stopPrice = newStop;

				if (candle.ClosePrice <= _stopPrice)
					SellMarket();
		}
		else if (Position < 0)
		{
				var profitPerLot = _entryPrice - candle.ClosePrice;
				if (profitPerLot >= ProfitPerLot || profitPerLot >= BreakEvenPerLot || profitPerLot <= -LossPerLot)
				{
					BuyMarket();
					return;
				}

				var newStop = candle.ClosePrice + atr * StopRatio;
				if (newStop < _stopPrice)
					_stopPrice = newStop;

				if (candle.ClosePrice >= _stopPrice)
					BuyMarket();
		}
	}
}
