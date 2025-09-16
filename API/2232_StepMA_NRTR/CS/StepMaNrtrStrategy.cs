using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StepMA NRTR trend-following strategy.
/// </summary>
public class StepMaNrtrStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _kv;
	private readonly StrategyParam<int> _stepSize;
	private readonly StrategyParam<bool> _useHighLow;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private readonly Queue<decimal> _ranges = new();
	private decimal _smax1;
	private decimal _smin1;
	private int _trend1;
	private bool _first = true;

	/// <summary>
	/// Volatility length.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Sensitivity factor.
	/// </summary>
	public decimal Kv { get => _kv.Value; set => _kv.Value = value; }

	/// <summary>
	/// Constant step size (0 - automatic).
	/// </summary>
	public int StepSize { get => _stepSize.Value; set => _stepSize.Value = value; }

	/// <summary>
	/// Use high/low range, otherwise close/close.
	/// </summary>
	public bool UseHighLow { get => _useHighLow.Value; set => _useHighLow.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public StepMaNrtrStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Volatility length", "Indicator");

		_kv = Param(nameof(Kv), 1m)
			.SetDisplay("Sensitivity", "Sensitivity factor", "Indicator");

		_stepSize = Param(nameof(StepSize), 0)
			.SetDisplay("Step Size", "Constant step size, 0 - auto", "Indicator");

		_useHighLow = Param(nameof(UseHighLow), true)
			.SetDisplay("Use High/Low", "Use high/low range, otherwise close/close", "Indicator");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
			.SetDisplay("Candle Type", "Candle type for processing", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Open Long", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Open Short", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		_ranges.Enqueue(range);

		if (_ranges.Count > Length)
			_ranges.Dequeue();

		if (_ranges.Count < Length)
			return;

		decimal step;

		if (StepSize == 0)
		{
			var atrMax = _ranges.Max();
			var atrMin = _ranges.Min();
			step = 0.5m * Kv * (atrMax + atrMin);
		}
		else
			step = Kv * StepSize;

		if (step == 0)
			return;

		var sizeP = step;
		var size2P = 2m * step;

		if (_first)
		{
			_trend1 = 0;
			_smax1 = candle.LowPrice + size2P;
			_smin1 = candle.HighPrice - size2P;
			_first = false;
		}

		decimal smax0, smin0;

		if (UseHighLow)
		{
			smax0 = candle.LowPrice + size2P;
			smin0 = candle.HighPrice - size2P;
		}
		else
		{
			smax0 = candle.ClosePrice + size2P;
			smin0 = candle.ClosePrice - size2P;
		}

		var trend0 = _trend1;

		if (candle.ClosePrice > _smax1)
			trend0 = 1;
		else if (candle.ClosePrice < _smin1)
			trend0 = -1;

		decimal stepMa;

		if (trend0 > 0)
		{
			if (smin0 < _smin1)
				smin0 = _smin1;
			stepMa = smin0 + sizeP;
		}
		else
		{
			if (smax0 > _smax1)
				smax0 = _smax1;
			stepMa = smax0 - sizeP;
		}

		var buySignal = trend0 > 0 && _trend1 < 0;
		var sellSignal = trend0 < 0 && _trend1 > 0;

		_smax1 = smax0;
		_smin1 = smin0;
		_trend1 = trend0;

		if (buySignal)
		{
			if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (BuyPosOpen && Position <= 0)
				BuyMarket();
		}
		else if (sellSignal)
		{
			if (BuyPosClose && Position > 0)
				SellMarket(Position);
			if (SellPosOpen && Position >= 0)
				SellMarket();
		}
	}
}
