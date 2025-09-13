using System;
using System.Collections.Generic;

using StockSharp.Algo;
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

	private WeightedMovingAverage _fastMa;
	private WeightedMovingAverage _slowMa;
	private ExponentialMovingAverage _trailingMa;

	private bool _initialized;
	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Trailing EMA period.
	/// </summary>
	public int TrailingMaPeriod
	{
		get => _trailingMaPeriod.Value;
		set => _trailingMaPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MaLWorldStrategy"/>.
	/// </summary>
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
		_fastMa = null;
		_slowMa = null;
		_trailingMa = null;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength, CandlePrice = CandlePrice.Close };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength, CandlePrice = CandlePrice.Close };
		_trailingMa = new ExponentialMovingAverage { Length = TrailingMaPeriod, CandlePrice = CandlePrice.Close };

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
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_trailingMa.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevFast = fast;
		_prevSlow = slow;

		if (Position > 0 && candle.LowPrice <= trail)
			SellMarket(Position);
		else if (Position < 0 && candle.HighPrice >= trail)
			BuyMarket(-Position);
	}
}
