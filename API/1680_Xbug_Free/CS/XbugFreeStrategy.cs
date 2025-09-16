using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Counter-trend strategy entering after price crosses its moving average.
/// </summary>
public class XbugFreeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _stopPoints;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private decimal? _prevMa;
	private decimal? _prevPrice;
	private decimal? _prevPrevMa;
	private decimal? _prevPrevPrice;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Shift applied to the moving average.
	/// Currently not used due to indicator limitations.
	/// </summary>
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }

	/// <summary>
	/// Stop-loss and take-profit distance in points.
	/// </summary>
	public int StopPoints { get => _stopPoints.Value; set => _stopPoints.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="XbugFreeStrategy"/>.
	/// </summary>
	public XbugFreeStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 19)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of moving average", "Parameters")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 15)
			.SetDisplay("MA Shift", "Horizontal shift of moving average", "Parameters");

		_stopPoints = Param(nameof(StopPoints), 270)
			.SetGreaterThanZero()
			.SetDisplay("Stop Points", "Distance for stop-loss and take-profit in points", "Risk")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		_sma = default!;
		_prevMa = _prevPrice = _prevPrevMa = _prevPrevPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(c => (c.HighPrice + c.LowPrice) / 2m, _sma, ProcessCandle).Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(new Unit(StopPoints * step, UnitTypes.Absolute),
			new Unit(StopPoints * step, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		if (_prevMa.HasValue && _prevPrice.HasValue &&
			_prevPrevMa.HasValue && _prevPrevPrice.HasValue)
		{
			var buySignal = maValue > price && _prevMa > _prevPrice && _prevPrevMa < _prevPrevPrice;
			var sellSignal = maValue < price && _prevMa < _prevPrice && _prevPrevMa > _prevPrevPrice;

			if (buySignal && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (sellSignal && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		_prevPrevMa = _prevMa;
		_prevPrevPrice = _prevPrice;
		_prevMa = maValue;
		_prevPrice = price;
	}
}
