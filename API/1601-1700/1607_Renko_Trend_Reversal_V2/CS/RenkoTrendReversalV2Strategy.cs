using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Renko reversal strategy. Brick size follows ATR of the source candles.
/// </summary>
public class RenkoTrendReversalV2Strategy : Strategy
{
	private readonly StrategyParam<int> _renkoAtrLength;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<bool> _allowShorts;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _brickClose;
	private int _lastBrickDirection;

	public int RenkoAtrLength { get => _renkoAtrLength.Value; set => _renkoAtrLength.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public bool AllowShorts { get => _allowShorts.Value; set => _allowShorts.Value = value; }
	public TimeSpan TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }
	public TimeSpan TradeEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RenkoTrendReversalV2Strategy()
	{
		_renkoAtrLength = Param(nameof(RenkoAtrLength), 10).SetGreaterThanZero()
			.SetDisplay("Renko ATR Length", "ATR period used as adaptive brick size.", "Renko");
		_stopLossPct = Param(nameof(StopLossPct), 3m).SetNotNegative()
			.SetDisplay("Stop Loss %", "Protective stop in percent.", "Protection");
		_takeProfitPct = Param(nameof(TakeProfitPct), 20m).SetNotNegative()
			.SetDisplay("Take Profit %", "Profit target in percent.", "Protection");
		_allowShorts = Param(nameof(AllowShorts), true)
			.SetDisplay("Allow Shorts", "Allow bearish Renko reversals to open short positions.", "Trading");
		_tradeStart = Param(nameof(TradeStart), TimeSpan.Zero)
			.SetDisplay("Trade Start", "Start of the allowed entry window.", "Timing");
		_tradeEnd = Param(nameof(TradeEnd), new TimeSpan(23, 59, 59))
			.SetDisplay("Trade End", "End of the allowed entry window.", "Timing");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Source Candle Type", "Source candles used to calculate ATR and build adaptive Renko bricks.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_brickClose = null;
		_lastBrickDirection = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (TakeProfitPct > 0m || StopLossPct > 0m)
		{
			StartProtection(
				TakeProfitPct > 0m ? new Unit(TakeProfitPct, UnitTypes.Percent) : null,
				StopLossPct > 0m ? new Unit(StopLossPct, UnitTypes.Percent) : null);
		}

		var atr = new AverageTrueRange { Length = RenkoAtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, (candle, atrValue) =>
		{
			if (candle.State != CandleStates.Finished || !atr.IsFormed || atrValue <= 0m)
				return;

			ProcessAdaptiveRenko(candle, atrValue);
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessAdaptiveRenko(ICandleMessage candle, decimal brickSize)
	{
		if (_brickClose is null)
		{
			_brickClose = candle.ClosePrice;
			return;
		}

		// A large source bar can form several Renko bricks. Each brick has an explicit
		// open/close relationship; a change of that relationship is the reversal signal.
		while (candle.ClosePrice >= _brickClose.Value + brickSize)
		{
			var open = _brickClose.Value;
			var close = open + brickSize;
			ProcessBrick(candle.OpenTime, open, close);
			_brickClose = close;
		}

		while (candle.ClosePrice <= _brickClose.Value - brickSize)
		{
			var open = _brickClose.Value;
			var close = open - brickSize;
			ProcessBrick(candle.OpenTime, open, close);
			_brickClose = close;
		}
	}

	private void ProcessBrick(DateTimeOffset time, decimal open, decimal close)
	{
		var direction = close > open ? 1 : -1;
		var reversal = _lastBrickDirection != 0 && direction != _lastBrickDirection;
		_lastBrickDirection = direction;

		if (!reversal || !InTradeWindow(time.TimeOfDay))
			return;

		if (direction > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (direction < 0 && AllowShorts && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (direction < 0 && !AllowShorts && Position > 0)
			SellMarket(Math.Abs(Position));
	}

	private bool InTradeWindow(TimeSpan time)
		=> TradeStart <= TradeEnd
			? time >= TradeStart && time <= TradeEnd
			: time >= TradeStart || time <= TradeEnd;
}
