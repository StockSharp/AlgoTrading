using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend strategy by Nitin.
/// </summary>
public class SupertrendNitinStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isAbove;
	private bool _hasPrev;
	private decimal _prevSupertrend;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public SupertrendNitinStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "ATR multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_isAbove = false;
		_hasPrev = false;
		_prevSupertrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper = median + Multiplier * atrValue;
		var lower = median - Multiplier * atrValue;

		if (!_hasPrev)
		{
			_prevSupertrend = candle.ClosePrice > median ? lower : upper;
			_isAbove = candle.ClosePrice > _prevSupertrend;
			_hasPrev = true;
			return;
		}

		var supertrend = _isAbove ? Math.Max(lower, _prevSupertrend) : Math.Min(upper, _prevSupertrend);
		var isAbove = candle.ClosePrice > supertrend;
		var crossedUp = isAbove && !_isAbove;
		var crossedDown = !isAbove && _isAbove;

		if (crossedUp && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (crossedDown && Position > 0)
		{
			SellMarket(Position);
		}

		_prevSupertrend = supertrend;
		_isAbove = isAbove;
	}
}
