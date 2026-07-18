import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class surfing_30_strategy(Strategy):
    def __init__(self):
        super(surfing_30_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 1.0)
        self._tp_points = self.Param("TakeProfitPoints", 80)
        self._sl_points = self.Param("StopLossPoints", 50)
        self._ma_period = self.Param("MaPeriod", 50)
        self._rsi_period = self.Param("RsiPeriod", 10)
        self._long_rsi = self.Param("LongRsiThreshold", 30.0)
        self._short_rsi = self.Param("ShortRsiThreshold", 70.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._prev_close = None
        self._prev_sma = None
        self._sl_price = None
        self._tp_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    def OnReseted(self):
        super(surfing_30_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_sma = None
        self._sl_price = None
        self._tp_price = None

    def OnStarted2(self, time):
        super(surfing_30_strategy, self).OnStarted2(time)

        self.Volume = float(self.OrderVolume)

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, rsi, self._process_candle).Start()

    def _close_current_position(self):
        if self.Position > 0:
            self.SellMarket(float(self.Position))
        elif self.Position < 0:
            self.BuyMarket(abs(float(self.Position)))

    def _set_targets(self, entry_price, is_long):
        price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            price_step = float(self.Security.PriceStep)

        sl_pts = int(self._sl_points.Value)
        tp_pts = int(self._tp_points.Value)

        if is_long:
            self._sl_price = entry_price - sl_pts * price_step if sl_pts > 0 else None
            self._tp_price = entry_price + tp_pts * price_step if tp_pts > 0 else None
        else:
            self._sl_price = entry_price + sl_pts * price_step if sl_pts > 0 else None
            self._tp_price = entry_price - tp_pts * price_step if tp_pts > 0 else None

    def _reset_targets(self):
        self._sl_price = None
        self._tp_price = None

    def _manage_active_position(self, candle):
        if self.Position > 0:
            if self._sl_price is not None and float(candle.LowPrice) <= self._sl_price:
                self._close_current_position()
                self._reset_targets()
                return True
            if self._tp_price is not None and float(candle.HighPrice) >= self._tp_price:
                self._close_current_position()
                self._reset_targets()
                return True
        elif self.Position < 0:
            if self._sl_price is not None and float(candle.HighPrice) >= self._sl_price:
                self._close_current_position()
                self._reset_targets()
                return True
            if self._tp_price is not None and float(candle.LowPrice) <= self._tp_price:
                self._close_current_position()
                self._reset_targets()
                return True
        return False

    def _process_candle(self, candle, sma_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sma_v = float(sma_val)
        rsi_v = float(rsi_val)

        if self._manage_active_position(candle):
            self._prev_close = close
            self._prev_sma = sma_v
            return

        if self._prev_close is None or self._prev_sma is None:
            self._prev_close = close
            self._prev_sma = sma_v
            return

        prev_close = self._prev_close
        prev_sma = self._prev_sma

        buy_signal = prev_close <= prev_sma and close > sma_v and rsi_v > float(self._long_rsi.Value)
        sell_signal = prev_close >= prev_sma and close < sma_v and rsi_v < float(self._short_rsi.Value)

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self._close_current_position()
                self._reset_targets()
            self.BuyMarket(float(self.OrderVolume))
            self._set_targets(close, True)
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self._close_current_position()
                self._reset_targets()
            self.SellMarket(float(self.OrderVolume))
            self._set_targets(close, False)

        self._prev_close = close
        self._prev_sma = sma_v

    def CreateClone(self):
        return surfing_30_strategy()
