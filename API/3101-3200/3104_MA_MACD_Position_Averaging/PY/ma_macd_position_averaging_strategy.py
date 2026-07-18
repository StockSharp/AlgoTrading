import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_macd_position_averaging_strategy(Strategy):
    """
    WMA crossover with SL/TP in price steps.
    """

    def __init__(self):
        super(ma_macd_position_averaging_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 15).SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 100).SetDisplay("Slow WMA", "Slow WMA period", "Indicators")
        self._sl_points = self.Param("StopLossPoints", 200).SetDisplay("Stop Loss", "SL in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 400).SetDisplay("Take Profit", "TP in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_macd_position_averaging_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ma_macd_position_averaging_strategy, self).OnStarted2(time)
        fast = WeightedMovingAverage()
        fast.Length = self._fast_period.Value
        slow = WeightedMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        f = float(fast_val)
        s = float(slow_val)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = f
            self._prev_slow = s
            return
        close = float(candle.ClosePrice)
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if self.Position > 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close <= self._entry_price - self._sl_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = f
                self._prev_slow = s
                return
            if self._tp_points.Value > 0 and close >= self._entry_price + self._tp_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = f
                self._prev_slow = s
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close >= self._entry_price + self._sl_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = f
                self._prev_slow = s
                return
            if self._tp_points.Value > 0 and close <= self._entry_price - self._tp_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = f
                self._prev_slow = s
                return
        if self._prev_fast <= self._prev_slow and f > s and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 80
        elif self._prev_fast >= self._prev_slow and f < s and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 80
        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return ma_macd_position_averaging_strategy()
