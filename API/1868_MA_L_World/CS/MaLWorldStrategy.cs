using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with trailing EMA and fixed stop levels.
/// </summary>
public class MaLWorldStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _trailingMaPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private ExponentialMovingAverage _trailingMa = null!;
	private bool _initialized;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldownRemaining;

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public int TrailingMaPeriod
	{
		get => _trailingMaPeriod.Value;
		set => _trailingMaPeriod.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal MinSpreadPercent
	{
		get => _minSpreadPercent.Value;
		set => _minSpreadPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public MaLWorldStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Period of the fast weighted MA", "Parameters");

		_slowMaLength = Param(nameof(SlowMaLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Period of the slow weighted MA", "Parameters");

		_trailingMaPeriod = Param(nameof(TrailingMaPeriod), 92)
			.SetGreaterThanZero()
			.SetDisplay("Trailing EMA", "Period of trailing EMA", "Risk");

		_stopLoss = Param(nameof(StopLoss), 95m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Fixed stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 670m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Fixed take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.0008m)
			.SetDisplay("Minimum Spread %", "Minimum normalized spread between fast and slow MA", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_fastMa = null!;
		_slowMa = null!;
		_trailingMa = null!;
		_initialized = false;
		_prevFast = 0m;
		_prevSlow = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_trailingMa = new ExponentialMovingAverage { Length = TrailingMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, _trailingMa, ProcessCandle).Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _trailingMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal trail)
	{
		if (candle.State != CandleStates.Finished || !_fastMa.IsFormed || !_slowMa.IsFormed || !_trailingMa.IsFormed)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var spreadPercent = candle.ClosePrice != 0m ? Math.Abs(fast - slow) / candle.ClosePrice : 0m;
		var crossUp = _prevFast <= _prevSlow && fast > slow && spreadPercent >= MinSpreadPercent;
		var crossDown = _prevFast >= _prevSlow && fast < slow && spreadPercent >= MinSpreadPercent;

		if (_cooldownRemaining == 0)
		{
			if (crossUp && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;

		if (Position > 0 && candle.LowPrice <= trail)
		{
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && candle.HighPrice >= trail)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
	}
}

