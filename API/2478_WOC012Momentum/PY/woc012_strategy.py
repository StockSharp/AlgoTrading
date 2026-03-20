import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange


class woc012_strategy(Strategy):
    def __init__(self):
        super(woc012_strategy, self).__init__()

        self._sequence_length = self.Param("SequenceLength", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Sequence Length", "Consecutive bars in same direction to trigger entry", "Signals")

        self._stop_loss_atr_mult = self.Param("StopLossAtrMult", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("SL ATR Mult", "Stop loss as ATR multiple", "Risk")

        self._trailing_atr_mult = self.Param("TrailingAtrMult", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail ATR Mult", "Trailing stop as ATR multiple", "Risk")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR calculation length", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._up_count = 0
        self._down_count = 0
        self._entry_price = 0.0
        self._stop_price = None

    @property
    def SequenceLength(self):
        return self._sequence_length.Value

    @SequenceLength.setter
    def SequenceLength(self, value):
        self._sequence_length.Value = value

    @property
    def StopLossAtrMult(self):
        return self._stop_loss_atr_mult.Value

    @StopLossAtrMult.setter
    def StopLossAtrMult(self, value):
        self._stop_loss_atr_mult.Value = value

    @property
    def TrailingAtrMult(self):
        return self._trailing_atr_mult.Value

    @TrailingAtrMult.setter
    def TrailingAtrMult(self, value):
        self._trailing_atr_mult.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(woc012_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(atr, self.process_candle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        atr = float(atr_val)

        if self._prev_close > 0:
            if close > self._prev_close:
                self._up_count += 1
                self._down_count = 0
            elif close < self._prev_close:
                self._down_count += 1
                self._up_count = 0
            else:
                self._up_count = 0
                self._down_count = 0

        self._prev_close = close

        if self.Position != 0:
            if self.Position > 0:
                trail = close - float(self.TrailingAtrMult) * atr
                if self._stop_price is None or trail > self._stop_price:
                    self._stop_price = trail

                if close <= self._stop_price:
                    self.SellMarket(abs(self.Position))
                    self._stop_price = None
                    self._entry_price = 0.0
                    return
            else:
                trail = close + float(self.TrailingAtrMult) * atr
                if self._stop_price is None or trail < self._stop_price:
                    self._stop_price = trail

                if close >= self._stop_price:
                    self.BuyMarket(abs(self.Position))
                    self._stop_price = None
                    self._entry_price = 0.0
                    return

        if self._up_count >= self.SequenceLength and self.Position <= 0:
            vol = self.Volume + abs(self.Position)
            self.BuyMarket(vol)
            self._entry_price = close
            self._stop_price = close - float(self.StopLossAtrMult) * atr
            self._up_count = 0
        elif self._down_count >= self.SequenceLength and self.Position >= 0:
            vol = self.Volume + abs(self.Position)
            self.SellMarket(vol)
            self._entry_price = close
            self._stop_price = close + float(self.StopLossAtrMult) * atr
            self._down_count = 0

    def OnReseted(self):
        super(woc012_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._up_count = 0
        self._down_count = 0
        self._entry_price = 0.0
        self._stop_price = None

    def CreateClone(self):
        return woc012_strategy()
