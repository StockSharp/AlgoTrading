using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LivermoreSeykotaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private int _barsSinceSignal;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal TrailAtrMultiplier { get => _trailAtrMultiplier.Value; set => _trailAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public LivermoreSeykotaBreakoutStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "EMA trend period", "Indicators");
		_pivotLength = Param(nameof(PivotLength), 30)
			.SetDisplay("Pivot Length", "Bars for pivot high/low", "General");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Indicators");
		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 10m)
			.SetDisplay("Trail ATR Mult", "ATR trailing mult", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_atr = null;
		_highest = null;
		_lowest = null;
		_prevHighest = 0;
		_prevLowest = 0;
		_barsSinceSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_highest = new Highest { Length = PivotLength };
		_lowest = new Lowest { Length = PivotLength };
		_prevHighest = 0;
		_prevLowest = 0;
		_barsSinceSignal = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _atr, _highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal, decimal highVal, decimal lowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_ema.IsFormed || !_atr.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevHighest = highVal;
			_prevLowest = lowVal;
			return;
		}

		if (atrVal <= 0 || _barsSinceSignal < CooldownBars)
		{
			_prevHighest = highVal;
			_prevLowest = lowVal;
			return;
		}

		// Breakout above previous highest with EMA confirmation — go long
		if (candle.ClosePrice > _prevHighest && candle.ClosePrice > emaVal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		// Breakout below previous lowest with EMA confirmation — go short
		else if (candle.ClosePrice < _prevLowest && candle.ClosePrice < emaVal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		_prevHighest = highVal;
		_prevLowest = lowVal;
	}
}
