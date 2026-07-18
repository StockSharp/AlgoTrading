using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on CCI (Commodity Channel Index) breakout.
/// Buys when CCI crosses above +100, sells when CCI crosses below -100.
/// </summary>
public class CciBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;
	private bool _hasPrevValues;
	private int _cooldown;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CciBreakoutStrategy"/>.
	/// </summary>
	public CciBreakoutStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
			.SetOptimize(14, 30, 4);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevCci = default;
		_hasPrevValues = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciInd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (cciInd.IsEmpty)
			return;

		decimal cciValue;
		try
		{
			cciValue = cciInd.GetValue<decimal>();
		}
		catch (IndexOutOfRangeException)
		{
			return;
		}

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevCci = cciValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevCci = cciValue;
			return;
		}

		// CCI crosses above +100 - buy signal
		if (_prevCci <= 100 && cciValue > 100 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 2;
		}
		// CCI crosses below -100 - sell signal
		else if (_prevCci >= -100 && cciValue < -100 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 2;
		}

		_prevCci = cciValue;
	}
}
