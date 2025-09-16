using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RoNz Rapid-Fire strategy based on MA and Parabolic SAR trend signals.
/// </summary>
public class RoNzRapidFireStrategy : Strategy
{
	public enum CloseTypes
	{
		SlClose,
		TrendClose
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<bool> _averaging;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _psarStep;
	private readonly StrategyParam<decimal> _psarMax;
	private readonly StrategyParam<CloseTypes> _closeType;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal? _prevSar;
	private decimal _tick;

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop in ticks.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Enable averaging.
	/// </summary>
	public bool Averaging
	{
		get => _averaging.Value;
		set => _averaging.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Parabolic SAR step.
	/// </summary>
	public decimal PsarStep
	{
		get => _psarStep.Value;
		set => _psarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum.
	/// </summary>
	public decimal PsarMax
	{
		get => _psarMax.Value;
		set => _psarMax.Value = value;
	}

	/// <summary>
	/// Close mode.
	/// </summary>
	public CloseTypes CloseType
	{
		get => _closeType.Value;
		set => _closeType.Value = value;
	}

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RoNzRapidFireStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume", "General");

		_stopLoss = Param(nameof(StopLoss), 150)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 100)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 0)
			.SetDisplay("Trailing Stop", "Trailing stop in ticks", "Risk");

		_averaging = Param(nameof(Averaging), false)
			.SetDisplay("Averaging", "Add to position on continuing trend", "General");

		_maPeriod = Param(nameof(MaPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicator");

		_psarStep = Param(nameof(PsarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("PSAR Step", "Parabolic SAR step", "Indicator");

		_psarMax = Param(nameof(PsarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("PSAR Max", "Parabolic SAR maximum", "Indicator");

		_closeType = Param(nameof(CloseType), CloseTypes.SlClose)
			.SetDisplay("Close Type", "Use stops or trend reversals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
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

		_tick = Security?.PriceStep ?? 1m;

		var sma = new Sma { Length = MaPeriod };
		var psar = new ParabolicSar
		{
			Acceleration = PsarStep,
			AccelerationStep = PsarStep,
			AccelerationMax = PsarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, psar, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal psarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevSar = _prevSar;
		bool upSignal = prevSar.HasValue && candle.ClosePrice > smaValue && psarValue < candle.ClosePrice && prevSar.Value > candle.ClosePrice;
		bool downSignal = prevSar.HasValue && candle.ClosePrice < smaValue && psarValue > candle.ClosePrice && prevSar.Value < candle.ClosePrice;

		if (Position > 0)
		{
			if (CloseType == CloseTypes.TrendClose && downSignal)
				SellMarket(Position);

			if (TakeProfit > 0 && candle.HighPrice >= _takePrice)
				SellMarket(Position);

			if (StopLoss > 0 && candle.LowPrice <= _stopPrice)
				SellMarket(Position);

			if (TrailingStop > 0)
			{
				var trail = candle.ClosePrice - TrailingStop * _tick;
				if (trail > _stopPrice)
					_stopPrice = trail;
			}

			if (Averaging && upSignal)
				EnterLong(candle);
		}
		else if (Position < 0)
		{
			if (CloseType == CloseTypes.TrendClose && upSignal)
				BuyMarket(Math.Abs(Position));

			if (TakeProfit > 0 && candle.LowPrice <= _takePrice)
				BuyMarket(Math.Abs(Position));

			if (StopLoss > 0 && candle.HighPrice >= _stopPrice)
				BuyMarket(Math.Abs(Position));

			if (TrailingStop > 0)
			{
				var trail = candle.ClosePrice + TrailingStop * _tick;
				if (trail < _stopPrice)
					_stopPrice = trail;
			}

			if (Averaging && downSignal)
				EnterShort(candle);
		}
		else
		{
			if (upSignal)
				EnterLong(candle);
			else if (downSignal)
				EnterShort(candle);
		}

		_prevSar = psarValue;
	}

	private void EnterLong(ICandleMessage candle)
	{
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLoss > 0 ? _entryPrice - StopLoss * _tick : 0m;
		_takePrice = TakeProfit > 0 ? _entryPrice + TakeProfit * _tick : 0m;
	}

	private void EnterShort(ICandleMessage candle)
	{
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLoss > 0 ? _entryPrice + StopLoss * _tick : 0m;
		_takePrice = TakeProfit > 0 ? _entryPrice - TakeProfit * _tick : 0m;
	}
}
