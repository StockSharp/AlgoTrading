using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// John Ehlers' Price Radio strategy.
/// Uses derivative-based amplitude and frequency thresholds to trade.
/// </summary>
public class ThePriceRadioStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _holdBars;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _envelope = null!;
	private SimpleMovingAverage _amSma = null!;
	private Highest _derivHigh = null!;
	private Lowest _derivLow = null!;
	private SimpleMovingAverage _fmSma = null!;
	private decimal _prevClose;
	private int _entriesExecuted;
	private int _barsInPosition;
	private int _barsSinceSignal;

	/// <summary>
	/// Lookback period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Maximum number of entries per run.
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	/// <summary>
	/// Forced position holding period in finished bars.
	/// </summary>
	public int HoldBars
	{
		get => _holdBars.Value;
		set => _holdBars.Value = value;
	}

	/// <summary>
	/// Minimum finished bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ThePriceRadioStrategy"/> class.
	/// </summary>
	public ThePriceRadioStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback period", "General")
			;

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Maximum entries per run", "Risk");

		_holdBars = Param(nameof(HoldBars), 180)
			.SetGreaterThanZero()
			.SetDisplay("Hold Bars", "Bars to hold position before forced exit", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 240)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_envelope = null!;
		_amSma = null!;
		_derivHigh = null!;
		_derivLow = null!;
		_fmSma = null!;
		_prevClose = 0;
		_entriesExecuted = 0;
		_barsInPosition = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_envelope = new Highest { Length = 4 };
		_amSma = new SMA { Length = Length };
		_derivHigh = new Highest { Length = Length };
		_derivLow = new Lowest { Length = Length };
		_fmSma = new SMA { Length = Length };
		_entriesExecuted = 0;
		_barsInPosition = 0;
		_barsSinceSignal = CooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _amSma);
			DrawIndicator(area, _fmSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var deriv = candle.ClosePrice - _prevClose;
		_prevClose = candle.ClosePrice;

		var envelope = _envelope.Process(new DecimalIndicatorValue(_envelope, Math.Abs(deriv), candle.OpenTime)).ToDecimal();
		var am = _amSma.Process(new DecimalIndicatorValue(_amSma, envelope, candle.OpenTime)).ToDecimal();

		var high = _derivHigh.Process(new DecimalIndicatorValue(_derivHigh, deriv, candle.OpenTime)).ToDecimal();
		var low = _derivLow.Process(new DecimalIndicatorValue(_derivLow, deriv, candle.OpenTime)).ToDecimal();

		var clamped = Math.Min(Math.Max(10m * deriv, low), high);
		var fm = _fmSma.Process(new DecimalIndicatorValue(_fmSma, clamped, candle.OpenTime)).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldBars)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_barsInPosition = 0;
				_barsSinceSignal = 0;
			}

			return;
		}

		_barsInPosition = 0;
		_barsSinceSignal++;

		if (_entriesExecuted >= MaxEntries || _barsSinceSignal < CooldownBars)
			return;

		if (deriv > am && deriv > fm)
		{
			BuyMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
		else if (deriv < -am && deriv < -fm)
		{
			SellMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
	}
}
