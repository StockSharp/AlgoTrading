namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Bread and Butter trend strategy converted from the MetaTrader 4 expert advisor.
/// Trades when three open-price linear weighted moving averages cross in the same direction.
/// </summary>
public class Breadandbutter2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _interval;
	private readonly StrategyParam<decimal> _crossFilter;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _wma5 = null!;
	private WeightedMovingAverage _wma10 = null!;
	private WeightedMovingAverage _wma15 = null!;

	private decimal? _previousWma5;
	private decimal? _previousWma10;
	private decimal? _previousWma15;

	private int _intervalCounter;

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Number of finished candles between pyramiding entries.
	/// </summary>
	public int Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	/// <summary>
	/// Placeholder parameter kept from the original script for future ADX filtering.
	/// </summary>
	public decimal CrossFilter
	{
		get => _crossFilter.Value;
		set => _crossFilter.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Breadandbutter2Strategy"/> class.
	/// </summary>
	public Breadandbutter2Strategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 20)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Profit target in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_stopLoss = Param(nameof(StopLoss), 20)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Loss limit in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_interval = Param(nameof(Interval), 4)
			.SetNotNegative()
			.SetDisplay("Interval", "Bars between additional entries", "Trading logic")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_crossFilter = Param(nameof(CrossFilter), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Cross Filter", "Reserved threshold for ADX confirmation", "Trading logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Series used for moving averages", "General");
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

		_previousWma5 = null;
		_previousWma10 = null;
		_previousWma15 = null;
		_intervalCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wma5 = new WeightedMovingAverage
		{
			Length = 5,
			CandlePrice = CandlePrice.Open,
		};

		_wma10 = new WeightedMovingAverage
		{
			Length = 10,
			CandlePrice = CandlePrice.Open,
		};

		_wma15 = new WeightedMovingAverage
		{
			Length = 15,
			CandlePrice = CandlePrice.Open,
		};

		SubscribeCandles(CandleType)
			.Bind(_wma5, _wma10, _wma15, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wma5, decimal wma10, decimal wma15)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previous5 = _previousWma5;
		var previous10 = _previousWma10;
		var previous15 = _previousWma15;

		_previousWma5 = wma5;
		_previousWma10 = wma10;
		_previousWma15 = wma15;

		if (previous5 is null || previous10 is null || previous15 is null)
		{
			_intervalCounter = 0;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rising = previous5 < previous10 && previous10 < previous15 && wma5 > wma10 && wma10 > wma15;
		var falling = previous5 > previous10 && previous10 > previous15 && wma5 < wma10 && wma10 < wma15;

		if (rising)
		{
			_intervalCounter = 0;
			AdjustPosition(Volume, candle.ClosePrice);
			return;
		}

		if (falling)
		{
			_intervalCounter = 0;
			AdjustPosition(-Volume, candle.ClosePrice);
			return;
		}

		HandlePyramiding(candle);
	}

	private void HandlePyramiding(ICandleMessage candle)
	{
		if (Interval <= 0 || Volume <= 0)
		{
			_intervalCounter = 0;
			return;
		}

		if (Position == 0)
		{
			_intervalCounter = 0;
			return;
		}

		_intervalCounter++;

		if (_intervalCounter < Interval)
			return;

		_intervalCounter = 0;

		var referencePrice = candle.ClosePrice;

		if (Position > 0)
		{
			var target = Position + Volume;
			BuyMarket(Volume);
			ApplyRiskManagement(referencePrice, target);
		}
		else if (Position < 0)
		{
			var target = Position - Volume;
			SellMarket(Volume);
			ApplyRiskManagement(referencePrice, target);
		}
	}

	private void AdjustPosition(decimal targetPosition, decimal referencePrice)
	{
		if (Volume <= 0)
			return;

		var diff = targetPosition - Position;

		if (diff > 0m)
		{
			BuyMarket(diff);
		}
		else if (diff < 0m)
		{
			SellMarket(-diff);
		}

		ApplyRiskManagement(referencePrice, targetPosition);
	}

	private void ApplyRiskManagement(decimal referencePrice, decimal resultingPosition)
	{
		if (TakeProfit > 0)
			SetTakeProfit(TakeProfit, referencePrice, resultingPosition);

		if (StopLoss > 0)
			SetStopLoss(StopLoss, referencePrice, resultingPosition);
	}
}

