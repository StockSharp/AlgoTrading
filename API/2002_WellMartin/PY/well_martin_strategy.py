import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class well_martin_strategy(Strategy):

    def __init__(self):
        super(well_martin_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 84) \
            .SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 1.8) \
            .SetDisplay("Bollinger Width", "Bollinger Bands width", "Indicators")
        self._take_profit = self.Param("TakeProfit", 1200.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 1400.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")

        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerWidth(self):
        return self._bollinger_width.Value

    @BollingerWidth.setter
    def BollingerWidth(self, value):
        self._bollinger_width.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def OnStarted(self, time):
        super(well_martin_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self.BollingerPeriod
        bb.Width = self.BollingerWidth

        self.SubscribeCandles(self.CandleType) \
            .BindEx(bb, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        upper_raw = bb_value.UpBand
        lower_raw = bb_value.LowBand
        if upper_raw is None or lower_raw is None:
            return

        upper = float(upper_raw)
        lower = float(lower_raw)
        close = float(candle.ClosePrice)
        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)

        if self.Position > 0:
            profit = close - self._entry_price
            if close >= upper or (tp > 0 and profit >= tp) or (sl > 0 and -profit >= sl):
                self.SellMarket()
                return
        elif self.Position < 0:
            profit = self._entry_price - close
            if close <= lower or (tp > 0 and profit >= tp) or (sl > 0 and -profit >= sl):
                self.BuyMarket()
                return

        if self.Position != 0:
            return

        if close < lower:
            self.BuyMarket()
            self._entry_price = close
        elif close > upper:
            self.SellMarket()
            self._entry_price = close

    def OnReseted(self):
        super(well_martin_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return well_martin_strategy()
