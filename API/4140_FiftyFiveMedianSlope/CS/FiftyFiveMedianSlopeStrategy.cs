namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Median moving average slope follower converted from the MQL4 expert "55_MA_med_FIN".
/// Opens trades when the 55-period median moving average rises or falls between two displaced snapshots.
/// </summary>
public class FiftyFiveMedianSlopeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageKinds> _maMethod;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma;
	private decimal?[] _maBuffer = Array.Empty<decimal?>();
	private bool _allowBuy = true;
	private bool _allowSell = true;
	private decimal _alignedBaseVolume;

	public FiftyFiveMedianSlopeStrategy()
	{
		_fixedVolume = Param(nameof(FixedVolume), 1m)
			.SetDisplay("Fixed volume", "Lot size used when greater than zero.", "Trading")
			.SetNotNegative();

		_riskPercentage = Param(nameof(RiskPercentage), 0m)
			.SetDisplay("Risk percentage", "Risk-based position sizing when fixed volume equals zero.", "Risk")
			.SetRange(0m, 100m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetDisplay("Take profit (points)", "Distance of the optional take-profit in price steps.", "Risk")
			.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetDisplay("Stop loss (points)", "Distance of the optional protective stop in price steps.", "Risk")
			.SetNotNegative();

		_maPeriod = Param(nameof(MaPeriod), 55)
			.SetDisplay("MA period", "Length of the median moving average.", "Indicators")
			.SetGreaterThanZero();

		_maShift = Param(nameof(MaShift), 13)
			.SetDisplay("MA shift", "Bars between the recent and historical moving average snapshots.", "Indicators")
			.SetNotNegative();

		_maMethod = Param(nameof(MaMethod), MovingAverageKinds.Exponential)
			.SetDisplay("MA method", "Calculation method used for the moving average.", "Indicators");

		_startHour = Param(nameof(StartHour), 8)
			.SetDisplay("Start hour", "Trading window lower bound expressed in hours (exclusive).", "Trading")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 20)
			.SetDisplay("End hour", "Trading window upper bound expressed in hours (exclusive).", "Trading")
			.SetRange(0, 23);

		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetDisplay("Max orders", "Maximum number of simultaneous positions per direction (0 = unlimited).", "Trading")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signal evaluation.", "Data");
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	public MovingAverageKinds MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_ma = null;
		_maBuffer = Array.Empty<decimal?>();
		_allowBuy = true;
		_allowSell = true;
		_alignedBaseVolume = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMovingAverage(MaMethod, MaPeriod);

		var bufferLength = Math.Max(MaShift, 1) + 2;
		_maBuffer = new decimal?[bufferLength];

		_alignedBaseVolume = AlignVolume(FixedVolume);
		if (_alignedBaseVolume > 0m)
		{
			Volume = _alignedBaseVolume;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_ma != null)
			{
				DrawIndicator(area, _ma);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (_ma is null)
		{
			return;
		}

		var isFinal = candle.State == CandleStates.Finished;
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var maValue = _ma.Process(medianPrice, candle.OpenTime, isFinal);

		if (!isFinal || !maValue.IsFinal)
		{
			return;
		}

		if (!_ma.IsFormed)
		{
			return;
		}

		var maDecimal = maValue.ToDecimal();
		PushValue(_maBuffer, maDecimal);

		if (!TryGetBufferValue(_maBuffer, 1, out var recent))
		{
			return;
		}

		var shiftIndex = Math.Max(MaShift, 1);
		if (!TryGetBufferValue(_maBuffer, shiftIndex, out var shifted))
		{
			return;
		}

		if (!IsWithinTradingHours(candle.OpenTime))
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (recent > shifted && _allowBuy)
		{
			TryEnterLong(candle);
			_allowBuy = false;
			_allowSell = true;
		}
		else if (recent < shifted && _allowSell)
		{
			TryEnterShort(candle);
			_allowSell = false;
			_allowBuy = true;
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (!CanAddLong())
		{
			return;
		}

		var volume = DetermineOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}

		if (!CanAddShort())
		{
			return;
		}

		var volume = DetermineOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);
	}

	private bool CanAddLong()
	{
		if (MaxOrders <= 0)
		{
			return true;
		}

		if (_alignedBaseVolume <= 0m)
		{
			return Position <= 0m;
		}

		var longVolume = Position > 0m ? Position : 0m;
		var orders = longVolume / _alignedBaseVolume;
		return orders < MaxOrders;
	}

	private bool CanAddShort()
	{
		if (MaxOrders <= 0)
		{
			return true;
		}

		if (_alignedBaseVolume <= 0m)
		{
			return Position >= 0m;
		}

		var shortVolume = Position < 0m ? Math.Abs(Position) : 0m;
		var orders = shortVolume / _alignedBaseVolume;
		return orders < MaxOrders;
	}

	private decimal DetermineOrderVolume(decimal price)
	{
		var fixedVolume = AlignVolume(FixedVolume);
		if (fixedVolume > 0m)
		{
			_alignedBaseVolume = fixedVolume;
			return fixedVolume;
		}

		if (RiskPercentage <= 0m)
		{
			return 0m;
		}

		if (price <= 0m)
		{
			return 0m;
		}

		var capital = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (capital <= 0m)
		{
			return 0m;
		}

		var riskCapital = capital * RiskPercentage / 100m;
		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
		{
			priceStep = 1m;
		}

		var estimatedVolume = riskCapital / (price * priceStep);
		return AlignVolume(estimatedVolume);
	}

	private decimal AlignVolume(decimal volume)
	{
		if (Security is null)
		{
			return volume;
		}

		var step = Security.VolumeStep ?? 0m;
		var min = Security.VolumeMin ?? 0m;
		var max = Security.VolumeMax ?? decimal.MaxValue;

		if (step > 0m)
		{
			var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (ratio == 0m && volume > 0m)
			{
				ratio = 1m;
			}

			volume = ratio * step;
		}

		if (min > 0m && volume < min)
		{
			volume = min;
		}

		if (volume > max)
		{
			volume = max;
		}

		return volume;
	}

	private static void PushValue(decimal?[] buffer, decimal value)
	{
		for (var i = buffer.Length - 1; i > 0; i--)
		{
			buffer[i] = buffer[i - 1];
		}

		buffer[0] = value;
	}

	private static bool TryGetBufferValue(decimal?[] buffer, int index, out decimal value)
	{
		value = 0m;

		if (index < 0 || index >= buffer.Length)
		{
			return false;
		}

		var stored = buffer[index];
		if (stored is null)
		{
			return false;
		}

		value = stored.Value;
		return true;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		var start = NormalizeHour(StartHour);
		var end = NormalizeHour(EndHour);

		if (start == end)
		{
			return true;
		}

		if (start < end)
		{
			return hour > start && hour < end;
		}

		return hour > start || hour < end;
	}

	private static int NormalizeHour(int hour)
	{
		if (hour < 0)
		{
			return 0;
		}

		if (hour > 23)
		{
			return 23;
		}

		return hour;
	}

	private static IIndicator CreateMovingAverage(MovingAverageKinds method, int length)
	{
		return method switch
		{
			MovingAverageKinds.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageKinds.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageKinds.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageKinds.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}
}

/// <summary>
/// Moving average calculation methods supported by the strategy.
/// </summary>
public enum MovingAverageKinds
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}
