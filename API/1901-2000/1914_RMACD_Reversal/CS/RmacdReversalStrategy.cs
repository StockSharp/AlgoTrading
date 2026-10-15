using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD signal crossover with several reversal entry modes.
/// </summary>
public class RmacdReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<AlgModes> _mode;

	private decimal _prevMacd;
	private decimal _prevMacd2;
	private decimal _prevSignal;
	private decimal _prevSignal2;
	private int _initialized;
	private int _barsSinceTrade;

	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public AlgModes Mode { get => _mode.Value; set => _mode.Value = value; }

	public RmacdReversalStrategy()
	{
		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal smoothing period", "Indicator");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_mode = Param(nameof(Mode), AlgModes.Breakdown)
			.SetDisplay("Mode", "Entry algorithm", "Trading");
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
		_prevMacd = 0;
		_prevMacd2 = 0;
		_prevSignal = 0;
		_prevSignal2 = 0;
		_initialized = 0;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.SignalMa.Length = SignalLength;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(macd, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed)
			return;

		_barsSinceTrade++;

		var typed = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return;

		if (_initialized == 0)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_initialized = 1;
			return;
		}
		else if (_initialized == 1)
		{
			_prevMacd2 = _prevMacd;
			_prevSignal2 = _prevSignal;
			_prevMacd = macd;
			_prevSignal = signal;
			_initialized = 2;
			return;
		}

		var buy = false;
		var sell = false;

		switch (Mode)
		{
			case AlgModes.Breakdown:
				buy = _prevMacd > 0m && macd <= 0m;
				sell = _prevMacd < 0m && macd >= 0m;
				break;

			case AlgModes.MacdTwist:
				buy = _prevMacd < _prevMacd2 && macd > _prevMacd;
				sell = _prevMacd > _prevMacd2 && macd < _prevMacd;
				break;

			case AlgModes.SignalTwist:
				buy = _prevSignal < _prevSignal2 && signal > _prevSignal;
				sell = _prevSignal > _prevSignal2 && signal < _prevSignal;
				break;

			case AlgModes.MacdDisposition:
				buy = _prevMacd > _prevSignal && macd <= signal;
				sell = _prevMacd < _prevSignal && macd >= signal;
				break;
		}

		if (buy && Position <= 0 && _barsSinceTrade >= CooldownBars)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_barsSinceTrade = 0;
		}
		else if (sell && Position >= 0 && _barsSinceTrade >= CooldownBars)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_barsSinceTrade = 0;
		}

		_prevMacd2 = _prevMacd;
		_prevSignal2 = _prevSignal;
		_prevMacd = macd;
		_prevSignal = signal;
	}

	/// <summary>
	/// Entry modes for RMACD strategy.
	/// </summary>
	public enum AlgModes
	{
		/// <summary>
		/// MACD histogram crossing the zero line.
		/// </summary>
		Breakdown,

		/// <summary>
		/// MACD histogram changes direction.
		/// </summary>
		MacdTwist,

		/// <summary>
		/// Signal line changes direction.
		/// </summary>
		SignalTwist,

		/// <summary>
		/// MACD histogram crosses the signal line.
		/// </summary>
		MacdDisposition
	}
}
