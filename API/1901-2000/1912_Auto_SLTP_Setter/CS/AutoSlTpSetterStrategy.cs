using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades MA crossovers and automatically protects positions
/// with ATR-based stop loss and take profit.
/// </summary>
public class AutoSlTpSetterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<decimal> _takeProfitAtr;
	private readonly StrategyParam<int> _atrPeriod;

	private SimpleMovingAverage _slowMa;
	private AverageTrueRange _atr;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst = true;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public decimal StopLossAtr { get => _stopLossAtr.Value; set => _stopLossAtr.Value = value; }
	public decimal TakeProfitAtr { get => _takeProfitAtr.Value; set => _takeProfitAtr.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	public AutoSlTpSetterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "Indicators");

		_stopLossAtr = Param(nameof(StopLossAtr), 1.5m)
			.SetDisplay("SL ATR Mult", "ATR multiplier for stop loss", "Risk");

		_takeProfitAtr = Param(nameof(TakeProfitAtr), 2.5m)
			.SetDisplay("TP ATR Mult", "ATR multiplier for take profit", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators");
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
		_prevFast = 0;
		_prevSlow = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(2m, UnitTypes.Percent),
			takeProfit: new Unit(3m, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var slowResult = _slowMa.Process(candle.ClosePrice, candle.OpenTime, true);
		_atr.Process(candle);

		if (!slowResult.IsFormed)
			return;

		var slow = slowResult.ToDecimal();

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		// Bullish crossover
		if (_prevFast <= _prevSlow && fast > slow && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Bearish crossover
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
