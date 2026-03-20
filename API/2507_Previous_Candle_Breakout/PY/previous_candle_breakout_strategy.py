import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class previous_candle_breakout_strategy(Strategy):
    def __init__(self):
        super(previous_candle_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))
        self._stop_loss_offset = self.Param("StopLossOffset", 1000.0)
        self._take_profit_offset = self.Param("TakeProfitOffset", 1500.0)

        self._previous_high = None
        self._previous_low = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossOffset(self):
        return self._stop_loss_offset.Value

    @StopLossOffset.setter
    def StopLossOffset(self, value):
        self._stop_loss_offset.Value = value

    @property
    def TakeProfitOffset(self):
        return self._take_profit_offset.Value

    @TakeProfitOffset.setter
    def TakeProfitOffset(self, value):
        self._take_profit_offset.Value = value

    def OnStarted(self, time):
        super(previous_candle_breakout_strategy, self).OnStarted(time)

        self._previous_high = None
        self._previous_low = None
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._previous_high is None or self._previous_low is None:
            self._previous_high = high
            self._previous_low = low
            return

        previous_high = self._previous_high
        previous_low = self._previous_low

        breakout_above = close > previous_high
        breakout_below = close < previous_low

        sl = float(self.StopLossOffset)
        tp = float(self.TakeProfitOffset)

        if self.Position > 0:
            if sl > 0.0 and close <= self._entry_price - sl:
                self.SellMarket()
                self._entry_price = 0.0
            elif tp > 0.0 and close >= self._entry_price + tp:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if sl > 0.0 and close >= self._entry_price + sl:
                self.BuyMarket()
                self._entry_price = 0.0
            elif tp > 0.0 and close <= self._entry_price - tp:
                self.BuyMarket()
                self._entry_price = 0.0

        if breakout_above:
            if self.Position < 0:
                self.BuyMarket()
                self._entry_price = 0.0

            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close

        elif breakout_below:
            if self.Position > 0:
                self.SellMarket()
                self._entry_price = 0.0

            if self.Position >= 0:
                self.SellMarket()
                self._entry_price = close

        self._previous_high = high
        self._previous_low = low

    def OnReseted(self):
        super(previous_candle_breakout_strategy, self).OnReseted()
        self._previous_high = None
        self._previous_low = None
        self._entry_price = 0.0

    def CreateClone(self):
        return previous_candle_breakout_strategy()
