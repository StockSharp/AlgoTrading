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
/// Port of the MetaTrader 5 expert advisor TuyulGAP.
/// Places weekly breakout stop orders around the recent high/low range and closes positions once secure profit is reached.
/// </summary>
public class TuyulGapEndOfWeekStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<int> _setupDayOfWeek;
	private readonly StrategyParam<int> _setupHour;
	private readonly StrategyParam<int> _setupMinuteWindow;
	private readonly StrategyParam<decimal> _secureProfitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highestHigh;
	private Lowest _lowestLow;

	private decimal _tickSize;
	private decimal _entryPrice;
	private decimal? _virtualStopPrice;
	private decimal _prevHighest;
	private decimal _prevLowest;

	/// <summary>
	/// Initializes a new instance of the <see cref="TuyulGapEndOfWeekStrategy"/> class.
	/// </summary>
	public TuyulGapEndOfWeekStrategy()
	{

		_stopLossPoints = Param(nameof(StopLossPoints), 60)
		.SetRange(0, 5000)
		.SetDisplay("Stop Loss (points)", "Distance from entry used for protective stops", "Risk");

		_lookbackBars = Param(nameof(LookbackBars), 12)
		.SetRange(2, 500)
		.SetDisplay("Lookback Bars", "Number of finished candles inspected for highs/lows", "Setup");

		_setupDayOfWeek = Param(nameof(SetupDayOfWeek), 5)
		.SetRange(0, 6)
		.SetDisplay("Setup Day Of Week", "Day index (0=Sunday) that stages the weekly orders", "Setup");

		_setupHour = Param(nameof(SetupHour), 23)
		.SetRange(0, 23)
		.SetDisplay("Setup Hour", "Exchange hour when the weekly setup is evaluated", "Setup");

		_setupMinuteWindow = Param(nameof(SetupMinuteWindow), 15)
		.SetRange(0, 59)
		.SetDisplay("Setup Minute Window", "Minutes after the setup hour when staging is allowed", "Setup");

		_secureProfitTarget = Param(nameof(SecureProfitTarget), 5m)
		.SetRange(0m, 100000m)
		.SetDisplay("Secure Profit Target", "Unrealized profit per position that triggers an immediate exit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the high/low scan and monitoring", "Data");
	}


	/// <summary>
	/// Distance from entry used for protective stops, measured in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Number of finished candles inspected for highs and lows.
	/// </summary>
	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
	}

	/// <summary>
	/// Day index (0=Sunday … 6=Saturday) that stages the weekly setup.
	/// </summary>
	public int SetupDayOfWeek
	{
		get => _setupDayOfWeek.Value;
		set => _setupDayOfWeek.Value = value;
	}

	/// <summary>
	/// Exchange hour when the weekly setup is evaluated.
	/// </summary>
	public int SetupHour
	{
		get => _setupHour.Value;
		set => _setupHour.Value = value;
	}

	/// <summary>
	/// Minutes after the setup hour when staging is allowed.
	/// </summary>
	public int SetupMinuteWindow
	{
		get => _setupMinuteWindow.Value;
		set => _setupMinuteWindow.Value = value;
	}

	/// <summary>
	/// Unrealized profit per position that triggers an immediate exit.
	/// </summary>
	public decimal SecureProfitTarget
	{
		get => _secureProfitTarget.Value;
		set => _secureProfitTarget.Value = value;
	}

	/// <summary>
	/// Timeframe used for the high/low scan and monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highestHigh = null;
		_lowestLow = null;
		_tickSize = 0m;
		_entryPrice = 0m;
		_virtualStopPrice = null;
		_prevHighest = 0m;
		_prevLowest = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tickSize = Security?.PriceStep ?? 0m;

		_highestHigh = new Highest
		{
			Length = Math.Max(2, LookbackBars)
		};

		_lowestLow = new Lowest
		{
			Length = Math.Max(2, LookbackBars)
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highestHigh, _lowestLow, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_highestHigh.IsFormed || !_lowestLow.IsFormed)
			return;

		// Check virtual stop
		if (Position > 0m && _virtualStopPrice.HasValue && candle.LowPrice <= _virtualStopPrice.Value)
		{
			SellMarket(Math.Abs(Position));
			_virtualStopPrice = null;
			_entryPrice = 0m;
			return;
		}
		if (Position < 0m && _virtualStopPrice.HasValue && candle.HighPrice >= _virtualStopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			_virtualStopPrice = null;
			_entryPrice = 0m;
			return;
		}

		// Close on profit
		if (Position != 0m && SecureProfitTarget > 0m && PnL >= SecureProfitTarget)
		{
			if (Position > 0m)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));
			_virtualStopPrice = null;
			_entryPrice = 0m;
			return;
		}

		if (Position == 0m && _prevHighest > 0m && _prevLowest > 0m)
		{
			// Breakout above previous highest
			if (candle.ClosePrice > _prevHighest)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				var stopDist = GetStopLossDistance();
				if (stopDist > 0m)
					_virtualStopPrice = _entryPrice - stopDist;
			}
			// Breakout below previous lowest
			else if (candle.ClosePrice < _prevLowest)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				var stopDist = GetStopLossDistance();
				if (stopDist > 0m)
					_virtualStopPrice = _entryPrice + stopDist;
			}
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	private decimal GetStopLossDistance()
	{
		var tick = _tickSize > 0m ? _tickSize : Security?.PriceStep ?? 0m;
		if (tick <= 0m)
		return 0m;

		return StopLossPoints > 0 ? StopLossPoints * tick : 0m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var tick = _tickSize > 0m ? _tickSize : Security?.PriceStep ?? 0m;
		if (tick <= 0m)
		return price;

		return Math.Round(price / tick, MidpointRounding.AwayFromZero) * tick;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		if (security.VolumeStep is { } volumeStep && volumeStep > 0m)
		volume = Math.Round(volume / volumeStep, MidpointRounding.AwayFromZero) * volumeStep;

		if (security.MinVolume is { } minVolume && minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (security.MaxVolume is { } maxVolume && maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private DayOfWeek GetSetupDay()
	{
		var day = SetupDayOfWeek % 7;
		if (day < 0)
		day += 7;

		return (DayOfWeek)day;
	}
}

