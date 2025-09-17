using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MelBar Take325 strategy converted from MetaTrader.
/// Waits for a tick volume breakout confirmed by a moving average reversal.
/// Closes trades when RSI crosses the configured level or when protective targets are hit.
/// </summary>
public class MelBarTake325Strategy : Strategy
{
	private readonly StrategyParam<decimal> _entryVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _volumeThreshold;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiLevel;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumeHistory = new();
	private readonly List<decimal> _smaHistory = new();
	private readonly List<decimal> _rsiHistory = new();

	private SMA _sma = null!;
	private RSI _rsi = null!;

	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal? _entryPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Entry volume expressed in lots.
	/// </summary>
	public decimal EntryVolume
	{
		get => _entryVolume.Value;
		set => _entryVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Zero disables the stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Tick volume breakout level.
	/// </summary>
	public int VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Moving average period length.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI exit level for long trades. Short trades use the symmetric level.
	/// </summary>
	public int RsiLevel
	{
		get => _rsiLevel.Value;
		set => _rsiLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MelBarTake325Strategy"/> parameters.
	/// </summary>
	public MelBarTake325Strategy()
	{
		_entryVolume = Param(nameof(EntryVolume), 0.1m)
			.SetDisplay("Entry Volume", "Order volume expressed in lots", "General")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 16)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk Management")
			.SetGreaterOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 45)
			.SetDisplay("Take Profit (pips)", "Target distance expressed in pips", "Risk Management")
			.SetGreaterOrEqualZero();

		_volumeThreshold = Param(nameof(VolumeThreshold), 1000)
			.SetDisplay("Volume Threshold", "Minimum tick volume required for breakout detection", "Indicators")
			.SetGreaterThanZero();

		_smaPeriod = Param(nameof(SmaPeriod), 12)
			.SetDisplay("SMA Period", "Length of the simple moving average on closing prices", "Indicators")
			.SetGreaterThanZero();

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators")
			.SetGreaterThanZero();

		_rsiLevel = Param(nameof(RsiLevel), 80)
			.SetDisplay("RSI Level", "Exit level for long trades", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "General");
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

		_volumeHistory.Clear();
		_smaHistory.Clear();
		_rsiHistory.Clear();
		_sma = null!;
		_rsi = null!;
		_pipSize = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_entryPrice = null;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = EntryVolume;

		_sma = new SMA { Length = SmaPeriod };
		_rsi = new RSI { Length = RsiPeriod };

		_pipSize = CalculatePipSize();
		_stopLossDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		_takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageStopsAndTargets(candle);

		if (Position > 0)
		{
			if (ShouldCloseLongByRsi())
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			if (ShouldCloseShortByRsi())
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
		}

		if (Position == 0)
		{
			var volumeBreakout = HasVolumeBreakout();
			var canOpenLong = volumeBreakout && HasBullishSmaSetup();
			var canOpenShort = volumeBreakout && HasBearishSmaSetup();

			if (canOpenLong && canOpenShort)
			{
				canOpenLong = false;
				canOpenShort = false;
			}

			if (canOpenLong)
			{
				EnterLong(candle);
			}
			else if (canOpenShort)
			{
				EnterShort(candle);
			}
		}

		UpdateHistory(candle.TotalVolume ?? 0m, smaValue, rsiValue);
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = EntryVolume;

		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_isLongPosition = true;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = EntryVolume;

		if (volume <= 0m)
			return;

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_isLongPosition = false;
	}

	private void ManageStopsAndTargets(ICandleMessage candle)
	{
		if (_entryPrice == null || Position == 0)
			return;

		var entry = _entryPrice.Value;

		if (_isLongPosition)
		{
			if (_stopLossDistance > 0m && candle.LowPrice <= entry - _stopLossDistance)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitDistance > 0m && candle.HighPrice >= entry + _takeProfitDistance)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else
		{
			if (_stopLossDistance > 0m && candle.HighPrice >= entry + _stopLossDistance)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitDistance > 0m && candle.LowPrice <= entry - _takeProfitDistance)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
		}
	}

	private bool HasVolumeBreakout()
	{
		if (_volumeHistory.Count < 3)
			return false;

		var older = _volumeHistory[^3];
		var previous = _volumeHistory[^2];
		var threshold = (decimal)VolumeThreshold;

		return older < threshold && previous > threshold;
	}

	private bool HasBullishSmaSetup()
	{
		if (!_sma.IsFormed || _smaHistory.Count < 3)
			return false;

		var older = _smaHistory[^3];
		var previous = _smaHistory[^2];
		var last = _smaHistory[^1];

		return older < previous && previous > last;
	}

	private bool HasBearishSmaSetup()
	{
		if (!_sma.IsFormed || _smaHistory.Count < 3)
			return false;

		var older = _smaHistory[^3];
		var previous = _smaHistory[^2];
		var last = _smaHistory[^1];

		return older > previous && previous < last;
	}

	private bool ShouldCloseLongByRsi()
	{
		if (!_rsi.IsFormed || _rsiHistory.Count < 3)
			return false;

		var older = _rsiHistory[^3];
		var previous = _rsiHistory[^2];
		var exitLevel = (decimal)RsiLevel;

		return older > exitLevel && previous < exitLevel;
	}

	private bool ShouldCloseShortByRsi()
	{
		if (!_rsi.IsFormed || _rsiHistory.Count < 3)
			return false;

		var older = _rsiHistory[^3];
		var previous = _rsiHistory[^2];
		var exitLevel = 100m - RsiLevel;

		return older < exitLevel && previous > exitLevel;
	}

	private void UpdateHistory(decimal volume, decimal smaValue, decimal rsiValue)
	{
		AddHistory(_volumeHistory, volume);

		if (_sma.IsFormed)
			AddHistory(_smaHistory, smaValue);

		if (_rsi.IsFormed)
			AddHistory(_rsiHistory, rsiValue);
	}

	private static void AddHistory(List<decimal> history, decimal value)
	{
		history.Add(value);

		if (history.Count > 10)
			history.RemoveAt(0);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_isLongPosition = false;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
			step *= 10m;

		return step;
	}
}
