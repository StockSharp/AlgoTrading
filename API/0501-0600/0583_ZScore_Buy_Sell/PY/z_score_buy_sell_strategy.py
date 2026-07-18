import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class z_score_buy_sell_strategy(Strategy):
    def __init__(self):
        super(z_score_buy_sell_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rolling_window = self.Param("RollingWindow", 300) \
            .SetGreaterThanZero() \
            .SetDisplay("Rolling Window", "Lookback period", "Parameters")
        self._z_threshold = self.Param("ZThreshold", 2.8) \
            .SetGreaterThanZero() \
            .SetDisplay("Z Threshold", "Z-score trigger level", "Parameters")
        self._cool_down = self.Param("CoolDown", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Cool Down", "Bars to wait after trade", "Parameters")
        self._buy_cooldown_counter = 0
        self._sell_cooldown_counter = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(z_score_buy_sell_strategy, self).OnReseted()
        cd = self._cool_down.Value
        self._buy_cooldown_counter = cd
        self._sell_cooldown_counter = cd

    def OnStarted2(self, time):
        super(z_score_buy_sell_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self._rolling_window.Value
        std_dev = StandardDeviation()
        std_dev.Length = self._rolling_window.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val, std_dev_val):
        if candle.State != CandleStates.Finished:
            return
        std_v = float(std_dev_val)
        if std_v == 0:
            return
        close = float(candle.ClosePrice)
        sma_v = float(sma_val)
        z_score = (close - sma_v) / std_v
        z_thresh = float(self._z_threshold.Value)
        cd = self._cool_down.Value
        if z_score > z_thresh:
            if self._sell_cooldown_counter >= cd:
                if self.Position >= 0:
                    self.SellMarket()
                self._sell_cooldown_counter = 0
                self._buy_cooldown_counter = cd
            else:
                self._sell_cooldown_counter += 1
        elif z_score < -z_thresh:
            if self._buy_cooldown_counter >= cd:
                if self.Position <= 0:
                    self.BuyMarket()
                self._sell_cooldown_counter = cd
                self._buy_cooldown_counter = 0
            else:
                self._buy_cooldown_counter += 1

    def CreateClone(self):
        return z_score_buy_sell_strategy()
