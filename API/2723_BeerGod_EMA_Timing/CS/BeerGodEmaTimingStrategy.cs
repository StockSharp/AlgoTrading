using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion strategy that triggers trades a few minutes after the bar opens using an EMA trend filter.
/// </summary>
public class BeerGodEmaTimingStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _triggerMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _ema = null!;
	private DateTimeOffset _currentCandleOpenTime = DateTimeOffset.MinValue;
	private decimal _currentEma;
	private decimal _previousEma;
	private decimal _currentClose;
	private decimal _previousClose;


	/// <summary>
	/// EMA length used as the directional filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Minutes from the candle open when the entry check is performed.
	/// </summary>
	public int TriggerMinutesFromOpen
	{
		get => _triggerMinutes.Value;
		set => _triggerMinutes.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BeerGodEmaTimingStrategy"/>.
	/// </summary>
	public BeerGodEmaTimingStrategy()
	{

		_emaLength = Param(nameof(EmaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA length for the trend filter", "Indicator")
			
			.SetOptimize(20, 120, 10);

		_triggerMinutes = Param(nameof(TriggerMinutesFromOpen), 3)
			.SetNotNegative()
			.SetDisplay("Trigger Minutes", "Minutes after open to check signals", "Timing")
			
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");
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

		_currentCandleOpenTime = DateTimeOffset.MinValue;
		_currentEma = 0m;
		_previousEma = 0m;
		_currentClose = 0m;
		_previousClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new EMA
		{
			Length = EmaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousEma = _currentEma;
		_previousClose = _currentClose;

		_currentEma = emaValue;
		_currentClose = candle.ClosePrice;

		if (!_ema.IsFormed || _previousEma == 0m)
			return;

		var price = candle.ClosePrice;
		var maCurrent = _currentEma;
		var maPrevious = _previousEma;
		var prevClose = _previousClose;

		var newBuy = price < maCurrent && maCurrent < maPrevious && price < prevClose;
		var newSell = price > maCurrent && maCurrent > maPrevious && price > prevClose;

		if (!newBuy && !newSell)
			return;

		if (newBuy && Position <= 0)
		{
			var volume = Volume;

			if (Position < 0)
				volume += Math.Abs(Position);

			BuyMarket(volume);
		}
		else if (newSell && Position >= 0)
		{
			var volume = Volume;

			if (Position > 0)
				volume += Position;

			SellMarket(volume);
		}
	}
}