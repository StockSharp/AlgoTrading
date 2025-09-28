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

public class VivaLasVegasStrategy : Strategy
{
	private readonly StrategyParam<int> _stopTakePips;
	private readonly StrategyParam<decimal> _baseVolume;
private readonly StrategyParam<MoneyManagementModes> _moneyManagementMode;
	private readonly StrategyParam<int> _seed;

	private Random _random = new();
	private IMoneyManagement _management;
	private decimal _previousPosition;
	private decimal _lastRealizedPnL;
	private bool _orderInFlight;

	public enum MoneyManagementModes
	{
		Martingale,
		NegativePyramid,
		Labouchere,
		OscarsGrind,
		System31,
	}

	public VivaLasVegasStrategy()
	{
		_stopTakePips = Param(nameof(StopTakePips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop/take distance", "Protective distance expressed in pips for both stop-loss and take-profit.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base volume", "Initial lot size used as the anchor for all money management progressions.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

_moneyManagementMode = Param(nameof(MoneyManagement), MoneyManagementModes.Martingale)
			.SetDisplay("Money management", "Progression model that decides the next order volume.", "General");

		_seed = Param(nameof(Seed), 0)
			.SetDisplay("Random seed", "Seed for the pseudo-random trade direction. Zero switches to time-based seeding.", "General");
	}

	public int StopTakePips
	{
		get => _stopTakePips.Value;
		set => _stopTakePips.Value = value;
	}

	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

public MoneyManagementModes MoneyManagement
{
get => _moneyManagementMode.Value;
set
{
_moneyManagementMode.Value = value;
InitializeMoneyManagement();
}
}

	public int Seed
	{
		get => _seed.Value;
		set => _seed.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
		_orderInFlight = false;
		InitializeMoneyManagement();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		InitializeMoneyManagement();

		_random = Seed == 0 ? new Random() : new Random(Seed);

		var steps = StopTakePips * GetPipMultiplier();
		if (StopTakePips > 0 && steps > 0m)
		{
			var unit = new Unit(steps, UnitTypes.Step);
			StartProtection(unit, unit);
		}
		else
		{
			StartProtection();
		}

		TryOpenPosition();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0m)
		{
			// A fill confirmed our open position, so further market orders must wait.
			_orderInFlight = false;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (_previousPosition == 0m && Position != 0m)
		{
			// A new position was just established; capture the realized PnL baseline.
			_lastRealizedPnL = PnL;
			_orderInFlight = false;
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;

			var closedVolume = Math.Abs(_previousPosition);
			if (closedVolume > 0m && _management != null)
			{
				var result = tradePnL > 0m ? TradeResults.Win : TradeResults.Loss;
				_management.Update(result, closedVolume, BaseVolume);
			}

			// The slot is free again; schedule the next random wager.
			_orderInFlight = false;
			TryOpenPosition();
		}

		_previousPosition = Position;
	}

	private void TryOpenPosition()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m || _orderInFlight)
			return;

		var volume = _management?.GetVolume(BaseVolume) ?? BaseVolume;
		volume = AdjustVolume(volume);

		if (volume <= 0m)
			return;

		var isBuy = _random.NextDouble() > 0.5;

		_orderInFlight = true;

		if (isBuy)
		{
			// Coin toss favoured the bullish side.
			BuyMarket(volume);
		}
		else
		{
			// Bearish outcome - sell into the market.
			SellMarket(volume);
		}
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal GetPipMultiplier()
	{
		var security = Security;
		if (security == null)
			return 1m;

		return security.Decimals is 3 or 5 ? 10m : 1m;
	}

	private void InitializeMoneyManagement()
	{
		_management = CreateMoneyManagement(MoneyManagement);
		_management.Reset(BaseVolume);
	}

private IMoneyManagement CreateMoneyManagement(MoneyManagementModes mode)
{
return mode switch
{
MoneyManagementModes.Martingale => new MartingaleManagement(),
MoneyManagementModes.NegativePyramid => new NegativePyramidManagement(),
MoneyManagementModes.Labouchere => new LabouchereManagement(),
MoneyManagementModes.OscarsGrind => new OscarsGrindManagement(),
MoneyManagementModes.System31 => new System31Management(),
_ => new MartingaleManagement(),
};
}

	private enum TradeResults
	{
		Win,
		Loss,
	}

	private interface IMoneyManagement
	{
		decimal GetVolume(decimal baseVolume);
		void Update(TradeResults result, decimal closedVolume, decimal baseVolume);
		void Reset(decimal baseVolume);
	}

	private sealed class MartingaleManagement : IMoneyManagement
	{
		private decimal _nextVolume;

		public decimal GetVolume(decimal baseVolume)
		{
			if (_nextVolume <= 0m)
				_nextVolume = baseVolume;

			return _nextVolume;
		}

		public void Update(TradeResults result, decimal closedVolume, decimal baseVolume)
		{
			_nextVolume = result == TradeResults.Win ? baseVolume : _nextVolume * 2m;
		}

		public void Reset(decimal baseVolume)
		{
			_nextVolume = 0m;
		}
	}

	private sealed class NegativePyramidManagement : IMoneyManagement
	{
		private decimal _nextVolume;

		public decimal GetVolume(decimal baseVolume)
		{
			if (_nextVolume <= 0m)
				_nextVolume = baseVolume;

			return _nextVolume;
		}

		public void Update(TradeResults result, decimal closedVolume, decimal baseVolume)
		{
			if (result == TradeResults.Win)
			{
				_nextVolume /= 2m;
				if (_nextVolume < baseVolume)
					_nextVolume = baseVolume;
			}
			else
			{
				_nextVolume *= 2m;
			}
		}

		public void Reset(decimal baseVolume)
		{
			_nextVolume = 0m;
		}
	}

	private sealed class LabouchereManagement : IMoneyManagement
	{
		private static readonly int[] _baseSeries = { 1, 2, 3 };
		private readonly List<decimal> _series = new();

		public decimal GetVolume(decimal baseVolume)
		{
			if (_series.Count == 0)
				Reset(baseVolume);

			if (_series.Count > 1)
				return (_series[0] + _series[^1]) * baseVolume;

			return _series[0] * baseVolume;
		}

		public void Update(TradeResults result, decimal closedVolume, decimal baseVolume)
		{
			if (_series.Count == 0)
				Reset(baseVolume);

			if (result == TradeResults.Win)
			{
				if (_series.Count > 2)
				{
					_series.RemoveAt(_series.Count - 1);
					_series.RemoveAt(0);
				}
				else
				{
					Reset(baseVolume);
				}
			}
			else
			{
				var first = _series[0];
				var last = _series[^1];
				_series.Add(first + last);
			}
		}

		public void Reset(decimal baseVolume)
		{
			_series.Clear();

			foreach (var value in _baseSeries)
				_series.Add(value);
		}
	}

	private sealed class OscarsGrindManagement : IMoneyManagement
	{
		private decimal _nextVolume;
		private decimal _currentResult;

		public decimal GetVolume(decimal baseVolume)
		{
			if (_nextVolume <= 0m)
				_nextVolume = baseVolume;

			return _nextVolume;
		}

		public void Update(TradeResults result, decimal closedVolume, decimal baseVolume)
		{
			if (result == TradeResults.Win)
			{
				_currentResult += closedVolume;

				if (_currentResult >= baseVolume)
				{
					_nextVolume = baseVolume;
					_currentResult = 0m;
					return;
				}

				_nextVolume += baseVolume;
				var cap = baseVolume + Math.Abs(_currentResult);
				if (_nextVolume > cap)
					_nextVolume = cap;
			}
			else
			{
				_currentResult -= closedVolume;
			}
		}

		public void Reset(decimal baseVolume)
		{
			_nextVolume = 0m;
			_currentResult = 0m;
		}
	}

	private sealed class System31Management : IMoneyManagement
	{
		private static readonly int[] _series = { 1, 1, 1, 2, 2, 4, 4, 8, 8 };
		private int _index;
		private bool _doubleUp;

		public decimal GetVolume(decimal baseVolume)
		{
			var multiplier = _series[_index];
			if (_doubleUp)
				multiplier *= 2;

			return multiplier * baseVolume;
		}

		public void Update(TradeResults result, decimal closedVolume, decimal baseVolume)
		{
			if (result == TradeResults.Win)
			{
				if (!_doubleUp)
				{
					_doubleUp = true;
				}
				else
				{
					_doubleUp = false;
					_index = 0;
				}
			}
			else
			{
				if (!_doubleUp)
				{
					_index = (_index + 1) % _series.Length;
				}
				else
				{
					_doubleUp = false;
				}
			}
		}

		public void Reset(decimal baseVolume)
		{
			_index = 0;
			_doubleUp = false;
		}
	}
}
