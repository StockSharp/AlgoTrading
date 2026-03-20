import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ultra_fatl_strategy(Strategy):

    def __init__(self):
        super(ultra_fatl_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._length = self.Param("Length", 8) \
            .SetDisplay("Length", "Smoothing period", "UltraFATL")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Bar index for signal calculation", "UltraFATL")

        self._prev_value = 0.0
        self._is_initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    def OnStarted(self, time):
        super(ultra_fatl_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = float(rsi_value)
        current = max(0.0, min(8.0, rsi / 12.5))

        if not self._is_initialized:
            self._prev_value = current
            self._is_initialized = True
            return

        previous = self._prev_value
        self._prev_value = current

        is_buy_signal = previous >= 5.0 and current < 5.0 and current > 0.0
        is_sell_signal = previous <= 4.0 and current > 4.0

        pos = self.Position
        if is_buy_signal and pos <= 0:
            self.BuyMarket(self.Volume + abs(pos))
        elif is_sell_signal and pos >= 0:
            self.SellMarket(self.Volume + abs(pos))

    def OnReseted(self):
        super(ultra_fatl_strategy, self).OnReseted()
        self._prev_value = 0.0
        self._is_initialized = False

    def CreateClone(self):
        return ultra_fatl_strategy()
