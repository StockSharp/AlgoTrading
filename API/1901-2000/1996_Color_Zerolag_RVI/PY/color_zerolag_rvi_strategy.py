import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeVigorIndex
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_rvi_strategy(Strategy):

    def __init__(self):
        super(color_zerolag_rvi_strategy, self).__init__()

        self._rvi_length = self.Param("RviLength", 14) \
            .SetDisplay("RVI Length", "RVI calculation period", "Indicator")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal Length", "RVI signal line period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._buy_open = self.Param("BuyOpen", True).SetDisplay("Buy Open", "Allow long entries", "Trading")
        self._buy_close = self.Param("BuyClose", True).SetDisplay("Buy Close", "Close longs on a sell signal", "Trading")
        self._sell_open = self.Param("SellOpen", True).SetDisplay("Sell Open", "Allow short entries", "Trading")
        self._sell_close = self.Param("SellClose", True).SetDisplay("Sell Close", "Close shorts on a buy signal", "Trading")

        self._prev_rvi = None
        self._prev_signal = None

    @property
    def RviLength(self):
        return self._rvi_length.Value

    @RviLength.setter
    def RviLength(self, value):
        self._rvi_length.Value = value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @SignalLength.setter
    def SignalLength(self, value):
        self._signal_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    def OnStarted2(self, time):
        super(color_zerolag_rvi_strategy, self).OnStarted2(time)

        rvi = RelativeVigorIndex()
        rvi.Average.Length = self.RviLength
        rvi.Signal.Length = self.SignalLength

        self.SubscribeCandles(self.CandleType) \
            .BindEx(rvi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, rvi_value):
        if candle.State != CandleStates.Finished:
            return

        avg_raw = rvi_value.Average
        sig_raw = rvi_value.Signal
        if avg_raw is None or sig_raw is None:
            return

        rvi_val = float(avg_raw)
        signal_val = float(sig_raw)

        if self._prev_rvi is None or self._prev_signal is None:
            self._prev_rvi = rvi_val
            self._prev_signal = signal_val
            return

        cross_up = self._prev_rvi < self._prev_signal and rvi_val > signal_val
        cross_down = self._prev_rvi > self._prev_signal and rvi_val < signal_val

        # Downward RVI crossover is the documented *buy* signal.
        # Closure and entry are independently configurable; do not net against
        # an opposite position when its close permission is disabled.
        if cross_down:
            current_position = self.Position
            if current_position < 0 and self.SellClose:
                self.BuyMarket(-current_position)
            if self.BuyOpen and (current_position == 0 or (current_position < 0 and self.SellClose)):
                self.BuyMarket(self.Volume)
        elif cross_up:
            current_position = self.Position
            if current_position > 0 and self.BuyClose:
                self.SellMarket(current_position)
            if self.SellOpen and (current_position == 0 or (current_position > 0 and self.BuyClose)):
                self.SellMarket(self.Volume)

        self._prev_rvi = rvi_val
        self._prev_signal = signal_val

    def OnReseted(self):
        super(color_zerolag_rvi_strategy, self).OnReseted()
        self._prev_rvi = None
        self._prev_signal = None

    def CreateClone(self):
        return color_zerolag_rvi_strategy()
