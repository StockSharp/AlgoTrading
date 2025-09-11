namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Golden Cross VWMA & EMA Strategy.
/// Opens long when VWMA crosses above EMA from 4h timeframe and short on opposite.
/// </summary>
public class GoldenCrossVwmaEmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _vwmaLength;
	private readonly StrategyParam<int> _emaLength;

	private VolumeWeightedMovingAverage _vwma;
	private ExponentialMovingAverage _ema;

	private decimal? _prevVwma;
	private decimal? _prevEma;
	private decimal _currentEma;
	private bool _emaIsFormed;

	private static readonly DataType HigherCandleType = TimeSpan.FromHours(4).TimeFrame();

	public GoldenCrossVwmaEmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for VWMA", "General");

		_vwmaLength = Param(nameof(VwmaLength), 50)
			.SetDisplay("VWMA Length", "Period for VWMA", "Indicators");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetDisplay("EMA Length", "Period for 4h EMA", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int VwmaLength
	{
		get => _vwmaLength.Value;
		set => _vwmaLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, HigherCandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_prevVwma = null;
		_prevEma = null;
		_currentEma = 0m;
		_emaIsFormed = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vwma = new VolumeWeightedMovingAverage { Length = VwmaLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_vwma, ProcessBase).Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(_ema, ProcessHigher).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwma);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigher(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_currentEma = emaValue;
		_emaIsFormed = _ema.IsFormed;
	}

	private void ProcessBase(ICandleMessage candle, decimal vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_vwma.IsFormed || !_emaIsFormed)
		return;

		if (_prevVwma is null || _prevEma is null)
		{
			_prevVwma = vwmaValue;
			_prevEma = _currentEma;
			return;
		}

		var crossover = _prevVwma <= _prevEma && vwmaValue > _currentEma;
		var crossunder = _prevVwma >= _prevEma && vwmaValue < _currentEma;

		if (crossover && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (crossunder && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));

		_prevVwma = vwmaValue;
		_prevEma = _currentEma;
	}
}
