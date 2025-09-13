using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weekly contrarian strategy using moving average and extreme price levels.
/// </summary>
public class ContrarianTradeMaStrategy : Strategy
{
	private readonly StrategyParam<int> _calcPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private SMA _sma = null!;

	private decimal _prevClose;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Lookback period for high/low calculations.
	/// </summary>
	public int CalcPeriod { get => _calcPeriod.Value; set => _calcPeriod.Value = value; }

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ContrarianTradeMaStrategy()
	{
		_calcPeriod = Param(nameof(CalcPeriod), 4)
			.SetDisplay("Calc Period", "Lookback period for extremes", "General");

		_maPeriod = Param(nameof(MaPeriod), 7)
			.SetDisplay("MA Period", "Moving average period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = CalcPeriod, CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = CalcPeriod, CandlePrice = CandlePrice.Low };
		_sma = new SMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, _sma, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed || !_sma.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (Position == 0)
		{
			// Check breakout conditions against previous close
			if (highest < _prevClose)
			{
				BuyMarket();
				_entryTime = candle.CloseTime;
			}
			else if (lowest > _prevClose)
			{
				SellMarket();
				_entryTime = candle.CloseTime;
			}
			else if (sma > candle.OpenPrice)
			{
				BuyMarket();
				_entryTime = candle.CloseTime;
			}
			else if (sma < candle.OpenPrice)
			{
				SellMarket();
				_entryTime = candle.CloseTime;
			}
		}
		else
		{
			// Close position after one week
			if (_entryTime != null && candle.CloseTime - _entryTime.Value >= TimeSpan.FromDays(7))
			{
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();

				_entryTime = null;
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
