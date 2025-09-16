using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades reversals after a series of candles with growing bodies.
/// Buys after <see cref="BarsCount"/> bearish candles with increasing body size
/// and sells after the same pattern of bullish candles.
/// Optional stop loss and take profit are defined in points.
/// </summary>
public class ETurboFxStrategy : Strategy
{
	private readonly StrategyParam<int> _barsCount;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private int _bearCount;
	private int _bullCount;
	private int _bearSizeCount;
	private int _bullSizeCount;
	private decimal? _prevBearBody;
	private decimal? _prevBullBody;

	/// <summary>
	/// Number of consecutive candles required for entry.
	/// </summary>
	public int BarsCount
	{
		get => _barsCount.Value;
		set => _barsCount.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ETurboFxStrategy"/>.
	/// </summary>
	public ETurboFxStrategy()
	{
		_barsCount = Param(nameof(BarsCount), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bars Count", "Number of consecutive candles", "Parameters")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 700)
			.SetDisplay("Stop Loss (points)", "Stop loss in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1200)
			.SetDisplay("Take Profit (points)", "Take profit in points", "Risk")
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

		_bearCount = 0;
		_bullCount = 0;
		_bearSizeCount = 0;
		_bullSizeCount = 0;
		_prevBearBody = null;
		_prevBullBody = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null,
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : null);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (candle.ClosePrice < candle.OpenPrice)
		{
			_bearCount++;
			_bearSizeCount = _prevBearBody != null && body > _prevBearBody ? _bearSizeCount + 1 : 0;
			_prevBearBody = body;

			_bullCount = 0;
			_bullSizeCount = 0;
			_prevBullBody = null;
		}
		else if (candle.ClosePrice > candle.OpenPrice)
		{
			_bullCount++;
			_bullSizeCount = _prevBullBody != null && body > _prevBullBody ? _bullSizeCount + 1 : 0;
			_prevBullBody = body;

			_bearCount = 0;
			_bearSizeCount = 0;
			_prevBearBody = null;
		}
		else
		{
			_bearCount = 0;
			_bullCount = 0;
			_bearSizeCount = 0;
			_bullSizeCount = 0;
			_prevBearBody = null;
			_prevBullBody = null;
		}

		if (_bearCount >= BarsCount && _bearSizeCount >= BarsCount - 1 && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
		}
		else if (_bullCount >= BarsCount && _bullSizeCount >= BarsCount - 1 && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}
	}
}
