namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pure price action strategy using Break of Structure (BOS) and Market Structure Shift (MSS).
/// </summary>
public class PurePriceActionStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableBos;
	private readonly StrategyParam<bool> _enableMss;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Enable Break of Structure entries.
	/// </summary>
	public bool EnableBos
	{
		get => _enableBos.Value;
		set => _enableBos.Value = value;
	}

	/// <summary>
	/// Enable Market Structure Shift entries.
	/// </summary>
	public bool EnableMss
	{
		get => _enableMss.Value;
		set => _enableMss.Value = value;
	}

	/// <summary>
	/// Stop-loss percent.
	/// </summary>
	public decimal SlPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percent.
	/// </summary>
	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	/// <summary>
	/// Lookback period for highest and lowest calculations.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Initializes a new instance of <see cref="PurePriceActionStrategy"/>.
	/// </summary>
	public PurePriceActionStrategy()
	{
		_enableBos = Param(nameof(EnableBos), true)
			.SetDisplay("Enable BOS", "Use Break of Structure entries", "Parameters");

		_enableMss = Param(nameof(EnableMss), true)
			.SetDisplay("Enable MSS", "Use Market Structure Shift entries", "Parameters");

		_slPercent = Param(nameof(SlPercent), 1m)
			.SetDisplay("Stop-Loss %", "Stop-loss percent", "Risk");

		_tpPercent = Param(nameof(TpPercent), 2m)
			.SetDisplay("Take-Profit %", "Take-profit percent", "Risk");

		_length = Param(nameof(Length), 5)
			.SetDisplay("Length", "Lookback length", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type", "General");
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

		_highest = null!;
		_lowest = null!;
		_prevHighest = _prevLowest = 0m;
		_stopLoss = _takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Length };
		_lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevHighest = highest;
			_prevLowest = lowest;
			return;
		}

		var bosSignal = EnableBos && _prevHighest <= _prevLowest && highest > lowest;
		var mssSignal = EnableMss && _prevLowest >= _prevHighest && lowest < highest;

		if (Position <= 0 && bosSignal)
		{
			var entry = candle.ClosePrice;
			_stopLoss = entry * (1m - SlPercent / 100m);
			_takeProfit = entry * (1m + TpPercent / 100m);
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && mssSignal)
		{
			var entry = candle.ClosePrice;
			_stopLoss = entry * (1m + SlPercent / 100m);
			_takeProfit = entry * (1m - TpPercent / 100m);
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && (candle.LowPrice <= _stopLoss || candle.ClosePrice >= _takeProfit))
		{
			ClosePosition();
		}
		else if (Position < 0 && (candle.HighPrice >= _stopLoss || candle.ClosePrice <= _takeProfit))
		{
			ClosePosition();
		}

		_prevHighest = highest;
		_prevLowest = lowest;
	}
}
