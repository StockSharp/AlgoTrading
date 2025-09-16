using System;
using System.Collections.Generic;
using StockSharp.BusinessEntities;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R and RSI combination.
/// </summary>
public class WprsiSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _filterUp;
	private readonly StrategyParam<int> _filterDown;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsPercentRange _wpr;
	private RelativeStrengthIndex _rsi;

	private decimal _prevWpr;
	private bool _isPrevInit;
	private bool _pendingBuy;
	private bool _pendingSell;
	private int _upCounter;
	private int _downCounter;

	/// <summary>
	/// Calculation length for WPR and RSI.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Bars count to confirm buy signal.
	/// </summary>
	public int FilterUp
	{
		get => _filterUp.Value;
		set => _filterUp.Value = value;
	}

	/// <summary>
	/// Bars count to confirm sell signal.
	/// </summary>
	public int FilterDown
	{
		get => _filterDown.Value;
		set => _filterDown.Value = value;
	}

	/// <summary>
	/// Candle type for indicators calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="WprsiSignalStrategy"/>.
	/// </summary>
	public WprsiSignalStrategy()
	{
		_period = Param(nameof(Period), 27)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Period for WPR and RSI", "Parameters");

		_filterUp = Param(nameof(FilterUp), 10)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Filter Up", "Bars to confirm buy", "Parameters");

		_filterDown = Param(nameof(FilterDown), 10)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Filter Down", "Bars to confirm sell", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "Parameters");
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
		_prevWpr = 0m;
		_isPrevInit = false;
		_pendingBuy = false;
		_pendingSell = false;
		_upCounter = 0;
		_downCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_wpr = new WilliamsPercentRange { Length = Period };
		_rsi = new RelativeStrengthIndex { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_wpr, _rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wpr);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isPrevInit)
		{
			_prevWpr = wprValue;
			_isPrevInit = true;
			return;
		}

		if (_pendingBuy)
		{
			if (wprValue <= -20)
				_pendingBuy = false;
			else if (--_upCounter <= 0)
			{
				if (rsiValue > 50 && Position <= 0)
					BuyMarket();
				_pendingBuy = false;
			}
		}
		else if (_prevWpr < -20 && wprValue > -20 && rsiValue > 50)
		{
			_pendingBuy = true;
			_upCounter = FilterUp;
		}

		if (_pendingSell)
		{
			if (wprValue >= -80)
				_pendingSell = false;
			else if (--_downCounter <= 0)
			{
				if (rsiValue < 50 && Position >= 0)
					SellMarket();
				_pendingSell = false;
			}
		}
		else if (_prevWpr > -80 && wprValue < -80 && rsiValue < 50)
		{
			_pendingSell = true;
			_downCounter = FilterDown;
		}

		_prevWpr = wprValue;
	}
}