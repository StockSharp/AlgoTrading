import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class channel_ea2_strategy(Strategy):
    def __init__(self):
        super(channel_ea2_strategy, self).__init__()

        self._begin_hour = self.Param("BeginHour", 1)
        self._end_hour = self.Param("EndHour", 10)
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._stop_buffer_multiplier = self.Param("StopBufferMultiplier", 2.0)

        self._session_high = None
        self._session_low = None
        self._channel_ready = False
        self._entry_price = None
        self._stop_loss_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(channel_ea2_strategy, self).OnStarted(time)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour

        if hour >= self._begin_hour.Value and hour < self._end_hour.Value:
            h = float(candle.HighPrice)
            lo = float(candle.LowPrice)
            if self._session_high is None or h > self._session_high:
                self._session_high = h
            if self._session_low is None or lo < self._session_low:
                self._session_low = lo
            self._channel_ready = True
            return

        if not self._channel_ready or self._session_high is None or self._session_low is None:
            return

        high = self._session_high
        low = self._session_low
        if high <= low:
            return

        buffer = self._get_price_buffer()

        if self.Position > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket(self.Position)
                self._entry_price = None
                self._stop_loss_price = None
            return
        elif self.Position < 0:
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket(abs(self.Position))
                self._entry_price = None
                self._stop_loss_price = None
            return

        if float(candle.HighPrice) > high + buffer:
            vol = self._trade_volume.Value if self._trade_volume.Value > 0 else float(self.Volume)
            self.BuyMarket(vol)
            self._entry_price = float(candle.ClosePrice)
            self._stop_loss_price = low - buffer
            self._session_high = None
            self._session_low = None
            self._channel_ready = False
            return

        if float(candle.LowPrice) < low - buffer:
            vol = self._trade_volume.Value if self._trade_volume.Value > 0 else float(self.Volume)
            self.SellMarket(vol)
            self._entry_price = float(candle.ClosePrice)
            self._stop_loss_price = high + buffer
            self._session_high = None
            self._session_low = None
            self._channel_ready = False

    def _get_price_buffer(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0 or self._stop_buffer_multiplier.Value <= 0:
            return 0.0
        return step * self._stop_buffer_multiplier.Value

    def OnReseted(self):
        super(channel_ea2_strategy, self).OnReseted()
        self._session_high = None
        self._session_low = None
        self._channel_ready = False
        self._entry_price = None
        self._stop_loss_price = None

    def CreateClone(self):
        return channel_ea2_strategy()
