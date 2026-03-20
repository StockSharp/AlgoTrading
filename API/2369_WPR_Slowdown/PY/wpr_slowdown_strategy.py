import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class wpr_slowdown_strategy(Strategy):
    def __init__(self):
        super(wpr_slowdown_strategy, self).__init__()
        self._wpr_period = self.Param("WprPeriod", 12) \
            .SetDisplay("WPR Period", "Williams %R indicator period", "Indicator")
        self._level_max = self.Param("LevelMax", -20.0) \
            .SetDisplay("Level Max", "Upper signal level", "Indicator")
        self._level_min = self.Param("LevelMin", -80.0) \
            .SetDisplay("Level Min", "Lower signal level", "Indicator")
        self._seek_slowdown = self.Param("SeekSlowdown", True) \
            .SetDisplay("Seek Slowdown", "Require slowdown between values", "Indicator")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Open Long", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Open Short", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_wpr = None

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def level_max(self):
        return self._level_max.Value

    @property
    def level_min(self):
        return self._level_min.Value

    @property
    def seek_slowdown(self):
        return self._seek_slowdown.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wpr_slowdown_strategy, self).OnReseted()
        self._prev_wpr = None

    def OnStarted(self, time):
        super(wpr_slowdown_strategy, self).OnStarted(time)
        self._prev_wpr = None
        wpr = WilliamsR()
        wpr.Length = int(self.wpr_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr):
        if candle.State != CandleStates.Finished:
            return
        wpr = float(wpr)
        slowdown = self._prev_wpr is None or abs(wpr - self._prev_wpr) < 1.0
        lmax = float(self.level_max)
        lmin = float(self.level_min)
        can_buy = wpr >= lmax and (not self.seek_slowdown or slowdown)
        can_sell = wpr <= lmin and (not self.seek_slowdown or slowdown)
        if can_buy:
            if self.Position <= 0:
                self.BuyMarket()
        elif can_sell:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_wpr = wpr

    def CreateClone(self):
        return wpr_slowdown_strategy()
