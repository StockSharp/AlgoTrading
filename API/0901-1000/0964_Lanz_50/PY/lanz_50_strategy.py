import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class lanz_50_strategy(Strategy):
    def __init__(self):
        super(lanz_50_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "EMA filter period", "Indicators")
        self._enable_buy = self.Param("EnableBuy", True) \
            .SetDisplay("Enable Buy", "Allow long trades", "Mode")
        self._enable_sell = self.Param("EnableSell", False) \
            .SetDisplay("Enable Sell", "Allow short trades", "Mode")
        self._max_entries = self.Param("MaxEntriesOverall", 45) \
            .SetDisplay("Max Entries Overall", "Maximum entries per run", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entries_overall = 0
        self._prev1_bullish = None
        self._prev2_bullish = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(lanz_50_strategy, self).OnReseted()
        self._entries_overall = 0
        self._prev1_bullish = None
        self._prev2_bullish = None

    def OnStarted2(self, time):
        super(lanz_50_strategy, self).OnStarted2(time)
        self._entries_overall = 0
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        ev = float(ema_val)
        bullish = close > open_p
        bearish = close < open_p
        buy_signal = (self._enable_buy.Value and close > ev
                      and bullish and self._prev1_bullish == True and self._prev2_bullish == True)
        sell_signal = (self._enable_sell.Value and close < ev
                       and bearish and self._prev1_bullish == False and self._prev2_bullish == False)
        if buy_signal and self.Position <= 0 and self._entries_overall < self._max_entries.Value:
            self.BuyMarket()
            self._entries_overall += 1
        elif sell_signal and self.Position >= 0 and self._entries_overall < self._max_entries.Value:
            self.SellMarket()
            self._entries_overall += 1
        self._prev2_bullish = self._prev1_bullish
        self._prev1_bullish = bullish

    def CreateClone(self):
        return lanz_50_strategy()
