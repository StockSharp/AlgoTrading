using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaCrossoverTpSl5EmaFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _targetPercent;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal TargetPercent { get => _targetPercent.Value; set => _targetPercent.Value = value; }
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaCrossoverTpSl5EmaFilterStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_emaLength = Param(nameof(EmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of the EMA filter", "Filter Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_targetPercent = Param(nameof(TargetPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Target %", "Take profit percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_stopPercent = Param(nameof(StopPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_isLong = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };
		var ema = new EMA { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);

		var prevFast = 0m;
		var prevSlow = 0m;
		var wasFastLess = false;
		var initialized = false;

		subscription
			.Bind(fastMa, slowMa, ema, (candle, fast, slow, emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!initialized && fastMa.IsFormed && slowMa.IsFormed && ema.IsFormed)
				{
					prevFast = fast;
					prevSlow = slow;
					wasFastLess = fast < slow;
					initialized = true;
					return;
				}

				if (!initialized)
					return;

				var isFastLess = fast < slow;

				if (wasFastLess != isFastLess)
				{
					if (!isFastLess && Position <= 0 && candle.ClosePrice > emaValue)
					{
						_entryPrice = candle.ClosePrice;
						_isLong = true;
						BuyMarket(Volume + Math.Abs(Position));
					}
					else if (isFastLess && Position >= 0 && candle.ClosePrice < emaValue)
					{
						_entryPrice = candle.ClosePrice;
						_isLong = false;
						SellMarket(Volume + Math.Abs(Position));
					}

					wasFastLess = isFastLess;
				}

				if (Position > 0 && _isLong && _entryPrice > 0)
				{
					var tp = _entryPrice * (1 + TargetPercent / 100m);
					var sl = _entryPrice * (1 - StopPercent / 100m);

					if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
						SellMarket(Math.Abs(Position));
				}
				else if (Position < 0 && !_isLong && _entryPrice > 0)
				{
					var tp = _entryPrice * (1 - TargetPercent / 100m);
					var sl = _entryPrice * (1 + StopPercent / 100m);

					if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
						BuyMarket(Math.Abs(Position));
				}

				prevFast = fast;
				prevSlow = slow;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
