using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Chaikin oscillator direction.
/// </summary>
public class SpectrAnalysisChaikinStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private int _barCount;
	private decimal? _fastEma;
	private decimal? _slowEma;
	private decimal? _prev;
	private decimal? _prev2;

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
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
	/// Candle type used for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpectrAnalysisChaikinStrategy"/>.
	/// </summary>
	public SpectrAnalysisChaikinStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicator");
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicator");
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Position Open", "Allow opening long positions", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Position Open", "Allow opening short positions", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Position Close", "Allow closing long positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Position Close", "Allow closing short positions", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Data");
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

		_barCount = 0;
		_fastEma = null;
		_slowEma = null;
		_prev = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barCount = 0;
		_fastEma = null;
		_slowEma = null;
		_prev = null;
		_prev2 = null;

		var ad = new AccumulationDistributionLine();
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ad, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ad);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		_fastEma = UpdateEma(_fastEma, adValue, FastMaPeriod);
		_slowEma = UpdateEma(_slowEma, adValue, SlowMaPeriod);

		if (_fastEma is not decimal fastEma || _slowEma is not decimal slowEma)
			return;

		var oscillator = fastEma - slowEma;

		if (_barCount < SlowMaPeriod || !IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(oscillator);
			return;
		}

		if (_prev is decimal prev && _prev2 is decimal prev2)
		{
			if (prev < prev2 && oscillator >= prev && oscillator > 0)
			{
				if (BuyPosOpen && Position <= 0)
					BuyMarket(Position < 0 ? Volume + Math.Abs(Position) : Volume);
				else if (SellPosClose && Position < 0)
					BuyMarket(Math.Abs(Position));
			}
			else if (prev > prev2 && oscillator <= prev && oscillator < 0)
			{
				if (SellPosOpen && Position >= 0)
					SellMarket(Position > 0 ? Volume + Position : Volume);
				else if (BuyPosClose && Position > 0)
					SellMarket(Position);
			}
		}

		UpdateHistory(oscillator);
	}

	private decimal UpdateEma(decimal? current, decimal value, int length)
	{
		if (current is null)
			return value;

		var multiplier = 2m / (length + 1);
		return current.Value + ((value - current.Value) * multiplier);
	}

	private void UpdateHistory(decimal oscillator)
	{
		_prev2 = _prev;
		_prev = oscillator;
	}
}
