using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy based on the Modified Optimum Elliptic Filter indicator.
/// </summary>
public class ModifiedOptimumEllipticFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly ModifiedOptimumEllipticFilter _filter = new();

	private decimal _prevFilter1;
	private decimal _prevFilter2;
	private bool _isInitialized;
	private int _barsSinceTrade;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ModifiedOptimumEllipticFilterStrategy"/>.
	/// </summary>
	public ModifiedOptimumEllipticFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");

		_cooldownBars = Param(nameof(CooldownBars), 1)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");
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

		_filter.Reset();
		_prevFilter1 = 0m;
		_prevFilter2 = 0m;
		_isInitialized = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_filter, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _filter);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || !_filter.IsFormed)
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		if (!_isInitialized)
		{
			_prevFilter2 = filterValue;
			_prevFilter1 = filterValue;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevFilter1 <= _prevFilter2 && filterValue > _prevFilter1;
		var crossDown = _prevFilter1 >= _prevFilter2 && filterValue < _prevFilter1;

		if (_barsSinceTrade >= CooldownBars)
		{
			if (crossUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_prevFilter2 = _prevFilter1;
		_prevFilter1 = filterValue;
	}

	private class ModifiedOptimumEllipticFilter : BaseIndicator
	{
		private decimal _price0;
		private decimal _price1;
		private decimal _price2;
		private decimal _price3;
		private decimal _filter0;
		private decimal _filter1;
		private int _priceCount;
		private int _filterCount;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (candle == null)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var price = (candle.HighPrice + candle.LowPrice) / 2m;
			_price3 = _price2;
			_price2 = _price1;
			_price1 = _price0;
			_price0 = price;
			_priceCount = Math.Min(_priceCount + 1, 4);

			decimal value;

			if (_priceCount < 4 || _filterCount < 2)
			{
				value = price;
				IsFormed = false;
			}
			else
			{
				value = 0.13785m * (2m * _price0 - _price1)
					+ 0.0007m * (2m * _price1 - _price2)
					+ 0.13785m * (2m * _price2 - _price3)
					+ 1.2103m * _filter0
					- 0.4867m * _filter1;
				IsFormed = true;
			}

			_filter1 = _filter0;
			_filter0 = value;
			_filterCount = Math.Min(_filterCount + 1, 2);

			return new DecimalIndicatorValue(this, value, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_price0 = 0m;
			_price1 = 0m;
			_price2 = 0m;
			_price3 = 0m;
			_filter0 = 0m;
			_filter1 = 0m;
			_priceCount = 0;
			_filterCount = 0;
		}
	}
}
