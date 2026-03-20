import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class binario31_strategy(Strategy):

    def __init__(self):
        super(binario31_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Indicator")
        self._channel_offset = self.Param("ChannelOffset", 50.0) \
            .SetDisplay("Channel Offset", "Distance from EMA for channel", "Indicator")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit distance", "Risk")
        self._stop_loss = self.Param("StopLossVal", 1500.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._entry_price = 0.0

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @EmaLength.setter
    def EmaLength(self, value):
        self._ema_length.Value = value

    @property
    def ChannelOffset(self):
        return self._channel_offset.Value

    @ChannelOffset.setter
    def ChannelOffset(self, value):
        self._channel_offset.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLossVal(self):
        return self._stop_loss.Value

    @StopLossVal.setter
    def StopLossVal(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(binario31_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        self.SubscribeCandles(self.CandleType) \
            .Bind(ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        offset = float(self.ChannelOffset)
        tp = float(self.TakeProfit)
        sl = float(self.StopLossVal)

        upper_band = ema_val + offset
        lower_band = ema_val - offset

        if self.Position > 0:
            profit = close - self._entry_price
            if (tp > 0 and profit >= tp) or (sl > 0 and -profit >= sl):
                self.SellMarket()
                return
        elif self.Position < 0:
            profit = self._entry_price - close
            if (tp > 0 and profit >= tp) or (sl > 0 and -profit >= sl):
                self.BuyMarket()
                return

        if self.Position == 0:
            if close > upper_band:
                self.BuyMarket()
                self._entry_price = close
            elif close < lower_band:
                self.SellMarket()
                self._entry_price = close

    def OnReseted(self):
        super(binario31_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return binario31_strategy()
