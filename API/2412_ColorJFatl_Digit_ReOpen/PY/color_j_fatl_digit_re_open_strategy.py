import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_j_fatl_digit_re_open_strategy(Strategy):
    def __init__(self):
        super(color_j_fatl_digit_re_open_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._jma_length = self.Param("JmaLength", 5)
        self._price_step_param = self.Param("PriceStep", 300)
        self._max_positions = self.Param("MaxPositions", 1)
        self._buy_pos_open = self.Param("BuyPosOpen", True)
        self._sell_pos_open = self.Param("SellPosOpen", True)
        self._buy_pos_close = self.Param("BuyPosClose", True)
        self._sell_pos_close = self.Param("SellPosClose", True)

        self._prev_jma = None
        self._prev_direction = 0
        self._last_entry_price = None
        self._positions_opened = 0
        self._price_step = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def JmaLength(self):
        return self._jma_length.Value

    @JmaLength.setter
    def JmaLength(self, value):
        self._jma_length.Value = value

    @property
    def PriceStep(self):
        return self._price_step_param.Value

    @PriceStep.setter
    def PriceStep(self, value):
        self._price_step_param.Value = value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @MaxPositions.setter
    def MaxPositions(self, value):
        self._max_positions.Value = value

    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value

    @BuyPosOpen.setter
    def BuyPosOpen(self, value):
        self._buy_pos_open.Value = value

    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value

    @SellPosOpen.setter
    def SellPosOpen(self, value):
        self._sell_pos_open.Value = value

    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value

    @BuyPosClose.setter
    def BuyPosClose(self, value):
        self._buy_pos_close.Value = value

    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    @SellPosClose.setter
    def SellPosClose(self, value):
        self._sell_pos_close.Value = value

    def OnStarted(self, time):
        super(color_j_fatl_digit_re_open_strategy, self).OnStarted(time)

        sec_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        self._price_step = sec_step * float(self.PriceStep)

        jma = JurikMovingAverage()
        jma.Length = self.JmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(jma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return

        jma_val = float(jma_value)
        close = float(candle.ClosePrice)

        if self._prev_jma is not None:
            if jma_val > self._prev_jma:
                direction = 1
            elif jma_val < self._prev_jma:
                direction = -1
            else:
                direction = 0
        else:
            direction = 0

        if direction == -1 and self.Position > 0 and self.BuyPosClose:
            self.SellMarket()
            self._positions_opened = 0
            self._last_entry_price = None
        elif direction == 1 and self.Position < 0 and self.SellPosClose:
            self.BuyMarket()
            self._positions_opened = 0
            self._last_entry_price = None

        if direction == 1 and self._prev_direction != 1 and self.BuyPosOpen and self.Position <= 0:
            self.BuyMarket()
            self._positions_opened = 1
            self._last_entry_price = close
        elif direction == -1 and self._prev_direction != -1 and self.SellPosOpen and self.Position >= 0:
            self.SellMarket()
            self._positions_opened = 1
            self._last_entry_price = close
        elif (self.Position > 0 and self.BuyPosOpen
              and self._positions_opened < int(self.MaxPositions)
              and self._last_entry_price is not None
              and close - self._last_entry_price >= self._price_step):
            self.BuyMarket()
            self._positions_opened += 1
            self._last_entry_price = close
        elif (self.Position < 0 and self.SellPosOpen
              and self._positions_opened < int(self.MaxPositions)
              and self._last_entry_price is not None
              and self._last_entry_price - close >= self._price_step):
            self.SellMarket()
            self._positions_opened += 1
            self._last_entry_price = close

        self._prev_direction = direction
        self._prev_jma = jma_val

    def OnReseted(self):
        super(color_j_fatl_digit_re_open_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_direction = 0
        self._last_entry_price = None
        self._positions_opened = 0

    def CreateClone(self):
        return color_j_fatl_digit_re_open_strategy()
