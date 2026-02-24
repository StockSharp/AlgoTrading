using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Q2MA cross strategy based on open and close moving averages.
/// Buys when close MA crosses above open MA, sells on opposite.
/// </summary>
public class Q2maCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _closeMa;
	private ExponentialMovingAverage _openMa;
	private decimal? _prevUp;
	private decimal? _prevDn;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Q2maCrossStrategy()
	{
		_length = Param(nameof(Length), 8)
			.SetDisplay("Length", "Moving average length", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Indicator timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUp = null;
		_prevDn = null;

		_closeMa = new ExponentialMovingAverage { Length = Length };
		_openMa = new ExponentialMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.ServerTime;

		var upResult = _closeMa.Process(candle.ClosePrice, t, true);
		var dnResult = _openMa.Process(candle.OpenPrice, t, true);

		if (!_closeMa.IsFormed || !_openMa.IsFormed)
			return;

		var up = upResult.GetValue<decimal>();
		var dn = dnResult.GetValue<decimal>();

		if (_prevUp is null || _prevDn is null)
		{
			_prevUp = up;
			_prevDn = dn;
			return;
		}

		// Close MA crosses above Open MA -> buy signal
		if (_prevUp <= _prevDn && up > dn && Position <= 0)
			BuyMarket();
		// Close MA crosses below Open MA -> sell signal
		else if (_prevUp >= _prevDn && up < dn && Position >= 0)
			SellMarket();

		_prevUp = up;
		_prevDn = dn;
	}
}
