using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// X2MA with JFATL filter strategy.
/// Opens long when the fast SMA crosses above the slow Jurik MA and price is above the filter.
/// Opens short when the fast SMA crosses below the slow Jurik MA and price is below the filter.
/// </summary>
public class X2MaJfatlStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _filterLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;
	private bool _isInitialized;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow Jurik moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Jurik filter length.
	/// </summary>
	public int FilterLength
	{
		get => _filterLength.Value;
		set => _filterLength.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public X2MaJfatlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of the fast moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowLength = Param(nameof(SlowLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of the slow Jurik MA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_filterLength = Param(nameof(FilterLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Filter Length", "Length of the Jurik filter", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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
		_prevDiff = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new JurikMovingAverage { Length = SlowLength };
		var filterMa = new JurikMovingAverage { Length = FilterLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastMa, slowMa, filterMa, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, filterMa);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevDiff = fastValue - slowValue;
			_isInitialized = true;
			return;
		}

		var diff = fastValue - slowValue;

		// Exit if price moves against the filter
		if (Position > 0 && candle.ClosePrice < filterValue)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && candle.ClosePrice > filterValue)
		{
			BuyMarket(Math.Abs(Position));
		}

		// Crossover entries
		if (_prevDiff <= 0m && diff > 0m && candle.ClosePrice > filterValue && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevDiff >= 0m && diff < 0m && candle.ClosePrice < filterValue && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevDiff = diff;
	}
}
