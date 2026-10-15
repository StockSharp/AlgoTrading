using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Box breakout strategy with RSI and MA200 filters.
/// </summary>
public class KaitoBoxWithRsiDivStrategy : Strategy
{
	private readonly StrategyParam<int> _boxLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _holdBars;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _entriesExecuted;
	private int _barsInPosition;
	private int _barsSinceSignal;

	/// <summary>
	/// Length of the box range.
	/// </summary>
	public int BoxLength
	{
		get => _boxLength.Value;
		set => _boxLength.Value = value;
	}

	/// <summary>
	/// Length of RSI indicator.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Period for long trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Maximum holding period in finished candles.
	/// </summary>
	public int HoldBars
	{
		get => _holdBars.Value;
		set => _holdBars.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KaitoBoxWithRsiDivStrategy()
	{
		_boxLength = Param(nameof(BoxLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Box Length", "Length of the box range", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length of RSI", "General");

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Trend moving average period", "Trend");

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Maximum entries per run", "Risk");

		_holdBars = Param(nameof(HoldBars), 240)
			.SetGreaterThanZero()
			.SetDisplay("Hold Bars", "Bars to hold position before forced exit", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 240)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(3).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_entriesExecuted = 0;
		_barsInPosition = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = BoxLength };
		var lowest = new Lowest { Length = BoxLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ma = new SimpleMovingAverage { Length = MaPeriod };

		_entriesExecuted = 0;
		_barsInPosition = 0;
		_barsSinceSignal = CooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, rsi, ma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestHigh, decimal lowestLow, decimal rsiValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

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

		var longSignal = candle.HighPrice >= highestHigh && rsiValue > 55m && candle.ClosePrice > maValue;
		var shortSignal = candle.LowPrice <= lowestLow && rsiValue < 45m && candle.ClosePrice < maValue;

		if (longSignal)
		{
			BuyMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
		else if (shortSignal)
		{
			SellMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}
	}
}
