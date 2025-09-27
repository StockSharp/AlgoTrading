using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Generic source-based strategy.
/// Opens long on rising candles and short on falling candles.
/// Optional stop loss, take profit, and trailing stop manage open positions.
/// </summary>
public class SourceStrategy : Strategy
{
	private readonly StrategyParam<bool> _useLongEntry;
	private readonly StrategyParam<bool> _useLongExclude;
	private readonly StrategyParam<bool> _useLongEntryWithPosition;
	private readonly StrategyParam<bool> _useShortEntry;
	private readonly StrategyParam<bool> _useShortExclude;
	private readonly StrategyParam<bool> _useShortEntryWithPosition;
	private readonly StrategyParam<bool> _useLongExit;
	private readonly StrategyParam<bool> _useLongExitExclude;
	private readonly StrategyParam<bool> _useShortExit;
	private readonly StrategyParam<bool> _useShortExitExclude;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailPointsPercent;
	private readonly StrategyParam<decimal> _trailOffsetPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;
	private bool _trailActive;

	/// <summary>
	/// Use long entries.
	/// </summary>
	public bool UseLongEntry { get => _useLongEntry.Value; set => _useLongEntry.Value = value; }

	/// <summary>
	/// Use long exclude source.
	/// </summary>
	public bool UseLongExclude { get => _useLongExclude.Value; set => _useLongExclude.Value = value; }

	/// <summary>
	/// Allow long entry even with existing position.
	/// </summary>
	public bool UseLongEntryWithPosition { get => _useLongEntryWithPosition.Value; set => _useLongEntryWithPosition.Value = value; }

	/// <summary>
	/// Use short entries.
	/// </summary>
	public bool UseShortEntry { get => _useShortEntry.Value; set => _useShortEntry.Value = value; }

	/// <summary>
	/// Use short exclude source.
	/// </summary>
	public bool UseShortExclude { get => _useShortExclude.Value; set => _useShortExclude.Value = value; }

	/// <summary>
	/// Allow short entry even with existing position.
	/// </summary>
	public bool UseShortEntryWithPosition { get => _useShortEntryWithPosition.Value; set => _useShortEntryWithPosition.Value = value; }

	/// <summary>
	/// Use long exits.
	/// </summary>
	public bool UseLongExit { get => _useLongExit.Value; set => _useLongExit.Value = value; }

	/// <summary>
	/// Use long exit exclude source.
	/// </summary>
	public bool UseLongExitExclude { get => _useLongExitExclude.Value; set => _useLongExitExclude.Value = value; }

	/// <summary>
	/// Use short exits.
	/// </summary>
	public bool UseShortExit { get => _useShortExit.Value; set => _useShortExit.Value = value; }

	/// <summary>
	/// Use short exit exclude source.
	/// </summary>
	public bool UseShortExitExclude { get => _useShortExitExclude.Value; set => _useShortExitExclude.Value = value; }

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TpPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Trailing activation distance in percent.
	/// </summary>
	public decimal TrailPointsPercent { get => _trailPointsPercent.Value; set => _trailPointsPercent.Value = value; }

