using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elliott Wave Oscillator based strategy.
/// Buys when the oscillator turns upward.
/// Sells when the oscillator turns downward.
/// </summary>
public class ElliottWaveOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEwo;
	private decimal _prevPrevEwo;
	private bool _isFirstValue;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ElliottWaveOscillatorStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Length of the fast SMA", "Indicator")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 35)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Length of the slow SMA", "Indicator")
			.SetCanOptimize(true);

		_takeProfitPct = Param(nameof(TakeProfitPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Percentage take profit", "Risk")
			.SetCanOptimize(true);

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevEwo = 0m;
		_prevPrevEwo = 0m;
		_isFirstValue = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfitPct / 100m, UnitTypes.Percent), new Unit(StopLossPct / 100m, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ewoValue = fastValue - slowValue;

		if (_isFirstValue)
		{
			_prevEwo = ewoValue;
			_prevPrevEwo = ewoValue;
			_isFirstValue = false;
			return;
		}

		if (_prevEwo < _prevPrevEwo && ewoValue > _prevEwo)
		{
			// Oscillator turns upward - open long
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevEwo > _prevPrevEwo && ewoValue < _prevEwo)
		{
			// Oscillator turns downward - open short
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevPrevEwo = _prevEwo;
		_prevEwo = ewoValue;
	}
}
