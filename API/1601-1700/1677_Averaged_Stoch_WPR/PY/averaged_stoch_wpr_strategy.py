import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class averaged_stoch_wpr_strategy(Strategy):
    def __init__(self):
        super(averaged_stoch_wpr_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._wpr_period = self.Param("WprPeriod", 14) \
            .SetDisplay("WPR Period", "Williams %R period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(averaged_stoch_wpr_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, wpr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, wpr):
        if candle.State != CandleStates.Finished:
            return
        # Buy: RSI oversold + WPR oversold
        if rsi < 30 and wpr < -80 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell: RSI overbought + WPR overbought
        elif rsi > 70 and wpr > -20 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit
        elif self.Position > 0 and rsi > 65:
            self.SellMarket()
        elif self.Position < 0 and rsi < 35:
            self.BuyMarket()

    def CreateClone(self):
        return averaged_stoch_wpr_strategy()
