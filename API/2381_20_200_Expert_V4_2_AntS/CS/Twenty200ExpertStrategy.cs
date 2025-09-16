using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// "20/200 expert v4.2 AntS" strategy.
/// Opens one trade per day based on the difference between two past opens.
/// Calculates dynamic volume and closes positions by stop-loss, take-profit or time.
/// </summary>
public class Twenty200ExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitLong;
	private readonly StrategyParam<int> _stopLossLong;
	private readonly StrategyParam<int> _takeProfitShort;
	private readonly StrategyParam<int> _stopLossShort;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _t1;
	private readonly StrategyParam<int> _t2;
	private readonly StrategyParam<int> _deltaLong;
	private readonly StrategyParam<int> _deltaShort;
	private readonly StrategyParam<decimal> _lot;
	private readonly StrategyParam<int> _bigLotSize;
	private readonly StrategyParam<bool> _autoLot;
	private readonly StrategyParam<int> _maxOpenTime;
	private readonly StrategyParam<DataType> _candleType;

	private Shift _shiftT1 = null!;
	private Shift _shiftT2 = null!;

	private bool _canTrade = true;
	private decimal _extLot;
	private decimal _prevBalance;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTimeOffset _entryTime;
	private bool _isLong;

	/// <summary>
	/// Take profit for long trades in pips.
	/// </summary>
	public int TakeProfitLong
	{
		get => _takeProfitLong.Value;
		set => _takeProfitLong.Value = value;
	}

	/// <summary>
	/// Stop loss for long trades in pips.
	/// </summary>
	public int StopLossLong
	{
		get => _stopLossLong.Value;
		set => _stopLossLong.Value = value;
	}

	/// <summary>
	/// Take profit for short trades in pips.
	/// </summary>
	public int TakeProfitShort
	{
		get => _takeProfitShort.Value;
		set => _takeProfitShort.Value = value;
	}

	/// <summary>
	/// Stop loss for short trades in pips.
	/// </summary>
	public int StopLossShort
	{
		get => _stopLossShort.Value;
		set => _stopLossShort.Value = value;
	}

	/// <summary>
	/// Hour of the day to check entry conditions.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	/// <summary>
	/// Bars shift for the first open price.
	/// </summary>
	public int T1
	{
		get => _t1.Value;
		set => _t1.Value = value;
	}

	/// <summary>
	/// Bars shift for the second open price.
	/// </summary>
	public int T2
	{
		get => _t2.Value;
		set => _t2.Value = value;
	}

	/// <summary>
	/// Minimal difference in pips to open long.
	/// </summary>
	public int DeltaLong
	{
		get => _deltaLong.Value;
		set => _deltaLong.Value = value;
	}

	/// <summary>
	/// Minimal difference in pips to open short.
	/// </summary>
	public int DeltaShort
	{
		get => _deltaShort.Value;
		set => _deltaShort.Value = value;
	}

	/// <summary>
	/// Base lot size.
	/// </summary>
	public decimal Lot
	{
		get => _lot.Value;
		set => _lot.Value = value;
	}

	/// <summary>
	/// Multiplier for lot after a losing trade.
	/// </summary>
	public int BigLotSize
	{
		get => _bigLotSize.Value;
		set => _bigLotSize.Value = value;
	}

	/// <summary>
	/// Enable automatic lot size calculation.
	/// </summary>
	public bool AutoLot
	{
		get => _autoLot.Value;
		set => _autoLot.Value = value;
	}

	/// <summary>
	/// Maximum position holding time in hours.
	/// </summary>
	public int MaxOpenTime
	{
		get => _maxOpenTime.Value;
		set => _maxOpenTime.Value = value;
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
	/// Constructor.
	/// </summary>
	public Twenty200ExpertStrategy()
	{
		_takeProfitLong =
			Param(nameof(TakeProfitLong), 39).SetDisplay("TP Long (pips)", "Take profit for long", "Risk");
		_stopLossLong = Param(nameof(StopLossLong), 147).SetDisplay("SL Long (pips)", "Stop loss for long", "Risk");
		_takeProfitShort =
			Param(nameof(TakeProfitShort), 32).SetDisplay("TP Short (pips)", "Take profit for short", "Risk");
		_stopLossShort = Param(nameof(StopLossShort), 267).SetDisplay("SL Short (pips)", "Stop loss for short", "Risk");
		_tradeHour = Param(nameof(TradeHour), 18).SetDisplay("Trade Hour", "Hour to enter", "General");
		_t1 = Param(nameof(T1), 6).SetDisplay("T1", "First bar shift", "Logic");
		_t2 = Param(nameof(T2), 2).SetDisplay("T2", "Second bar shift", "Logic");
		_deltaLong = Param(nameof(DeltaLong), 6).SetDisplay("Delta Long", "Min rise", "Logic");
		_deltaShort = Param(nameof(DeltaShort), 21).SetDisplay("Delta Short", "Min fall", "Logic");
		_lot = Param(nameof(Lot), 0.01m).SetDisplay("Lot", "Base volume", "Volume");
		_bigLotSize = Param(nameof(BigLotSize), 6).SetDisplay("Big Lot Multiplier", "Volume multiplier", "Volume");
		_autoLot = Param(nameof(AutoLot), true).SetDisplay("Auto Lot", "Use dynamic volume", "Volume");
		_maxOpenTime = Param(nameof(MaxOpenTime), 504).SetDisplay("Max Open Time", "Max hours to hold", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
						  .SetDisplay("Candle Type", "Time frame", "General");
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

		_shiftT1 = new Shift { Length = T1 };
		_shiftT2 = new Shift { Length = T2 };

		_prevBalance = Portfolio?.CurrentValue ?? 0m;
		UpdateLot();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void UpdateLot()
	{
		if (!AutoLot)
		{
			_extLot = Lot;
			return;
		}

		var balance = Portfolio?.CurrentValue ?? 0m;
		var min = Security?.MinVolume ?? 0.01m;
		var step = Security?.VolumeStep ?? 0.01m;
		var max = Security?.MaxVolume ?? 100m;

		var volume = balance * min / 300m;
		volume = Math.Floor(volume / step) * step;
		volume = Math.Max(min, Math.Min(max, volume));
		_extLot = volume;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		UpdateLot();
		_canTrade = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var pip = Security?.PriceStep ?? 1m;
		var openT1 = _shiftT1.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		var openT2 = _shiftT2.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();

		if (Position != 0)
		{
			if (MaxOpenTime > 0 && candle.OpenTime - _entryTime >= TimeSpan.FromHours(MaxOpenTime))
			{
				ClosePosition();
				return;
			}

			if (_isLong)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
					ClosePosition();
			}
			else
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
					ClosePosition();
			}

			return;
		}

		if (candle.OpenTime.Hour > TradeHour)
			_canTrade = true;

		if (!_canTrade || candle.OpenTime.Hour != TradeHour)
			return;

		if (!_shiftT1.IsFormed || !_shiftT2.IsFormed)
			return;

		var volume = AutoLot ? _extLot : Lot;
		var balance = Portfolio?.CurrentValue ?? 0m;
		if (_prevBalance > balance)
			volume *= BigLotSize;
		_prevBalance = balance;

		var diffShort = openT1 - openT2;
		var diffLong = openT2 - openT1;

		if (diffShort > DeltaShort * pip)
		{
			SellMarket(volume);
			_isLong = false;
			_entryTime = candle.OpenTime;
			_stopPrice = candle.OpenPrice + StopLossShort * pip;
			_takePrice = candle.OpenPrice - TakeProfitShort * pip;
			_canTrade = false;
		}
		else if (diffLong > DeltaLong * pip)
		{
			BuyMarket(volume);
			_isLong = true;
			_entryTime = candle.OpenTime;
			_stopPrice = candle.OpenPrice - StopLossLong * pip;
			_takePrice = candle.OpenPrice + TakeProfitLong * pip;
			_canTrade = false;
		}
	}
}
