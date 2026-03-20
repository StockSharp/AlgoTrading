import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class wpr_histogram_strategy(Strategy):
    def __init__(self):
        super(wpr_histogram_strategy, self).__init__()
        self._wpr_period = self.Param("WprPeriod", 14).SetDisplay("WPR Period", "Period for Williams %R", "Indicator")
        self._high_level = self.Param("HighLevel", -30.0).SetDisplay("High Level", "Overbought threshold", "Indicator")
        self._low_level = self.Param("LowLevel", -70.0).SetDisplay("Low Level", "Oversold threshold", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_zone = None
    @property
    def wpr_period(self): return self._wpr_period.Value
    @property
    def high_level(self): return self._high_level.Value
    @property
    def low_level(self): return self._low_level.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(wpr_histogram_strategy, self).OnReseted()
        self._previous_zone = None
    def OnStarted(self, time):
        super(wpr_histogram_strategy, self).OnStarted(time)
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished: return
        wv = float(wpr_value)
        hl = float(self.high_level)
        ll = float(self.low_level)
        if wv > hl: current_zone = 0
        elif wv < ll: current_zone = 2
        else: current_zone = 1
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_zone = current_zone
            return
        if self._previous_zone is None:
            self._previous_zone = current_zone
            return
        if self._previous_zone == 0 and current_zone != 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._previous_zone == 2 and current_zone != 2 and self.Position >= 0:
            self.SellMarket()
        self._previous_zone = current_zone
    def CreateClone(self): return wpr_histogram_strategy()
