import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class ema_plus_wpr_v2_strategy(Strategy):
    def __init__(self):
        super(ema_plus_wpr_v2_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._wpr_length = self.Param("WprLength", 14) \
            .SetDisplay("WPR Length", "Williams %R period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def wpr_length(self):
        return self._wpr_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ema_plus_wpr_v2_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        wpr = WilliamsR()
        wpr.Length = self.wpr_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, wpr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val, wpr_val):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        # WPR range is -100 to 0
        # Buy on oversold
        if wpr_val < -80 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell on overbought
        elif wpr_val > -20 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return ema_plus_wpr_v2_strategy()
