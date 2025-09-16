using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a custom XdinMA indicator derived from two moving averages.
/// The line is calculated as <c>ma_main * 2 - ma_plus</c> and orders are generated when its slope changes direction.
/// </summary>
public class ColorXdinMAStrategy : Strategy
{
	private readonly StrategyParam<int> _mainLength;
	private readonly StrategyParam<int> _plusLength;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _mainMa = null!;
	private SMA _plusMa = null!;

	private bool _isInitialized;
	private decimal _prev;
	private decimal _prevPrev;

	/// <summary>
	/// Period of the main moving average.
	/// </summary>
	public int MainLength
	{
		get => _mainLength.Value;
		set => _mainLength.Value = value;
	}

	/// <summary>
	/// Period of the additional moving average.
	/// </summary>
	public int PlusLength
	{
		get => _plusLength.Value;
		set => _plusLength.Value = value;
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
	/// Initializes a new instance of <see cref="ColorXdinMAStrategy"/>.
	/// </summary>
	public ColorXdinMAStrategy()
	{
		_mainLength = Param(nameof(MainLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Main MA Length", "Period of the main moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_plusLength = Param(nameof(PlusLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Additional MA Length", "Period of the additional moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mainMa = new SMA { Length = MainLength };
		_plusMa = new SMA { Length = PlusLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_mainMa, _plusMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _mainMa);
			DrawIndicator(area, _plusMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal main, decimal plus)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var xdin = main * 2m - plus;

		if (!_isInitialized)
		{
			if (_mainMa.IsFormed && _plusMa.IsFormed)
			{
				_prevPrev = _prev = xdin;
				_isInitialized = true;
			}
			return;
		}

		if (_prev < _prevPrev && xdin > _prev)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prev > _prevPrev && xdin < _prev)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevPrev = _prev;
		_prev = xdin;
	}
}
