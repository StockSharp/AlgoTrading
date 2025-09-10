using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buy Dip Multiple Positions strategy.
/// Opens long entries on strong dips with volume confirmation and manages a shared trailing stop.
/// </summary>
public class BuyDipMultiplePositionsStrategy : Strategy
{
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _trailRatePercent;
	private readonly StrategyParam<decimal> _initialStopPercent;
	private readonly StrategyParam<decimal> _targetPricePercent;
	private readonly StrategyParam<decimal> _priceSurgePercent;
	private readonly StrategyParam<int> _surgeLookbackBars;
	private readonly StrategyParam<DataType> _candleType;

	private RollingWindow<decimal> _closeWindow;
	private readonly RollingWindow<bool> _revWindow = new(3);
	private readonly List<(decimal price, decimal size)> _entries = [];

	private decimal _prevLow;
	private decimal _prevVol1;
	private decimal _prevVol2;
	private decimal _initialStop;
	private decimal _targetPrice;
	private decimal _trailStop;
	private int _trailBars;
	private bool _lastTradeInProfit = true;
	private decimal _winningTrades;
	private decimal _losingTrades;

	/// <summary>
	/// Maximum simultaneous trades.
	/// </summary>
	public int MaxPositions { get => _maxPositions.Value; set => _maxPositions.Value = value; }

	/// <summary>
	/// Trailing stop increase per bar (%).
	/// </summary>
	public decimal TrailRatePercent { get => _trailRatePercent.Value; set => _trailRatePercent.Value = value; }

	/// <summary>
	/// Initial stop percent of entry low.
	/// </summary>
	public decimal InitialStopPercent { get => _initialStopPercent.Value; set => _initialStopPercent.Value = value; }

	/// <summary>
	/// Target price percent above entry low.
	/// </summary>
	public decimal TargetPricePercent { get => _targetPricePercent.Value; set => _targetPricePercent.Value = value; }

	/// <summary>
	/// Price surge threshold percent.
	/// </summary>
	public decimal PriceSurgePercent { get => _priceSurgePercent.Value; set => _priceSurgePercent.Value = value; }

	/// <summary>
	/// Bars lookback for surge threshold.
	/// </summary>
	public int SurgeLookbackBars { get => _surgeLookbackBars.Value; set => _surgeLookbackBars.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BuyDipMultiplePositionsStrategy"/> class.
	/// </summary>
	public BuyDipMultiplePositionsStrategy()
	{
		_maxPositions = Param(nameof(MaxPositions), 20)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum simultaneous trades", "General");

		_trailRatePercent = Param(nameof(TrailRatePercent), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trail Rate %", "Trailing stop increase per bar", "Risk");

		_initialStopPercent = Param(nameof(InitialStopPercent), 85m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Stop %", "Initial stop percent of low", "Risk");

		_targetPricePercent = Param(nameof(TargetPricePercent), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Target Price %", "Target percent above low", "Risk");

		_priceSurgePercent = Param(nameof(PriceSurgePercent), 89m)
			.SetGreaterThanZero()
			.SetDisplay("Surge Percent %", "Price surge threshold percent", "General");

		_surgeLookbackBars = Param(nameof(SurgeLookbackBars), 14)
			.SetGreaterThanZero()
			.SetDisplay("Surge Lookback", "Bars lookback for surge", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_closeWindow = new RollingWindow<decimal>(_surgeLookbackBars.Value + 1);
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
		_closeWindow = new RollingWindow<decimal>(SurgeLookbackBars + 1);
		_revWindow.Clear();
		_entries.Clear();
		_prevLow = 0m;
		_prevVol1 = 0m;
		_prevVol2 = 0m;
		_initialStop = 0m;
		_targetPrice = 0m;
		_trailStop = 0m;
		_trailBars = 0;
		_lastTradeInProfit = true;
		_winningTrades = 0m;
		_losingTrades = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		_closeWindow.Add(candle.ClosePrice);

		var isReversal = false;

		if (_closeWindow.IsFull() && _prevVol2 > 0m && _prevLow > 0m)
		{
			var avgPrevVol = (_prevVol1 + _prevVol2) / 2m;
			var isVolumeConfirm = candle.TotalVolume > avgPrevVol * 1.2m;
			var isPriceReversal = candle.ClosePrice < _prevLow * 0.998m;
			var surgeThreshold = _closeWindow[0] * (PriceSurgePercent / 100m);
			var isPriceSurge = candle.ClosePrice < surgeThreshold;
			isReversal = isPriceReversal && isVolumeConfirm && isPriceSurge;

			if (isReversal && _entries.Count < MaxPositions && (_entries.Count == 0 || _lastTradeInProfit))
			{
				var entryPrice = candle.LowPrice;
				var initialStop = candle.LowPrice * (InitialStopPercent / 100m);
				var targetPrice = candle.LowPrice * (1m + TargetPricePercent / 100m);
				var risk = entryPrice - initialStop;
				var capital = Portfolio.CurrentValue ?? 0m;
				var maxLoss = capital * 0.02m;
				var size = risk > 0m ? maxLoss / risk : 0m;

				if (size > 0m)
				{
					BuyLimit(size, entryPrice);
					_entries.Add((entryPrice, size));
					_initialStop = initialStop;
					_targetPrice = targetPrice;
				}
			}
		}

		_revWindow.Add(isReversal);

		if (_entries.Count > 0 && _revWindow.IsFull())
		{
			var trailReady = _revWindow[0];
			var exitReady = _revWindow[1];
			var trailRate = TrailRatePercent / 100m;

			if (trailReady)
			{
				_trailBars++;
				_trailStop = _initialStop * (1m + trailRate * _trailBars);
			}
			else
			{
				_trailBars = 0;
				_trailStop = 0m;
			}

			var stop = _initialStop;
			if (trailReady && _trailStop > 0m)
				stop = _trailStop;

			var exitPrice = candle.ClosePrice;

			if ((exitReady && !trailReady && candle.LowPrice <= stop) ||
				(trailReady && _trailStop > 0m && candle.LowPrice <= stop) ||
				candle.HighPrice >= _targetPrice)
			{
				var volume = 0m;
				var pnl = 0m;

				foreach (var (price, size) in _entries)
				{
					volume += size;
					pnl += (exitPrice - price) * size;
				}

				SellMarket(volume);

				if (pnl > 0m)
				{
					_winningTrades++;
					_lastTradeInProfit = true;
				}
				else
				{
					_losingTrades++;
					_lastTradeInProfit = false;
				}

				_entries.Clear();
				_trailBars = 0;
				_trailStop = 0m;
			}
		}

		_prevVol2 = _prevVol1;
		_prevVol1 = candle.TotalVolume;
		_prevLow = candle.LowPrice;
	}

	#region RollingWindow
	private class RollingWindow<T>
	{
		private readonly Queue<T> _queue = [];
		private readonly int _size;

		public RollingWindow(int size)
		{
			_size = size;
		}

		public void Add(T value)
		{
			if (_queue.Count == _size)
				_queue.Dequeue();
			_queue.Enqueue(value);
		}

		public bool IsFull()
		{
			return _queue.Count == _size;
		}

		public T this[int index]
		{
			get => _queue.ElementAt(index);
		}

		public void Clear()
		{
			_queue.Clear();
		}
	}
	#endregion
}
