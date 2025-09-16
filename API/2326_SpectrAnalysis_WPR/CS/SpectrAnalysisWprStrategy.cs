using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R trend direction.
/// </summary>
public class SpectrAnalysisWprStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private decimal? _prev;
	private decimal? _prev2;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SpectrAnalysisWprStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
		_wprPeriod = Param(nameof(WprPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "Indicator");
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Allow Long Entry", "Enable long position opening", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Allow Short Entry", "Enable short position opening", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Allow Long Exit", "Enable closing of long positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Allow Short Exit", "Enable closing of short positions", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var wpr = new WilliamsR { Length = WprPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev is null || _prev2 is null)
		{
			_prev2 = _prev;
			_prev = wprValue;
			return;
		}

		// Upward direction detected
		if (_prev < _prev2)
		{
			if (BuyPosOpen && wprValue >= _prev && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (SellPosClose && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
		// Downward direction detected
		else if (_prev > _prev2)
		{
			if (SellPosOpen && wprValue <= _prev && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (BuyPosClose && Position > 0)
			{
				SellMarket(Math.Abs(Position));
			}
		}

		_prev2 = _prev;
		_prev = wprValue;
	}
}
