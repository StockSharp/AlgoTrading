using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// On-Balance Volume (OBV) breakout strategy with ATR-like channel.
/// Opens long when OBV crosses above previous high, short when below previous low.
/// </summary>
public class ObvAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevObv;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private int _mode;
	private int _prevMode;

	/// <summary>
	/// Lookback length for OBV high/low calculation.
	/// </summary>
	public int LookbackLength
	{
		get => _lookbackLength.Value;
		set => _lookbackLength.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ObvAtrStrategy"/>.
	/// </summary>
	public ObvAtrStrategy()
	{
		_lookbackLength = Param(nameof(LookbackLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("OBV Lookback", "Lookback length for OBV highs and lows", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Parameters");
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

		_prevObv = null;
		_prevHigh = null;
		_prevLow = null;
		_mode = 0;
		_prevMode = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var obv = new OnBalanceVolume();
		var highest = new Highest { Length = LookbackLength };
		var lowest = new Lowest { Length = LookbackLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(obv, (candle, value) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var obvVal = value.ToDecimal();

				var prevHigh = _prevHigh;
				var prevLow = _prevLow;
				var prevObv = _prevObv;

				var high = highest.Process(value).ToDecimal();
				var low = lowest.Process(value).ToDecimal();

				_prevObv = obvVal;
				_prevHigh = high;
				_prevLow = low;

				if (prevHigh.HasValue && prevObv.HasValue && obvVal > prevHigh.Value && prevObv <= prevHigh.Value)
					_mode = 1;
				else if (prevLow.HasValue && prevObv.HasValue && obvVal < prevLow.Value && prevObv >= prevLow.Value)
					_mode = -1;

				var bullSignal = _mode == 1 && _prevMode != 1;
				var bearSignal = _mode == -1 && _prevMode != -1;
				_prevMode = _mode;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (bullSignal)
					BuyMarket(Volume + Math.Abs(Position));

				if (bearSignal)
					SellMarket(Volume + Math.Abs(Position));
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(3, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, obv);
			DrawOwnTrades(area);
		}
	}
}
