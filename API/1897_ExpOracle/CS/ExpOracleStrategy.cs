using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Oracle indicator combining RSI and CCI.
/// Supports three signal modes: zero line breakdown, direction twist,
/// and crossing between indicator and its signal line.
/// </summary>
public class ExpOracleStrategy : Strategy
{
	/// <summary>
	/// Trading algorithm modes.
	/// </summary>
	public enum AlgorithmModes
	{
		/// <summary>
		/// Signal line crossing zero.
		/// </summary>
		Breakdown,

		/// <summary>
		/// Change of signal line direction.
		/// </summary>
		Twist,

		/// <summary>
		/// Signal line crossing main line.
		/// </summary>
		Disposition
	}

	private readonly StrategyParam<int> _oraclePeriod;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<AlgorithmModes> _mode;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;

	private RelativeStrengthIndex _rsi;
	private CommodityChannelIndex _cci;
	private SimpleMovingAverage _sma;

	private readonly decimal[] _rsiBuf = new decimal[4];
	private readonly decimal[] _cciBuf = new decimal[4];

	private decimal _prevSignal;
	private decimal _prevPrevSignal;
	private decimal _prevOracle;
	private int _barsSinceTrade;

	/// <summary>
	/// Oracle calculation period.
	/// </summary>
	public int OraclePeriod
	{
		get => _oraclePeriod.Value;
		set => _oraclePeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for signal line.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// Selected trading algorithm.
	/// </summary>
	public AlgorithmModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow long positions.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpOracleStrategy"/>.
	/// </summary>
	public ExpOracleStrategy()
	{
		_oraclePeriod = Param(nameof(OraclePeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("Oracle Period", "Oracle period", "Parameters");

		_smooth = Param(nameof(Smooth), 8)
			.SetGreaterThanZero()
			.SetDisplay("Smooth", "Smoothing length", "Parameters");

		_mode = Param(nameof(Mode), AlgorithmModes.Breakdown)
			.SetDisplay("Mode", "Signal algorithm", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "Parameters");

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long entries", "Parameters");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short entries", "Parameters");
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
		_prevSignal = 0;
		_prevPrevSignal = 0;
		_prevOracle = 0;
		_barsSinceTrade = CooldownBars;
		Array.Clear(_rsiBuf, 0, _rsiBuf.Length);
		Array.Clear(_cciBuf, 0, _cciBuf.Length);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = OraclePeriod };
		_cci = new CommodityChannelIndex { Length = OraclePeriod };
		_sma = new SimpleMovingAverage { Length = Smooth };

		StartProtection(null, null);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// RSI uses decimal input, CCI uses candle input
		var rsiResult = _rsi.Process(candle.ClosePrice, candle.OpenTime, true);
		var cciResult = _cci.Process(candle);

		if (!rsiResult.IsFormed || !cciResult.IsFormed)
			return;

		var rsiVal = rsiResult.ToDecimal();
		var cciVal = cciResult.ToDecimal();

		// shift buffers
		_rsiBuf[3] = _rsiBuf[2];
		_rsiBuf[2] = _rsiBuf[1];
		_rsiBuf[1] = _rsiBuf[0];
		_rsiBuf[0] = rsiVal;

		_cciBuf[3] = _cciBuf[2];
		_cciBuf[2] = _cciBuf[1];
		_cciBuf[1] = _cciBuf[0];
		_cciBuf[0] = cciVal;

		// compute Oracle value
		var div0 = _cciBuf[0] - _rsiBuf[0];
		var dDiv = div0;
		var div1 = _cciBuf[1] - _rsiBuf[1] - dDiv;
		dDiv += div1;
		var div2 = _cciBuf[2] - _rsiBuf[2] - dDiv;
		dDiv += div2;
		var div3 = _cciBuf[3] - _rsiBuf[3] - dDiv;

		var max = Math.Max(Math.Max(div0, div1), Math.Max(div2, div3));
		var min = Math.Min(Math.Min(div0, div1), Math.Min(div2, div3));
		var oracle = max + min;

		// smooth to get signal
		var signalResult = _sma.Process(oracle, candle.OpenTime, true);
		if (!signalResult.IsFormed)
			return;

		var signal = signalResult.ToDecimal();
		_barsSinceTrade++;

		switch (Mode)
		{
			case AlgorithmModes.Breakdown:
				if (AllowBuy && _barsSinceTrade >= CooldownBars && _prevSignal <= 0m && signal > 0m && Position <= 0)
				{
					if (Position < 0)
						BuyMarket();

					BuyMarket();
					_barsSinceTrade = 0;
				}
				else if (AllowSell && _barsSinceTrade >= CooldownBars && _prevSignal >= 0m && signal < 0m && Position >= 0)
				{
					if (Position > 0)
						SellMarket();

					SellMarket();
					_barsSinceTrade = 0;
				}
				break;

			case AlgorithmModes.Twist:
				if (AllowBuy && _barsSinceTrade >= CooldownBars && _prevPrevSignal > _prevSignal && signal >= _prevSignal && Position <= 0)
				{
					if (Position < 0)
						BuyMarket();

					BuyMarket();
					_barsSinceTrade = 0;
				}
				else if (AllowSell && _barsSinceTrade >= CooldownBars && _prevPrevSignal < _prevSignal && signal <= _prevSignal && Position >= 0)
				{
					if (Position > 0)
						SellMarket();

					SellMarket();
					_barsSinceTrade = 0;
				}
				break;

			case AlgorithmModes.Disposition:
				if (AllowBuy && _barsSinceTrade >= CooldownBars && _prevSignal < _prevOracle && signal >= oracle && Position <= 0)
				{
					if (Position < 0)
						BuyMarket();

					BuyMarket();
					_barsSinceTrade = 0;
				}
				else if (AllowSell && _barsSinceTrade >= CooldownBars && _prevSignal > _prevOracle && signal <= oracle && Position >= 0)
				{
					if (Position > 0)
						SellMarket();

					SellMarket();
					_barsSinceTrade = 0;
				}
				break;
		}

		_prevPrevSignal = _prevSignal;
		_prevSignal = signal;
		_prevOracle = oracle;
	}
}
