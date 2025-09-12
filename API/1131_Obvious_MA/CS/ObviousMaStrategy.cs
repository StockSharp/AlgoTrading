using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV-based moving average strategy.
/// </summary>
public class ObviousMaStrategy : Strategy
{
	private readonly StrategyParam<int> _longEntryLength;
	private readonly StrategyParam<int> _longExitLength;
	private readonly StrategyParam<int> _shortEntryLength;
	private readonly StrategyParam<int> _shortExitLength;
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;

	private OnBalanceVolume _obv = null!;
	private SimpleMovingAverage _obvMaLongEntry = null!;
	private SimpleMovingAverage _obvMaLongExit = null!;
	private SimpleMovingAverage _obvMaShortEntry = null!;
	private SimpleMovingAverage _obvMaShortExit = null!;

	private decimal _prevObv;
	private decimal _prevLongEntryMa;
	private decimal _prevLongExitMa;
	private decimal _prevShortEntryMa;
	private decimal _prevShortExitMa;
	private bool _isFirst;

	/// <summary>
	/// Initializes a new instance of <see cref="ObviousMaStrategy"/>.
	/// </summary>
	public ObviousMaStrategy()
	{
		_longEntryLength = Param(nameof(LongEntryLength), 190)
			.SetGreaterThanZero()
			.SetDisplay("Long Entry MA Length", "OBV MA length for long entries", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_longExitLength = Param(nameof(LongExitLength), 202)
			.SetGreaterThanZero()
			.SetDisplay("Long Exit MA Length", "OBV MA length for long exits", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_shortEntryLength = Param(nameof(ShortEntryLength), 395)
			.SetGreaterThanZero()
			.SetDisplay("Short Entry MA Length", "OBV MA length for short entries", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 10);

		_shortExitLength = Param(nameof(ShortExitLength), 300)
			.SetGreaterThanZero()
			.SetDisplay("Short Exit MA Length", "OBV MA length for short exits", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 10);

		_tradeDirection = Param(nameof(TradeDirection), "Long")
			.SetDisplay("Direction", "Trading direction: Long or Short", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Parameters");
	}

	/// <summary>
	/// OBV MA length for long entries.
	/// </summary>
	public int LongEntryLength
	{
		get => _longEntryLength.Value;
		set => _longEntryLength.Value = value;
	}

	/// <summary>
	/// OBV MA length for long exits.
	/// </summary>
	public int LongExitLength
	{
		get => _longExitLength.Value;
		set => _longExitLength.Value = value;
	}

	/// <summary>
	/// OBV MA length for short entries.
	/// </summary>
	public int ShortEntryLength
	{
		get => _shortEntryLength.Value;
		set => _shortEntryLength.Value = value;
	}

	/// <summary>
	/// OBV MA length for short exits.
	/// </summary>
	public int ShortExitLength
	{
		get => _shortExitLength.Value;
		set => _shortExitLength.Value = value;
	}

	/// <summary>
	/// Trading direction.
	/// </summary>
	public string TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_prevObv = 0m;
		_prevLongEntryMa = 0m;
		_prevLongExitMa = 0m;
		_prevShortEntryMa = 0m;
		_prevShortExitMa = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_obv = new OnBalanceVolume();
		_obvMaLongEntry = new SimpleMovingAverage { Length = LongEntryLength };
		_obvMaLongExit = new SimpleMovingAverage { Length = LongExitLength };
		_obvMaShortEntry = new SimpleMovingAverage { Length = ShortEntryLength };
		_obvMaShortExit = new SimpleMovingAverage { Length = ShortExitLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_obv, ProcessObv)
			.Start();

		StartProtection(stopLoss: new Unit(2, UnitTypes.Percent), takeProfit: new Unit(3, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _obv);
			DrawIndicator(area, _obvMaLongEntry);
			DrawIndicator(area, _obvMaLongExit);
			DrawIndicator(area, _obvMaShortEntry);
			DrawIndicator(area, _obvMaShortExit);
			DrawOwnTrades(area);
		}
	}

	private void ProcessObv(ICandleMessage candle, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var obvVal = obvValue.ToDecimal();
		var longEntry = _obvMaLongEntry.Process(obvValue).ToDecimal();
		var longExit = _obvMaLongExit.Process(obvValue).ToDecimal();
		var shortEntry = _obvMaShortEntry.Process(obvValue).ToDecimal();
		var shortExit = _obvMaShortExit.Process(obvValue).ToDecimal();

		ProcessCandle(obvVal, longEntry, longExit, shortEntry, shortExit);
	}

	private void ProcessCandle(decimal obvValue, decimal longEntry, decimal longExit, decimal shortEntry, decimal shortExit)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dir = TradeDirection;

		if (!_isFirst)
		{
			var longCond = _prevObv <= _prevLongEntryMa && obvValue > longEntry && dir != "Short" && Position <= 0;
			var longExitCond = _prevObv >= _prevLongExitMa && obvValue < longExit && Position > 0;
			var shortCond = _prevObv >= _prevShortEntryMa && obvValue < shortEntry && dir != "Long" && Position >= 0;
			var shortExitCond = _prevObv <= _prevShortExitMa && obvValue > shortExit && Position < 0;

			if (longCond)
				BuyMarket(Volume + Math.Abs(Position));
			if (longExitCond)
				SellMarket(Position);
			if (shortCond)
				SellMarket(Volume + Math.Abs(Position));
			if (shortExitCond)
				BuyMarket(Math.Abs(Position));
		}

		_prevObv = obvValue;
		_prevLongEntryMa = longEntry;
		_prevLongExitMa = longExit;
		_prevShortEntryMa = shortEntry;
		_prevShortExitMa = shortExit;
		_isFirst = false;
	}
}

