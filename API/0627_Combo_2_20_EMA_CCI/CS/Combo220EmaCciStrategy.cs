using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on combination of 2/20 EMA and CCI signals.
/// </summary>
public class Combo220EmaCciStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private CommodityChannelIndex _cci;
	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private int _prevEmaPos;
	private int _prevCciPos;

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	/// <summary>
	/// Fast MA length for CCI.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow MA length for CCI.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Start trading time.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Combo220EmaCciStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 14)
		.SetRange(5, 50)
		.SetDisplay("EMA Length", "EMA indicator length", "Indicators")
		.SetCanOptimize(true);

		_cciLength = Param(nameof(CciLength), 10)
		.SetRange(5, 50)
		.SetDisplay("CCI Length", "CCI indicator length", "Indicators")
		.SetCanOptimize(true);

		_fastMaLength = Param(nameof(FastMaLength), 10)
		.SetRange(5, 50)
		.SetDisplay("Fast MA Length", "Fast MA length for CCI", "Indicators")
		.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 20)
		.SetRange(5, 50)
		.SetDisplay("Slow MA Length", "Slow MA length for CCI", "Indicators")
		.SetCanOptimize(true);

		_reverse = Param(nameof(Reverse), false)
		.SetDisplay("Reverse", "Trade reverse signals", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Start Time", "Begin trading after this time", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_cci = new CommodityChannelIndex { Length = CciLength };
		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };

		_prevHigh = 0m;
		_prevLow = 0m;
		_prevClose = 0m;
		_prevEmaPos = 0;
		_prevCciPos = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, _cci, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.OpenTime;
		if (time < StartTime)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		var fastVal = _fastMa.Process(new DecimalIndicatorValue(_fastMa, cciValue, candle.OpenTime)).ToDecimal();
		var slowVal = _slowMa.Process(new DecimalIndicatorValue(_slowMa, cciValue, candle.OpenTime)).ToDecimal();

		var nHH = Math.Max(candle.HighPrice, _prevHigh);
		var nLL = Math.Min(candle.LowPrice, _prevLow);
		var nXS = nLL > emaValue || nHH < emaValue ? nLL : nHH;
		var emaPos = nXS < _prevClose ? 1 : nXS > _prevClose ? -1 : _prevEmaPos;
		_prevEmaPos = emaPos;

		var cciPos = slowVal < fastVal ? 1 : slowVal > fastVal ? -1 : _prevCciPos;
		_prevCciPos = cciPos;

		var pos = 0;
		if (emaPos == 1 && cciPos == 1)
		pos = 1;
		else if (emaPos == -1 && cciPos == -1)
		pos = -1;

		if (Reverse)
		pos = -pos;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		if (pos == 1 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (pos == -1 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (pos == 0 && Position != 0)
		{
			ClosePosition();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}
}
