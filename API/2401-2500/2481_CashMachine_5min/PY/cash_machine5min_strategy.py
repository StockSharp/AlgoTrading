import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class cash_machine5min_strategy(Strategy):
    def __init__(self):
        super(cash_machine5min_strategy, self).__init__()

        self._tp_atr_mult = self.Param("TpAtrMult", 2.5)
        self._sl_atr_mult = self.Param("SlAtrMult", 1.5)
        self._trail_atr_mult = self.Param("TrailAtrMult", 1.0)
        self._de_marker_length = self.Param("DeMarkerLength", 14)
        self._rsi_length = self.Param("RsiLength", 14)
        self._atr_length = self.Param("AtrLength", 14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_de_marker = None
        self._prev_rsi = None
        self._entry_price = 0.0
        self._stop_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(cash_machine5min_strategy, self).OnStarted2(time)

        self._prev_de_marker = None
        self._prev_rsi = None
        self._entry_price = 0.0
        self._stop_price = None

        de_marker = DeMarker()
        de_marker.Length = self._de_marker_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(de_marker, rsi, atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, de_marker_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        de_marker = float(de_marker_value)
        rsi = float(rsi_value)
        atr = float(atr_value)
        close = float(candle.ClosePrice)
        tp_mult = float(self._tp_atr_mult.Value)
        sl_mult = float(self._sl_atr_mult.Value)
        trail_mult = float(self._trail_atr_mult.Value)

        pos = float(self.Position)
        if pos > 0:
            trail = close - trail_mult * atr
            if self._stop_price is None or trail > self._stop_price:
                self._stop_price = trail
            tp = self._entry_price + tp_mult * atr
            if close <= self._stop_price or close >= tp:
                self.SellMarket(abs(pos))
                self._stop_price = None
                self._entry_price = 0.0
        elif pos < 0:
            trail = close + trail_mult * atr
            if self._stop_price is None or trail < self._stop_price:
                self._stop_price = trail
            tp = self._entry_price - tp_mult * atr
            if close >= self._stop_price or close <= tp:
                self.BuyMarket(abs(pos))
                self._stop_price = None
                self._entry_price = 0.0

        if self.Position == 0 and self._prev_de_marker is not None and self._prev_rsi is not None:
            long_signal = (self._prev_de_marker < 0.25 and de_marker >= 0.25) or (self._prev_rsi < 25.0 and rsi >= 25.0)
            short_signal = (self._prev_de_marker > 0.75 and de_marker <= 0.75) or (self._prev_rsi > 75.0 and rsi <= 75.0)

            if long_signal:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - sl_mult * atr
            elif short_signal:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + sl_mult * atr

        self._prev_de_marker = de_marker
        self._prev_rsi = rsi

    def OnReseted(self):
        super(cash_machine5min_strategy, self).OnReseted()
        self._prev_de_marker = None
        self._prev_rsi = None
        self._entry_price = 0.0
        self._stop_price = None

    def CreateClone(self):
        return cash_machine5min_strategy()
