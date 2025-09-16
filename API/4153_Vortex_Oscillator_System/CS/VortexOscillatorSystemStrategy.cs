using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex oscillator breakout system converted from the MetaTrader 4 expert.
/// Opens trades when the oscillator leaves the neutral band and optionally closes them on oscillator stops.
/// </summary>
public class VortexOscillatorSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _vortexLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<bool> _useBuyStopLoss;
	private readonly StrategyParam<decimal> _buyStopLossLevel;
	private readonly StrategyParam<bool> _useBuyTakeProfit;
	private readonly StrategyParam<decimal> _buyTakeProfitLevel;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<bool> _useSellStopLoss;
	private readonly StrategyParam<decimal> _sellStopLossLevel;
	private readonly StrategyParam<bool> _useSellTakeProfit;
	private readonly StrategyParam<decimal> _sellTakeProfitLevel;

	private VortexIndicator _vortex = null!;
	private bool _longSetup;
	private bool _shortSetup;

	/// <summary>
	/// Number of candles used by the Vortex indicator.
	/// </summary>
	public int VortexLength
	{
		get => _vortexLength.Value;
		set => _vortexLength.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the Vortex oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order volume used for new positions.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Oscillator level that enables long setups.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Use the long-side oscillator stop filter before opening new longs.
	/// </summary>
	public bool UseBuyStopLoss
	{
		get => _useBuyStopLoss.Value;
		set => _useBuyStopLoss.Value = value;
	}

	/// <summary>
	/// Oscillator value that liquidates long positions when the stop-loss filter is active.
	/// </summary>
	public decimal BuyStopLossLevel
	{
		get => _buyStopLossLevel.Value;
		set => _buyStopLossLevel.Value = value;
	}

	/// <summary>
	/// Use the oscillator based take-profit for long positions.
	/// </summary>
	public bool UseBuyTakeProfit
	{
		get => _useBuyTakeProfit.Value;
		set => _useBuyTakeProfit.Value = value;
	}

	/// <summary>
	/// Oscillator value that triggers the long take-profit when enabled.
	/// </summary>
	public decimal BuyTakeProfitLevel
	{
		get => _buyTakeProfitLevel.Value;
		set => _buyTakeProfitLevel.Value = value;
	}

	/// <summary>
	/// Oscillator level that enables short setups.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Use the short-side oscillator stop filter before opening new shorts.
	/// </summary>
	public bool UseSellStopLoss
	{
		get => _useSellStopLoss.Value;
		set => _useSellStopLoss.Value = value;
	}

	/// <summary>
	/// Oscillator value that liquidates short positions when the stop-loss filter is active.
	/// </summary>
	public decimal SellStopLossLevel
	{
		get => _sellStopLossLevel.Value;
		set => _sellStopLossLevel.Value = value;
	}

	/// <summary>
	/// Use the oscillator based take-profit for short positions.
	/// </summary>
	public bool UseSellTakeProfit
	{
		get => _useSellTakeProfit.Value;
		set => _useSellTakeProfit.Value = value;
	}

	/// <summary>
	/// Oscillator value that triggers the short take-profit when enabled.
	/// </summary>
	public decimal SellTakeProfitLevel
	{
		get => _sellTakeProfitLevel.Value;
		set => _sellTakeProfitLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="VortexOscillatorSystemStrategy"/>.
	/// </summary>
	public VortexOscillatorSystemStrategy()
	{
		_vortexLength = Param(nameof(VortexLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Number of candles used to compute the Vortex indicator.", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Vortex oscillator calculations.", "General");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume for entries.", "Trading")
			.SetCanOptimize(true);

		_buyThreshold = Param(nameof(BuyThreshold), -0.75m)
			.SetDisplay("Buy Threshold", "Oscillator value that enables long setups.", "Oscillator")
			.SetCanOptimize(true);

		_useBuyStopLoss = Param(nameof(UseBuyStopLoss), false)
			.SetDisplay("Use Buy Stop-Loss", "Require the oscillator to stay above the long stop level before opening longs.", "Risk");

		_buyStopLossLevel = Param(nameof(BuyStopLossLevel), -1m)
			.SetDisplay("Buy Stop-Loss Level", "Oscillator level that closes long positions when enabled.", "Risk")
			.SetCanOptimize(true);

		_useBuyTakeProfit = Param(nameof(UseBuyTakeProfit), false)
			.SetDisplay("Use Buy Take-Profit", "Enable oscillator based take-profit for long positions.", "Risk");

		_buyTakeProfitLevel = Param(nameof(BuyTakeProfitLevel), 0m)
			.SetDisplay("Buy Take-Profit Level", "Oscillator level that exits long positions when enabled.", "Risk")
			.SetCanOptimize(true);

		_sellThreshold = Param(nameof(SellThreshold), 0.75m)
			.SetDisplay("Sell Threshold", "Oscillator value that enables short setups.", "Oscillator")
			.SetCanOptimize(true);

		_useSellStopLoss = Param(nameof(UseSellStopLoss), false)
			.SetDisplay("Use Sell Stop-Loss", "Require the oscillator to stay below the short stop level before opening shorts.", "Risk");

		_sellStopLossLevel = Param(nameof(SellStopLossLevel), 1m)
			.SetDisplay("Sell Stop-Loss Level", "Oscillator level that closes short positions when enabled.", "Risk")
			.SetCanOptimize(true);

		_useSellTakeProfit = Param(nameof(UseSellTakeProfit), false)
			.SetDisplay("Use Sell Take-Profit", "Enable oscillator based take-profit for short positions.", "Risk");

		_sellTakeProfitLevel = Param(nameof(SellTakeProfitLevel), 0m)
			.SetDisplay("Sell Take-Profit Level", "Oscillator level that exits short positions when enabled.", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_vortex = null!;
		_longSetup = false;
		_shortSetup = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vortex = new VortexIndicator
		{
			Length = VortexLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_vortex, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_vortex.IsFormed)
			return;

		var oscillator = CalculateOscillator(viPlus, viMinus);
		if (oscillator == null)
			return;

		UpdateSetupFlags(oscillator.Value);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageOpenPosition(oscillator.Value);
		ExecuteEntries();
	}

	private void UpdateSetupFlags(decimal oscillator)
	{
		if (UseBuyStopLoss)
		{
			if (oscillator <= BuyThreshold && oscillator > BuyStopLossLevel)
			{
				_longSetup = true;
				_shortSetup = false;
			}
		}
		else if (oscillator <= BuyThreshold)
		{
			_longSetup = true;
			_shortSetup = false;
		}

		if (UseSellStopLoss)
		{
			if (oscillator >= SellThreshold && oscillator < SellStopLossLevel)
			{
				_shortSetup = true;
				_longSetup = false;
			}
		}
		else if (oscillator >= SellThreshold)
		{
			_shortSetup = true;
			_longSetup = false;
		}

		if (oscillator >= BuyThreshold && oscillator <= SellThreshold)
		{
			_longSetup = false;
			_shortSetup = false;
		}
	}

	private void ManageOpenPosition(decimal oscillator)
	{
		var position = Position;

		if (position > 0)
		{
			if (UseBuyStopLoss && oscillator <= BuyStopLossLevel)
			{
				SellMarket(position);
			}

			if (UseBuyTakeProfit && oscillator >= BuyTakeProfitLevel)
			{
				SellMarket(position);
			}
		}
		else if (position < 0)
		{
			var volumeToClose = Math.Abs(position);

			if (UseSellStopLoss && oscillator >= SellStopLossLevel)
			{
				BuyMarket(volumeToClose);
			}

			if (UseSellTakeProfit && oscillator <= SellTakeProfitLevel)
			{
				BuyMarket(volumeToClose);
			}
		}
	}

	private void ExecuteEntries()
	{
		var position = Position;

		if (_longSetup && position <= 0)
		{
			var volumeToBuy = Volume + (position < 0 ? Math.Abs(position) : 0m);
			BuyMarket(volumeToBuy);
			_longSetup = false;
		}
		else if (_shortSetup && position >= 0)
		{
			var volumeToSell = Volume + (position > 0 ? position : 0m);
			SellMarket(volumeToSell);
			_shortSetup = false;
		}
	}

	private static decimal? CalculateOscillator(decimal viPlus, decimal viMinus)
	{
		var sum = viPlus + viMinus;
		if (sum == 0m)
			return null;

		return (viPlus - viMinus) / sum;
	}
}
