using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic RSI OHLC bars.
/// </summary>
public class StochasticRsiOhlcStrategy : Strategy
{
	private readonly StrategyParam<int> _kLength;
	private readonly StrategyParam<int> _dLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _longEntry;
	private readonly StrategyParam<decimal> _shortEntry;
	private readonly StrategyParam<decimal> _longPivot;
	private readonly StrategyParam<decimal> _shortPivot;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsiClose = null!;
	private RelativeStrengthIndex _rsiHigh = null!;
	private RelativeStrengthIndex _rsiLow = null!;
	private StochasticOscillator _stochClose = null!;
	private StochasticOscillator _stochHigh = null!;
	private StochasticOscillator _stochLow = null!;

	private decimal _prev1;
	private decimal _prev2;
	private decimal _prev3;
	private bool _hasPrev1;
	private bool _hasPrev2;
	private bool _hasPrev3;

	/// <summary>
	/// %K length.
	/// </summary>
	public int KLength
	{
		get => _kLength.Value;
		set => _kLength.Value = value;
	}

	/// <summary>
	/// %D length.
	/// </summary>
	public int DLength
	{
		get => _dLength.Value;
		set => _dLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Long entry level.
	/// </summary>
	public decimal LongEntry
	{
		get => _longEntry.Value;
		set => _longEntry.Value = value;
	}

	/// <summary>
	/// Short entry level.
	/// </summary>
	public decimal ShortEntry
	{
		get => _shortEntry.Value;
		set => _shortEntry.Value = value;
	}

	/// <summary>
	/// Long pivot level.
	/// </summary>
	public decimal LongPivot
	{
		get => _longPivot.Value;
		set => _longPivot.Value = value;
	}

	/// <summary>
	/// Short pivot level.
	/// </summary>
	public decimal ShortPivot
	{
		get => _shortPivot.Value;
		set => _shortPivot.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticRsiOhlcStrategy"/> class.
	/// </summary>
	public StochasticRsiOhlcStrategy()
	{
		_kLength = Param(nameof(KLength), 14)
			.SetRange(5, 30)
			.SetDisplay("K Length", "%K length", "Indicators")
			.SetCanOptimize(true);

		_dLength = Param(nameof(DLength), 3)
			.SetRange(1, 10)
			.SetDisplay("D Length", "%D length", "Indicators")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetRange(5, 30)
			.SetDisplay("RSI Length", "RSI length", "Indicators")
			.SetCanOptimize(true);

		_longEntry = Param(nameof(LongEntry), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Enter Long", "Long entry", "Trading")
			.SetCanOptimize(true);

		_shortEntry = Param(nameof(ShortEntry), 60m)
			.SetRange(0m, 100m)
			.SetDisplay("Enter Short", "Short entry", "Trading")
			.SetCanOptimize(true);

		_longPivot = Param(nameof(LongPivot), 2m)
			.SetRange(0m, 100m)
			.SetDisplay("Long Pivot", "Long pivot", "Trading")
			.SetCanOptimize(true);

		_shortPivot = Param(nameof(ShortPivot), 98m)
			.SetRange(0m, 100m)
			.SetDisplay("Short Pivot", "Short pivot", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		_rsiClose = new RelativeStrengthIndex { Length = RsiLength };
		_rsiHigh = new RelativeStrengthIndex { Length = RsiLength };
		_rsiLow = new RelativeStrengthIndex { Length = RsiLength };

		_stochClose = new StochasticOscillator
		{
			K = { Length = KLength },
			D = { Length = DLength }
		};
		_stochHigh = new StochasticOscillator
		{
			K = { Length = KLength },
			D = { Length = DLength }
		};
		_stochLow = new StochasticOscillator
		{
			K = { Length = KLength },
			D = { Length = DLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochClose);
			DrawIndicator(area, _stochHigh);
			DrawIndicator(area, _stochLow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsiClose = _rsiClose.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var rsiHigh = _rsiHigh.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var rsiLow = _rsiLow.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		var stochCloseVal = (StochasticOscillatorValue)_stochClose.Process(rsiClose, candle.OpenTime, true);
		var stochHighVal = (StochasticOscillatorValue)_stochHigh.Process(rsiHigh, candle.OpenTime, true);
		var stochLowVal = (StochasticOscillatorValue)_stochLow.Process(rsiLow, candle.OpenTime, true);

		if (stochCloseVal.K is not decimal stochClose ||
			stochHighVal.K is not decimal stochHigh ||
			stochLowVal.K is not decimal stochLow)
		{
			return;
		}

		var stochOpen = _hasPrev1 ? _prev1 : stochClose;

		if (!_hasPrev1)
		{
			_prev1 = stochClose;
			_hasPrev1 = true;
			return;
		}
		if (!_hasPrev2)
		{
			_prev2 = _prev1;
			_prev1 = stochClose;
			_hasPrev2 = true;
			return;
		}
		if (!_hasPrev3)
		{
			_prev3 = _prev2;
			_prev2 = _prev1;
			_prev1 = stochClose;
			_hasPrev3 = true;
			return;
		}

		var longCondition = _prev3 >= LongPivot && _prev1 < stochClose &&
			(_prev2 >= LongEntry || _prev1 >= LongEntry || stochClose >= LongEntry);
		var shortCondition = _prev3 <= ShortPivot && _prev1 > stochClose &&
			(_prev2 <= ShortEntry || _prev1 <= ShortEntry || stochClose <= ShortEntry);

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = stochClose;
	}
}

