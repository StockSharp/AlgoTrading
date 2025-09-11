using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CE XAU/USDT Strategy based on price crossing its SMA.
/// Buys when price crosses above the SMA and sells when price crosses below.
/// </summary>
public class CE_XAU_USDTStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma;
	private decimal _prevClose;
	private decimal _prevSma;
	private bool _initialized;

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CE_XAU_USDTStrategy"/>.
	/// </summary>
	public CE_XAU_USDTStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("SMA Period", "Period for SMA", "Parameters")
		.SetCanOptimize(true);

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
		_prevClose = 0m;
		_prevSma = 0m;
		_initialized = false;
		_sma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SMA { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_sma.IsFormed)
		return;

		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevSma = smaValue;
			_initialized = true;
			return;
		}

		var crossUp = _prevClose <= _prevSma && candle.ClosePrice > smaValue;
		var crossDown = _prevClose >= _prevSma && candle.ClosePrice < smaValue;

		if (crossUp && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		BuyMarket();

		if (crossDown && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		SellMarket();

		_prevClose = candle.ClosePrice;
		_prevSma = smaValue;
	}
}

