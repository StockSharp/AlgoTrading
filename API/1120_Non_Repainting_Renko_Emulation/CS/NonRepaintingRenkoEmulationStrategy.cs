using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Non-repainting renko emulation strategy.
/// </summary>
public class NonRepaintingRenkoEmulationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _brickSize;

	private decimal _renkoPrice;
	private decimal _prevRenkoPrice;
	private int _brickDir;
	private bool _newBrick;

	private readonly List<decimal> _renkoPrices = new();
	private readonly List<int> _brickDirs = new();

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Renko brick size.
	/// </summary>
	public decimal BrickSize
	{
		get => _brickSize.Value;
		set => _brickSize.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NonRepaintingRenkoEmulationStrategy"/>.
	/// </summary>
	public NonRepaintingRenkoEmulationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");

		_brickSize = Param(nameof(BrickSize), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Brick Size", "Renko brick size", "General")
			.SetCanOptimize(true);
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
		_renkoPrice = 0m;
		_prevRenkoPrice = 0m;
		_brickDir = 0;
		_newBrick = false;
		_renkoPrices.Clear();
		_brickDirs.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

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

		var close = candle.ClosePrice;
		_newBrick = false;

		if (_renkoPrice == 0m)
		{
			_renkoPrice = close;
			_prevRenkoPrice = close;
			_renkoPrices.Add(_renkoPrice);
			_brickDirs.Add(0);
			return;
		}

		var diff = close - _renkoPrice;
		var size = BrickSize;

		if (Math.Abs(diff) >= size)
		{
			var numBricks = (int)Math.Floor(Math.Abs(diff) / size);
			_prevRenkoPrice = _renkoPrice;
			_renkoPrice += numBricks * size * Math.Sign(diff);
			_brickDir = Math.Sign(diff);
			_newBrick = true;

			_renkoPrices.Add(_renkoPrice);
			_brickDirs.Add(_brickDir);

			var maxHistory = (int)(size * 3) + 10;
			var remove = _renkoPrices.Count - maxHistory;
			if (remove > 0)
			{
				_renkoPrices.RemoveRange(0, remove);
				_brickDirs.RemoveRange(0, remove);
			}
		}

		if (!_newBrick)
			return;

		var lb = (int)size;
		var count = _renkoPrices.Count;

		if (lb <= 0 || count <= lb * 3)
			return;

		var dirLb = _brickDirs[count - 1 - lb];
		var priceLb = _renkoPrices[count - 1 - lb];
		var price2Lb = _renkoPrices[count - 1 - lb * 2];
		var price3Lb = _renkoPrices[count - 1 - lb * 3];

		if (dirLb < _brickDir && priceLb < price2Lb && _renkoPrice < priceLb && price2Lb < price3Lb && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (dirLb > _brickDir && priceLb > price2Lb && _renkoPrice > priceLb && price2Lb > price3Lb && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (dirLb < _brickDir && Position < 0)
			BuyMarket(Math.Abs(Position));
		else if (dirLb > _brickDir && Position > 0)
			SellMarket(Math.Abs(Position));
	}
}