	/// <summary>
	/// Trailing offset in percent.
	/// </summary>
	public decimal TrailOffsetPercent { get => _trailOffsetPercent.Value; set => _trailOffsetPercent.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the class.
	/// </summary>
	public SourceStrategy()
	{
		_useLongEntry = Param(nameof(UseLongEntry), true)
			.SetDisplay("Use Long Entry", "Enable long entries", "Entries");

		_useLongExclude = Param(nameof(UseLongExclude), false)
			.SetDisplay("Use Long Exclude", "Exclude long entries", "Entries");

		_useLongEntryWithPosition = Param(nameof(UseLongEntryWithPosition), false)
			.SetDisplay("Use Long Entry With Position", "Allow long entry with existing position", "Entries");

		_useShortEntry = Param(nameof(UseShortEntry), false)
			.SetDisplay("Use Short Entry", "Enable short entries", "Entries");

		_useShortExclude = Param(nameof(UseShortExclude), false)
			.SetDisplay("Use Short Exclude", "Exclude short entries", "Entries");

		_useShortEntryWithPosition = Param(nameof(UseShortEntryWithPosition), false)
			.SetDisplay("Use Short Entry With Position", "Allow short entry with existing position", "Entries");

		_useLongExit = Param(nameof(UseLongExit), false)
			.SetDisplay("Use Long Exit", "Enable long exits", "Exits");

		_useLongExitExclude = Param(nameof(UseLongExitExclude), false)
			.SetDisplay("Use Long Exit Exclude", "Exclude long exits", "Exits");

		_useShortExit = Param(nameof(UseShortExit), false)
			.SetDisplay("Use Short Exit", "Enable short exits", "Exits");

		_useShortExitExclude = Param(nameof(UseShortExitExclude), false)
			.SetDisplay("Use Short Exit Exclude", "Exclude short exits", "Exits");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk Management");

		_slPercent = Param(nameof(SlPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk Management");

		_tpPercent = Param(nameof(TpPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk Management");

		_trailPointsPercent = Param(nameof(TrailPointsPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Points %", "Activation distance", "Risk Management");

		_trailOffsetPercent = Param(nameof(TrailOffsetPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Offset %", "Trailing offset", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0m;
		_trailingStop = 0m;
		_trailActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longSource = candle.ClosePrice - candle.OpenPrice;
		var shortSource = -longSource;

		if (UseLongEntry && longSource > 0m && (!UseLongExclude) && (UseLongEntryWithPosition || Position == 0))
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			InitTrailing(true);
		}

		if (UseShortEntry && shortSource > 0m && (!UseShortExclude) && (UseShortEntryWithPosition || Position == 0))
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			InitTrailing(false);
		}

		if (Position > 0)
		{
			ManageLong(candle);

			if (UseLongExit && longSource < 0m && (!UseLongExitExclude))
			{
				SellMarket(Position);
				ResetTrailing();
			}
		}
		else if (Position < 0)
		{
			ManageShort(candle);

			if (UseShortExit && shortSource < 0m && (!UseShortExitExclude))
			{
				BuyMarket(-Position);
				ResetTrailing();
			}
		}
	}

	private void ManageLong(ICandleMessage candle)
	{
		if (UseStopLoss)
		{
			var stop = _entryPrice * (1m - SlPercent / 100m);
			if (candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetTrailing();
				return;
			}
		}

		if (UseTakeProfit)
		{
			var limit = _entryPrice * (1m + TpPercent / 100m);
			if (candle.HighPrice >= limit)
			{
				SellMarket(Position);
				ResetTrailing();
				return;
			}
		}

		if (UseTrailingStop)
		{
			var activation = _entryPrice * (1m + TrailPointsPercent / 100m);
			if (!_trailActive && candle.ClosePrice >= activation)
				_trailActive = true;

			if (_trailActive)
			{
				var newStop = candle.ClosePrice * (1m - TrailOffsetPercent / 100m);
				if (newStop > _trailingStop)
					_trailingStop = newStop;

				if (candle.LowPrice <= _trailingStop)
				{
					SellMarket(Position);
					ResetTrailing();
				}
			}
		}
	}

	private void ManageShort(ICandleMessage candle)
	{
		if (UseStopLoss)
		{
			var stop = _entryPrice * (1m + SlPercent / 100m);
			if (candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetTrailing();
				return;
			}
		}

		if (UseTakeProfit)
		{
			var limit = _entryPrice * (1m - TpPercent / 100m);
			if (candle.LowPrice <= limit)
			{
				BuyMarket(-Position);
				ResetTrailing();
				return;
			}
		}

		if (UseTrailingStop)
		{
			var activation = _entryPrice * (1m - TrailPointsPercent / 100m);
			if (!_trailActive && candle.ClosePrice <= activation)
				_trailActive = true;

			if (_trailActive)
			{
				var newStop = candle.ClosePrice * (1m + TrailOffsetPercent / 100m);
				if (_trailingStop == 0m || newStop < _trailingStop)
					_trailingStop = newStop;

				if (candle.HighPrice >= _trailingStop)
				{
					BuyMarket(-Position);
					ResetTrailing();
				}
			}
		}
	}

	private void InitTrailing(bool isLong)
	{
		_trailingStop = 0m;
		_trailActive = false;

		if (!UseTrailingStop)
			return;

		if (isLong)
			_trailingStop = _entryPrice * (1m - TrailOffsetPercent / 100m);
		else
			_trailingStop = _entryPrice * (1m + TrailOffsetPercent / 100m);
	}

	private void ResetTrailing()
	{
		_trailingStop = 0m;
		_trailActive = false;
	}
}
