using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// KPrmSt cross strategy based on the stochastic oscillator.
/// Opens long when %K crosses below %D.
/// Opens short when %K crosses above %D.
/// </summary>
public class KPrmStCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal? _prevK;
	private decimal? _prevD;
	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// The type of candles to use for indicator calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the %K line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Period for the %D line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Slowing factor applied to %K.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Stop-loss in price units. 0 disables the stop.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit in price units. 0 disables the target.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public KPrmStCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator calculation", "General");

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Main line period", "KPrmSt");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Signal line period", "KPrmSt");

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Smoothing factor for %K", "KPrmSt");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk Management");
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
		_prevK = null;
		_prevD = null;
		_entryPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var stochastic = new Stochastic
		{
			Length = KPeriod,
			K = Slowing,
			D = DPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(stochastic, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal k, decimal d)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevK is null || _prevD is null)
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		// Crossover from above to below triggers a long entry
		if (_prevK > _prevD && k <= d)
		{
			if (Position <= 0)
			{
				_entryPrice = candle.ClosePrice;
				_isLong = true;
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		// Crossover from below to above triggers a short entry
		else if (_prevK < _prevD && k >= d)
		{
			if (Position >= 0)
			{
				_entryPrice = candle.ClosePrice;
				_isLong = false;
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		if (Position != 0 && _entryPrice != 0m)
		{
			var diff = candle.ClosePrice - _entryPrice;

			if (_isLong)
			{
				if (StopLoss > 0m && diff <= -StopLoss)
					SellMarket(Math.Abs(Position));
				if (TakeProfit > 0m && diff >= TakeProfit)
					SellMarket(Math.Abs(Position));
			}
			else
			{
				if (StopLoss > 0m && diff >= StopLoss)
					BuyMarket(Math.Abs(Position));
				if (TakeProfit > 0m && diff <= -TakeProfit)
					BuyMarket(Math.Abs(Position));
			}
		}

		_prevK = k;
		_prevD = d;
	}
}
