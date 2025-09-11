using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Golden Transform Strategy.
/// Combines Rate of Change, triple Hull-based TRIX and Fisher Transform.
/// </summary>
public class GoldenTransformStrategy : Strategy
{
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _trixLength;
	private readonly StrategyParam<int> _hmaEntryLength;
	private readonly StrategyParam<int> _fisherLength;
	private readonly StrategyParam<int> _fisherSmoothLength;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _roc;
	private HullMovingAverage _hmaEntry;
	private FisherTransform _fisher;
	private HullMovingAverage _fisherSmooth;
	private HullMovingAverage _hull1;
	private HullMovingAverage _hull2;
	private HullMovingAverage _hull3;

	private decimal _prevRoc;
	private decimal _prevTrix;
	private decimal _prevTripleHull;
	private decimal _prevFish1;
	private decimal _prevFish0;
	private decimal _prevPrevFish0;
	private int _valueCount;

	/// <summary>
	/// Rate of Change period.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// Hull TRIX length.
	/// </summary>
	public int TrixLength
	{
		get => _trixLength.Value;
		set => _trixLength.Value = value;
	}

	/// <summary>
	/// Hull MA entry length.
	/// </summary>
	public int HullEntryLength
	{
		get => _hmaEntryLength.Value;
		set => _hmaEntryLength.Value = value;
	}

	/// <summary>
	/// Fisher Transform length.
	/// </summary>
	public int FisherLength
	{
		get => _fisherLength.Value;
		set => _fisherLength.Value = value;
	}

	/// <summary>
	/// Fisher smoothing length.
	/// </summary>
	public int FisherSmoothLength
	{
		get => _fisherSmoothLength.Value;
		set => _fisherSmoothLength.Value = value;
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
	/// Initializes a new instance of <see cref="GoldenTransformStrategy"/>.
	/// </summary>
	public GoldenTransformStrategy()
	{
		_rocLength = Param(nameof(RocLength), 50)
			.SetDisplay("ROC Length", "Period for Rate of Change", "General")
			.SetCanOptimize(true);

		_trixLength = Param(nameof(TrixLength), 90)
			.SetDisplay("Hull TRIX Length", "Length for triple Hull moving average", "Indicators")
			.SetCanOptimize(true);

		_hmaEntryLength = Param(nameof(HullEntryLength), 65)
			.SetDisplay("Hull Entry Length", "Length for entry Hull MA filter", "Indicators")
			.SetCanOptimize(true);

		_fisherLength = Param(nameof(FisherLength), 50)
			.SetDisplay("Fisher Length", "Period for Fisher Transform", "Indicators")
			.SetCanOptimize(true);

		_fisherSmoothLength = Param(nameof(FisherSmoothLength), 5)
			.SetDisplay("Fisher Smooth Length", "Smoothing length for Fisher", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRoc = 0m;
		_prevTrix = 0m;
		_prevTripleHull = 0m;
		_prevFish1 = 0m;
		_prevFish0 = 0m;
		_prevPrevFish0 = 0m;
		_valueCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_roc = new RateOfChange { Length = RocLength };
		_hmaEntry = new HullMovingAverage { Length = HullEntryLength };
		_fisher = new FisherTransform { Length = FisherLength };
		_fisherSmooth = new HullMovingAverage { Length = FisherSmoothLength };
		_hull1 = new HullMovingAverage { Length = TrixLength };
		_hull2 = new HullMovingAverage { Length = TrixLength };
		_hull3 = new HullMovingAverage { Length = TrixLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rocVal = _roc.Process(candle.ClosePrice);
		var hmaEntryVal = _hmaEntry.Process(candle.ClosePrice);
		var logClose = (decimal)Math.Log((double)candle.ClosePrice);
		var hull1Val = _hull1.Process(logClose);
		var hull2Val = _hull2.Process(hull1Val.ToDecimal());
		var hull3Val = _hull3.Process(hull2Val.ToDecimal());
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var fish0Val = _fisher.Process(hl2);
		var fish1Val = _fisherSmooth.Process(fish0Val.ToDecimal());

		if (!rocVal.IsFinal || !_hull3.IsFormed || !fish0Val.IsFinal || !_fisherSmooth.IsFormed || !hmaEntryVal.IsFinal)
			return;

		var roc = rocVal.ToDecimal();
		var hullEntry = hmaEntryVal.ToDecimal();
		var fish0 = fish0Val.ToDecimal();
		var fish1 = fish1Val.ToDecimal();
		var hull3 = hull3Val.ToDecimal();

		var trix = (_prevTripleHull == 0m ? 0m : (hull3 - _prevTripleHull) * 10000m);
		_prevTripleHull = hull3;

		_valueCount++;
		if (_valueCount < 3)
		{
			_prevPrevFish0 = _prevFish0;
			_prevFish0 = fish0;
			_prevFish1 = fish1;
			_prevRoc = roc;
			_prevTrix = trix;
			return;
		}

		var fish2 = _prevFish0;
		var crossOverRocTrix = _prevRoc <= _prevTrix && roc > trix;
		var crossUnderRocTrix = _prevRoc >= _prevTrix && roc < trix;
		var crossUnderFish = _prevFish1 >= _prevPrevFish0 && fish1 < fish2;
		var crossOverFish = _prevFish1 <= _prevPrevFish0 && fish1 > fish2;

		if (crossOverRocTrix && trix < 0m && candle.OpenPrice > hullEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossUnderRocTrix && trix > 0m && candle.OpenPrice < hullEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && (crossUnderRocTrix || (fish1 > 1.5m && crossUnderFish)))
			SellMarket(Position);
		else if (Position < 0 && (crossOverRocTrix || (fish1 < -1.5m && crossOverFish)))
			BuyMarket(-Position);

		_prevPrevFish0 = _prevFish0;
		_prevFish0 = fish0;
		_prevFish1 = fish1;
		_prevRoc = roc;
		_prevTrix = trix;
	}
}
