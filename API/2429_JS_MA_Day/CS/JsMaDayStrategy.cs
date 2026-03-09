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
/// Daily SMA strategy comparing moving average to open price.
/// Opens position when the moving average crosses daily open in trend direction.
/// </summary>
public class JsMaDayStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<decimal> _prices = new();
	private decimal? _prevMa;
	private decimal? _prevOpen;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
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
	/// Constructor.
	/// </summary>
	public JsMaDayStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA period on daily candles", "Parameters")
			
			.SetOptimize(2, 20, 1);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse Signals", "Reverse entry direction", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for moving average", "General");
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
		_prices.Clear();
		_prevMa = null;
		_prevOpen = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prices.Add(candle.ClosePrice);
		if (_prices.Count > MaPeriod)
			_prices.RemoveAt(0);

		if (_prices.Count < MaPeriod)
			return;

		var sum = 0m;
		foreach (var price in _prices)
			sum += price;

		var ma = sum / _prices.Count;
		var open = candle.OpenPrice;

		if (_prevMa is decimal prevMa && _prevOpen is decimal prevOpen)
		{
			var buyCondition = ma > open && prevMa <= prevOpen;
			var sellCondition = ma < open && prevMa >= prevOpen;

			if (buyCondition)
			{
				if (!Reverse && Position <= 0)
					BuyMarket();
				else if (Reverse && Position >= 0)
					SellMarket();
			}
			else if (sellCondition)
			{
				if (!Reverse && Position >= 0)
					SellMarket();
				else if (Reverse && Position <= 0)
					BuyMarket();
			}
		}

		_prevMa = ma;
		_prevOpen = open;
	}
}
