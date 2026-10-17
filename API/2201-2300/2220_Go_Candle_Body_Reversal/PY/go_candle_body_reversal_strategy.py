import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class go_candle_body_reversal_strategy(Strategy):
    """Buy on body SMA crossing from positive to negative; sell on reverse."""

    def __init__(self):
        super(go_candle_body_reversal_strategy, self).__init__()
        self._period = self.Param("Period", 174) \
            .SetDisplay("Period", "SMA period for candle body", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._bodies = []
        self._body_sum = Decimal.Zero
        self._prev_nonzero_sign = 0

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(go_candle_body_reversal_strategy, self).OnReseted()
        self._bodies = []
        self._body_sum = Decimal.Zero
        self._prev_nonzero_sign = 0

    def OnStarted2(self, time):
        super(go_candle_body_reversal_strategy, self).OnStarted2(time)
        self._bodies = []
        self._body_sum = Decimal.Zero
        self._prev_nonzero_sign = 0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        body = candle.ClosePrice - candle.OpenPrice
        self._bodies.append(body)
        self._body_sum += body
        length = int(self.Period)
        if length <= 0:
            raise ValueError('Period must be positive')
        if len(self._bodies) > length:
            self._body_sum -= self._bodies.pop(0)
        if len(self._bodies) < length:
            return

        sign = (self._body_sum > Decimal.Zero) - (self._body_sum < Decimal.Zero)
        if sign == 0:
            return
        previous = self._prev_nonzero_sign
        self._prev_nonzero_sign = sign
        if previous == 0 or previous == sign or not self.IsFormedAndOnlineAndAllowTrading():
            return

        position = self.Position
        if previous > 0 and sign < 0 and position <= 0:
            if position < 0:
                self.BuyMarket(-position)
            self.BuyMarket(self.Volume)
        elif previous < 0 and sign > 0 and position >= 0:
            if position > 0:
                self.SellMarket(position)
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return go_candle_body_reversal_strategy()
