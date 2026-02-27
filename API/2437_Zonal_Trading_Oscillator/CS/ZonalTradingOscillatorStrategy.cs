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
/// Zone trading strategy based on Bill Williams' Awesome and Accelerator Oscillators.
/// Buys when both oscillators turn green and sells when both turn red.
/// </summary>
public class ZonalTradingOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private AwesomeOscillator _ao;
	private SimpleMovingAverage _acMa;
	private decimal? _prevAo;
	private decimal? _prevAc;
	private int _aoTrend;
	private int _acTrend;

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZonalTradingOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for oscillators", "General");
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

		_ao = new AwesomeOscillator
		{
			ShortMa = { Length = 5 },
			LongMa = { Length = 34 }
		};
		_acMa = new SimpleMovingAverage { Length = 5 };

		_prevAo = null;
		_prevAc = null;
		_aoTrend = 0;
		_acTrend = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ao, (candle, aoValue) =>
			{
				if (candle.State != CandleStates.Finished || !aoValue.IsFinal)
					return;

				var ao = aoValue.ToDecimal();

				// Compute AC = AO - SMA(AO, 5)
				var aoSma = _acMa.Process(ao, candle.CloseTime, true);
				if (!_acMa.IsFormed)
				{
					_prevAo = ao;
					return;
				}

				var ac = ao - aoSma.ToDecimal();

				if (_prevAo is not null && _prevAc is not null)
				{
					_aoTrend = ao > _prevAo ? 1 : ao < _prevAo ? -1 : _aoTrend;
					_acTrend = ac > _prevAc ? 1 : ac < _prevAc ? -1 : _acTrend;

					// Close positions on opposite signal
					if (Position > 0 && (_aoTrend < 0 || _acTrend < 0))
					{
						SellMarket();
					}
					else if (Position < 0 && (_aoTrend > 0 || _acTrend > 0))
					{
						BuyMarket();
					}

					// Open new positions when both agree
					if (Position <= 0 && _aoTrend > 0 && _acTrend > 0)
					{
						BuyMarket();
					}
					else if (Position >= 0 && _aoTrend < 0 && _acTrend < 0)
					{
						SellMarket();
					}
				}

				_prevAo = ao;
				_prevAc = ac;
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
