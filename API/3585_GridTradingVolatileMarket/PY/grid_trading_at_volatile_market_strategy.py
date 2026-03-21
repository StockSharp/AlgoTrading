import clr
from collections import deque

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class grid_trading_at_volatile_market_strategy(Strategy):
    """
    Grid Trading at Volatile Market: RSI + SMA trend filter for initial entry
    then manages a grid of averaging orders based on distance threshold.
    """

    def __init__(self):
        super(grid_trading_at_volatile_market_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period for entry signals", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Indicators")
        self._max_grid_levels = self.Param("MaxGridLevels", 3) \
            .SetDisplay("Max Grid Levels", "Maximum averaging levels", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for signal detection", "General")

        self._sma_queue = deque()
        self._sma_sum = 0.0
        self._grid_direction = None  # 'buy' or 'sell'
        self._grid_level = 0
        self._last_entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_trading_at_volatile_market_strategy, self).OnReseted()
        self._sma_queue = deque()
        self._sma_sum = 0.0
        self._grid_direction = None
        self._grid_level = 0
        self._last_entry_price = 0.0

    def OnStarted(self, time):
        super(grid_trading_at_volatile_market_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rsi = float(rsi_val)

        self._sma_queue.append(close)
        self._sma_sum += close
        sma_period = self._sma_period.Value
        while len(self._sma_queue) > sma_period:
            self._sma_sum -= self._sma_queue.popleft()

        if len(self._sma_queue) < sma_period:
            return

        sma_value = self._sma_sum / len(self._sma_queue)

        if self._grid_direction is None:
            if rsi < 35 and close < sma_value:
                self.BuyMarket()
                self._grid_direction = 'buy'
                self._grid_level = 1
                self._last_entry_price = close
            elif rsi > 65 and close > sma_value:
                self.SellMarket()
                self._grid_direction = 'sell'
                self._grid_level = 1
                self._last_entry_price = close
        else:
            distance_threshold = self._last_entry_price * 0.005

            if self._grid_direction == 'buy':
                if self._grid_level < self._max_grid_levels.Value and close < self._last_entry_price - distance_threshold:
                    self.BuyMarket()
                    self._grid_level += 1
                    self._last_entry_price = close
                elif close > sma_value and rsi > 50:
                    if self.Position > 0:
                        self.SellMarket()
                    self._grid_direction = None
                    self._grid_level = 0
            elif self._grid_direction == 'sell':
                if self._grid_level < self._max_grid_levels.Value and close > self._last_entry_price + distance_threshold:
                    self.SellMarket()
                    self._grid_level += 1
                    self._last_entry_price = close
                elif close < sma_value and rsi < 50:
                    if self.Position < 0:
                        self.BuyMarket()
                    self._grid_direction = None
                    self._grid_level = 0

    def CreateClone(self):
        return grid_trading_at_volatile_market_strategy()
