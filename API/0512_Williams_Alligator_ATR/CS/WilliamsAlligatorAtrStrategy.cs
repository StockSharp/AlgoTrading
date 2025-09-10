using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams Alligator strategy with ATR-based stop-loss.
/// Opens a long position when Lips crosses above Jaw.
/// Closes the position when Lips crosses below Jaw or ATR stop triggers.
/// </summary>
public class WilliamsAlligatorAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;
	private AverageTrueRange _atr;

	private bool _isInitialized;
	private bool _prevLipsAboveJaw;
	private decimal _entryPrice;

	/// <summary>
	/// Jaw SMMA period.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Teeth SMMA period.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Lips SMMA period.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initialize <see cref="WilliamsAlligatorAtrStrategy"/>.
	/// </summary>
	public WilliamsAlligatorAtrStrategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Alligator jaw period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Alligator teeth period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Alligator lips period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stop-loss", "ATR")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss", "ATR")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_isInitialized = false;
		_prevLipsAboveJaw = false;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawLength };
		_teeth = new SmoothedMovingAverage { Length = TeethLength };
		_lips = new SmoothedMovingAverage { Length = LipsLength };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawVal = _jaw.Process(new DecimalIndicatorValue(_jaw, median, candle.ServerTime));
		var teethVal = _teeth.Process(new DecimalIndicatorValue(_teeth, median, candle.ServerTime));
		var lipsVal = _lips.Process(new DecimalIndicatorValue(_lips, median, candle.ServerTime));

		if (!jawVal.IsFormed || !teethVal.IsFormed || !lipsVal.IsFormed || !_atr.IsFormed)
			return;

		var jaw = jawVal.ToDecimal();
		var lips = lipsVal.ToDecimal();
		var atr = atrValue.ToDecimal();
		var price = candle.ClosePrice;

		var lipsAboveJaw = lips > jaw;

		if (!_isInitialized)
		{
			_prevLipsAboveJaw = lipsAboveJaw;
			_isInitialized = true;
			return;
		}

		if (!_prevLipsAboveJaw && lipsAboveJaw && Position <= 0)
		{
			RegisterBuy();
			_entryPrice = price;
		}
		else if (_prevLipsAboveJaw && !lipsAboveJaw && Position > 0)
		{
			RegisterSell(Position);
			_entryPrice = 0m;
		}
		else if (Position > 0)
		{
			var stopPrice = _entryPrice - AtrMultiplier * atr;
			if (price <= stopPrice)
			{
				RegisterSell(Position);
				_entryPrice = 0m;
			}
		}

		_prevLipsAboveJaw = lipsAboveJaw;
	}
}
