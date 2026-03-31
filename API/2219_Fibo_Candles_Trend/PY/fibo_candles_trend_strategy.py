import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class fibo_candles_trend_strategy(Strategy):
    def __init__(self):
        super(fibo_candles_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type and timeframe of candles", "General")
        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "Lookback period for high/low", "FiboCandles")
        self._fibo_level = self.Param("Level", 1) \
            .SetDisplay("Fibo Level", "Fibonacci ratio level (1-5)", "FiboCandles")
        self._stop_loss = self.Param("StopLoss", 1000) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk")
        self._highest = None
        self._lowest = None
        self._trend = 0
        self._previous_color = None
        self._level_multiplier = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def period(self):
        return self._period.Value

    @property
    def fibo_level(self):
        return self._fibo_level.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    def _get_level_multiplier(self, level):
        levels = {1: 0.236, 2: 0.382, 3: 0.500, 4: 0.618, 5: 0.762}
        return levels.get(level, 0.236)

    def OnReseted(self):
        super(fibo_candles_trend_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._trend = 0
        self._previous_color = None
        self._level_multiplier = 0.0

    def OnStarted2(self, time):
        super(fibo_candles_trend_strategy, self).OnStarted2(time)
        self._highest = Highest()
        self._highest.Length = self.period
        self._lowest = Lowest()
        self._lowest.Length = self.period
        self._level_multiplier = self._get_level_multiplier(self.fibo_level)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        highest_val = float(highest_val)
        lowest_val = float(lowest_val)
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return
        rng = highest_val - lowest_val
        trend = self._trend
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)
        if o > c:
            if not (trend < 0 and rng * self._level_multiplier < c - lowest_val):
                trend = 1
            else:
                trend = -1
        else:
            if not (trend > 0 and rng * self._level_multiplier < highest_val - c):
                trend = -1
            else:
                trend = 1
        color = 1 if trend == 1 else 0
        if self._previous_color is not None:
            if color == 1 and self._previous_color == 0:
                self.BuyMarket()
            elif color == 0 and self._previous_color == 1:
                self.SellMarket()
        self._previous_color = color
        self._trend = trend

    def CreateClone(self):
        return fibo_candles_trend_strategy()
