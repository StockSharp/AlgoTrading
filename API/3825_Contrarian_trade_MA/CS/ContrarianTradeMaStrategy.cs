using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weekly contrarian strategy translated from the MQL "Contrarian_trade_MA" expert advisor.
/// Combines weekly extremes and a moving average filter to trade against recent moves.
/// </summary>
public class ContrarianTradeMaStrategy : Strategy
{
	private readonly StrategyParam<int> _calcPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private SMA _sma = null!;

	private decimal? _previousClose;
	private decimal? _previousSma;
	private DateTimeOffset? _entryTime;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal _pipSize;

	/// <summary>
	/// Lookback period used to detect weekly extremes.
	/// </summary>
	public int CalcPeriod
	{
		get => _calcPeriod.Value;
		set => _calcPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period applied to weekly closes.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type for weekly calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ContrarianTradeMaStrategy()
	{
		_calcPeriod = Param(nameof(CalcPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Calculation Period", "Number of completed weeks in the extreme calculation", "General")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the simple moving average", "General")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 300m)
		.SetDisplay("Stop Loss (points)", "Distance from the entry price to the protective stop", "Risk")
		.SetRange(0m, decimal.MaxValue)
		.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 0.5m)
		.SetDisplay("Volume", "Trade volume expressed in lots", "General")
		.SetRange(0.01m, decimal.MaxValue)
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(7).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the weekly logic", "General");
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

		_previousClose = null;
		_previousSma = null;
		_entryTime = null;
		_entryPrice = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest
		{
			Length = CalcPeriod,
			CandlePrice = CandlePrice.High
		};

		_lowest = new Lowest
		{
			Length = CalcPeriod,
			CandlePrice = CandlePrice.Low
		};

		_sma = new SMA
		{
			Length = MaPeriod
		};

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		_pipSize = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highest, _lowest, _sma, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_highest.IsFormed || !_lowest.IsFormed || !_sma.IsFormed)
		{
			_previousClose = candle.ClosePrice;
			_previousSma = smaValue;
			return;
		}

		if (_previousClose is not decimal prevClose || _previousSma is not decimal prevSma)
		{
			_previousClose = candle.ClosePrice;
			_previousSma = smaValue;
			return;
		}

		if (Position == 0m)
		{
			var volume = Volume;
			if (volume > 0m)
			{
				if (highest < prevClose && TryEnter(true, volume, candle))
				goto SaveState;

				if (lowest > prevClose && TryEnter(false, volume, candle))
				goto SaveState;

				if (prevSma > candle.OpenPrice && TryEnter(true, volume, candle))
				goto SaveState;

				if (prevSma < candle.OpenPrice && TryEnter(false, volume, candle))
				goto SaveState;
			}
		}
		else
		{
			ManagePosition(candle);
		}

		SaveState:
		_previousClose = candle.ClosePrice;
		_previousSma = smaValue;
	}

	private bool TryEnter(bool isLong, decimal volume, ICandleMessage candle)
	{
		if (volume <= 0m)
		return false;

		if (isLong)
		BuyMarket(volume);
		else
		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_entryPrice = entryPrice;
		_entryTime = candle.CloseTime;
		_stopPrice = CalculateStopPrice(isLong, entryPrice);

		return true;
	}

	private decimal? CalculateStopPrice(bool isLong, decimal entryPrice)
	{
		var points = StopLossPoints;
		if (points <= 0m)
		return null;

		var distance = points * _pipSize;
		if (distance <= 0m)
		return null;

		return isLong ? entryPrice - distance : entryPrice + distance;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (_entryTime is DateTimeOffset entryTime)
		{
			var lifetime = candle.CloseTime - entryTime;
			if (lifetime >= TimeSpan.FromDays(7))
			{
				ExitPosition(volume);
				return;
			}
		}

		if (_stopPrice is decimal stopPrice)
		{
			if (Position > 0m)
			{
				if (candle.LowPrice <= stopPrice)
				{
					ExitPosition(volume);
					return;
				}
			}
			else if (Position < 0m)
			{
				if (candle.HighPrice >= stopPrice)
				{
					ExitPosition(volume);
					return;
				}
			}
		}
	}

	private void ExitPosition(decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position > 0m)
		SellMarket(volume);
		else if (Position < 0m)
		BuyMarket(volume);

		ClearEntryState();
	}

	private void ClearEntryState()
	{
		_entryTime = null;
		_entryPrice = null;
		_stopPrice = null;
	}
}
