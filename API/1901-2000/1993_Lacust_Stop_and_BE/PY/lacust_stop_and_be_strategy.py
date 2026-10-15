import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lacust_stop_and_be_strategy(Strategy):

    def __init__(self):
        super(lacust_stop_and_be_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Candle type", "General")
        self._stop_loss = self.Param("StopLoss", 400.0) \
            .SetDisplay("Stop loss", "Stop loss distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take profit", "Take profit distance", "Risk")
        self._trailing_start = self.Param("TrailingStart", 300.0) \
            .SetDisplay("Trailing start", "Profit to activate trailing", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 200.0) \
            .SetDisplay("Trailing stop", "Trailing stop distance", "Risk")
        self._breakeven_gain = self.Param("BreakevenGain", 250.0) \
            .SetDisplay("Breakeven gain", "Profit for breakeven move", "Risk")
        self._breakeven = self.Param("Breakeven", 100.0) \
            .SetDisplay("Breakeven", "Profit locked at breakeven", "Risk")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def TrailingStart(self):
        return self._trailing_start.Value

    @TrailingStart.setter
    def TrailingStart(self, value):
        self._trailing_start.Value = value

    @property
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    @property
    def BreakevenGain(self):
        return self._breakeven_gain.Value

    @BreakevenGain.setter
    def BreakevenGain(self, value):
        self._breakeven_gain.Value = value

    @property
    def Breakeven(self):
        return self._breakeven.Value

    @Breakeven.setter
    def Breakeven(self, value):
        self._breakeven.Value = value

    def OnStarted2(self, time):
        super(lacust_stop_and_be_strategy, self).OnStarted2(time)

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)
        ts_start = float(self.TrailingStart)
        ts_stop = float(self.TrailingStop)
        be_gain = float(self.BreakevenGain)
        be = float(self.Breakeven)

        if self.Position == 0:
            if close > open_price:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - sl
                self._take_price = self._entry_price + tp
            elif close < open_price:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + sl
                self._take_price = self._entry_price - tp
            return

        if self.Position > 0:
            if close - self._entry_price >= be_gain and self._stop_price < self._entry_price + be:
                self._stop_price = self._entry_price + be

            if close - self._entry_price >= ts_start and self._stop_price < close - ts_stop:
                self._stop_price = close - ts_stop

            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket()

        elif self.Position < 0:
            if self._entry_price - close >= be_gain and self._stop_price > self._entry_price - be:
                self._stop_price = self._entry_price - be

            if self._entry_price - close >= ts_start and self._stop_price > close + ts_stop:
                self._stop_price = close + ts_stop

            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket()

    def OnReseted(self):
        super(lacust_stop_and_be_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def CreateClone(self):
        return lacust_stop_and_be_strategy()
