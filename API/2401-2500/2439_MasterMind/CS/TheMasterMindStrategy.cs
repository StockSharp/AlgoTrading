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
/// Strategy entering on extreme Stochastic and Williams %R values.
/// </summary>
public class TheMasterMindStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _wpr;
	private decimal? _prevD;
	private int _lastSignal;

	/// <summary>
	/// Stochastic base length.
	/// </summary>
	public int StochasticLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TheMasterMindStrategy"/>.
	/// </summary>
	public TheMasterMindStrategy()
	{
		_stochLength = Param(nameof(StochasticLength), 14)
			.SetDisplay("Stochastic Length", "Base length for Stochastic", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "Common");
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
		_prevD = null;
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochasticLength },
			D = { Length = 3 }
		};

		_wpr = new WilliamsR { Length = StochasticLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, (candle, stochValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				// Process WilliamsR manually (candle-based)
				var wprResult = _wpr.Process(candle);
				if (!_wpr.IsFormed)
					return;

				var stoch = (IStochasticOscillatorValue)stochValue;
				if (stoch.D is not decimal d || stoch.K is not decimal k)
					return;

				var wpr = wprResult.ToDecimal();

				var buySignal = _prevD >= 20m && d < 20m && wpr < -85m;
				var sellSignal = _prevD <= 80m && d > 80m && wpr > -15m;

				if (buySignal && _lastSignal != 1 && Position <= 0)
				{
					BuyMarket();
					_lastSignal = 1;
				}
				else if (sellSignal && _lastSignal != -1 && Position >= 0)
				{
					SellMarket();
					_lastSignal = -1;
				}

				_prevD = d;
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
