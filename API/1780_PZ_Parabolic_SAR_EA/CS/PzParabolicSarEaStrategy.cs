using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with ATR based risk management.
/// Implements entry and exit SAR settings, optional trailing stop,
/// partial closing and break-even logic.
/// </summary>
public class PzParabolicSarEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeStep;
	private readonly StrategyParam<decimal> _tradeMax;
	private readonly StrategyParam<decimal> _stopStep;
	private readonly StrategyParam<decimal> _stopMax;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<int> _trailingAtrPeriod;
	private readonly StrategyParam<decimal> _trailingAtrMultiplier;
	private readonly StrategyParam<bool> _partialClosing;
	private readonly StrategyParam<decimal> _percentageToClose;
	private readonly StrategyParam<bool> _breakEven;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _partialClosed;
	private decimal _trailStop;

	public decimal TradeStep { get => _tradeStep.Value; set => _tradeStep.Value = value; }
	public decimal TradeMax { get => _tradeMax.Value; set => _tradeMax.Value = value; }
	public decimal StopStep { get => _stopStep.Value; set => _stopStep.Value = value; }
	public decimal StopMax { get => _stopMax.Value; set => _stopMax.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }
	public int TrailingAtrPeriod { get => _trailingAtrPeriod.Value; set => _trailingAtrPeriod.Value = value; }
	public decimal TrailingAtrMultiplier { get => _trailingAtrMultiplier.Value; set => _trailingAtrMultiplier.Value = value; }
	public bool PartialClosing { get => _partialClosing.Value; set => _partialClosing.Value = value; }
	public decimal PercentageToClose { get => _percentageToClose.Value; set => _percentageToClose.Value = value; }
	public bool BreakEven { get => _breakEven.Value; set => _breakEven.Value = value; }
	public decimal LotSize { get => _lotSize.Value; set => _lotSize.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PzParabolicSarEaStrategy()
	{
		_tradeStep = Param(nameof(TradeStep), 0.002m)
			.SetDisplay("Trade SAR Step", "Acceleration step for entry SAR", "Indicators");
		_tradeMax = Param(nameof(TradeMax), 0.2m)
			.SetDisplay("Trade SAR Max", "Maximum acceleration for entry SAR", "Indicators");
		_stopStep = Param(nameof(StopStep), 0.004m)
			.SetDisplay("Stop SAR Step", "Acceleration step for exit SAR", "Indicators");
		_stopMax = Param(nameof(StopMax), 0.4m)
			.SetDisplay("Stop SAR Max", "Maximum acceleration for exit SAR", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 30)
			.SetDisplay("ATR Period", "ATR period for stop distance", "Risk");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2.5m)
			.SetDisplay("ATR Mult", "ATR multiplier for stop distance", "Risk");
		_useTrailing = Param(nameof(UseTrailing), false)
			.SetDisplay("Use Trailing", "Enable ATR trailing stop", "Risk");
		_trailingAtrPeriod = Param(nameof(TrailingAtrPeriod), 30)
			.SetDisplay("Trail ATR Period", "ATR period for trailing stop", "Risk");
		_trailingAtrMultiplier = Param(nameof(TrailingAtrMultiplier), 1.75m)
			.SetDisplay("Trail ATR Mult", "ATR multiplier for trailing stop", "Risk");
		_partialClosing = Param(nameof(PartialClosing), true)
			.SetDisplay("Partial Close", "Enable partial closing", "Risk");
		_percentageToClose = Param(nameof(PercentageToClose), 0.5m)
			.SetDisplay("Close %", "Part of position to close", "Risk");
		_breakEven = Param(nameof(BreakEven), true)
			.SetDisplay("Break Even", "Move stop to breakeven on partial close", "Risk");
		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetDisplay("Lot Size", "Base volume to trade", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_entryPrice = 0m;
		_partialClosed = false;
		_trailStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = LotSize;

		var tradeSar = new ParabolicSar { AccelerationStep = TradeStep, AccelerationMax = TradeMax };
		var stopSar = new ParabolicSar { AccelerationStep = StopStep, AccelerationMax = StopMax };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var trailAtr = new AverageTrueRange { Length = TrailingAtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(tradeSar, stopSar, atr, trailAtr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tradeSar);
			DrawIndicator(area, stopSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tradeSar, decimal stopSar, decimal atr, decimal trailAtr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Exit on stop SAR flip
		if (Position > 0 && stopSar > candle.ClosePrice)
		{
		ClosePosition();
		}
		else if (Position < 0 && stopSar < candle.ClosePrice)
		{
		ClosePosition();
		}

		// Trailing stop management
		if (UseTrailing && Position != 0)
		{
		if (Position > 0)
		{
		var newStop = candle.ClosePrice - trailAtr * TrailingAtrMultiplier;
		if (newStop > _trailStop)
		_trailStop = newStop;

		if (candle.LowPrice <= _trailStop)
		ClosePosition();
		}
		else
		{
		var newStop = candle.ClosePrice + trailAtr * TrailingAtrMultiplier;
		if (_trailStop == 0m || newStop < _trailStop)
		_trailStop = newStop;

		if (candle.HighPrice >= _trailStop)
		ClosePosition();
		}
		}

		// Partial closing and break even
		if (Position != 0 && !_partialClosed && PartialClosing)
		{
		var profit = Position > 0 ? candle.ClosePrice - _entryPrice : _entryPrice - candle.ClosePrice;
		var stopDistance = atr * AtrMultiplier;

		if (profit > stopDistance)
		{
		var volumeToClose = Math.Abs(Position) * PercentageToClose;
		if (Position > 0)
		SellMarket(volumeToClose);
		else
		BuyMarket(volumeToClose);

		if (BreakEven)
		_trailStop = _entryPrice;

		_partialClosed = true;
		}
		}

		// Entry logic
		if (Position <= 0 && tradeSar < candle.ClosePrice && stopSar < candle.ClosePrice)
		{
		BuyMarket();
		_entryPrice = candle.ClosePrice;
		_partialClosed = false;
		_trailStop = candle.ClosePrice - atr * AtrMultiplier;
		}
		else if (Position >= 0 && tradeSar > candle.ClosePrice && stopSar > candle.ClosePrice)
		{
		SellMarket();
		_entryPrice = candle.ClosePrice;
		_partialClosed = false;
		_trailStop = candle.ClosePrice + atr * AtrMultiplier;
		}
	}
}
