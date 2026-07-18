import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class levels_with_revolve_strategy(Strategy):
    def __init__(self):
        super(levels_with_revolve_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("MA Period", "Moving average period for the level", "Parameters")
        self._stop_pct = self.Param("StopPct", 1.5) \
            .SetDisplay("Stop %", "Stop loss percent from entry", "Risk")
        self._take_pct = self.Param("TakePct", 3.0) \
            .SetDisplay("Take %", "Take profit percent from entry", "Risk")
        self._prev_price = 0.0
        self._prev_ma = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def take_pct(self):
        return self._take_pct.Value

    def OnReseted(self):
        super(levels_with_revolve_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_ma = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(levels_with_revolve_strategy, self).OnStarted2(time)
        self._prev_price = 0.0
        self._prev_ma = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        ma = ExponentialMovingAverage()
        ma.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        ma_value = float(ma_value)
        price = float(candle.ClosePrice)
        stop = float(self.stop_pct)
        take = float(self.take_pct)
        # Check stop/take
        if self.Position > 0 and self._entry_price > 0:
            pnl_pct = (price - self._entry_price) / self._entry_price * 100.0
            if pnl_pct >= take or pnl_pct <= -stop:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            pnl_pct = (self._entry_price - price) / self._entry_price * 100.0
            if pnl_pct >= take or pnl_pct <= -stop:
                self.BuyMarket()
                self._entry_price = 0.0
        if not self._has_prev:
            self._prev_price = price
            self._prev_ma = ma_value
            self._has_prev = True
            return
        # Cross above MA - buy (or reverse short)
        if self._prev_price < self._prev_ma and price >= ma_value:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = price
        # Cross below MA - sell (or reverse long)
        elif self._prev_price > self._prev_ma and price <= ma_value:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                self._entry_price = price
        self._prev_price = price
        self._prev_ma = ma_value

    def CreateClone(self):
        return levels_with_revolve_strategy()
