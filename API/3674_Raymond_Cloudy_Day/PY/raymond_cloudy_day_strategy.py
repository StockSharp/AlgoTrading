import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class raymond_cloudy_day_strategy(Strategy):
    def __init__(self):
        super(raymond_cloudy_day_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._protective_offset_ticks = self.Param("ProtectiveOffsetTicks", 500)
        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._pivot_candle_type = self.Param("PivotCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._trade_session_level = None
        self._extended_buy_level = None
        self._extended_sell_level = None
        self._take_profit_buy_level = None
        self._take_profit_sell_level = None
        self._take_profit_buy_level2 = None
        self._take_profit_sell_level2 = None

        self._entry_price = None
        self._take_price = None
        self._stop_price = None

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def ProtectiveOffsetTicks(self):
        return self._protective_offset_ticks.Value

    @ProtectiveOffsetTicks.setter
    def ProtectiveOffsetTicks(self, value):
        self._protective_offset_ticks.Value = value

    @property
    def SignalCandleType(self):
        return self._signal_candle_type.Value

    @SignalCandleType.setter
    def SignalCandleType(self, value):
        self._signal_candle_type.Value = value

    @property
    def PivotCandleType(self):
        return self._pivot_candle_type.Value

    @PivotCandleType.setter
    def PivotCandleType(self, value):
        self._pivot_candle_type.Value = value

    def OnReseted(self):
        super(raymond_cloudy_day_strategy, self).OnReseted()
        self._trade_session_level = None
        self._extended_buy_level = None
        self._extended_sell_level = None
        self._take_profit_buy_level = None
        self._take_profit_sell_level = None
        self._take_profit_buy_level2 = None
        self._take_profit_sell_level2 = None
        self._reset_protection()

    def _reset_protection(self):
        self._entry_price = None
        self._take_price = None
        self._stop_price = None

    def OnStarted2(self, time):
        super(raymond_cloudy_day_strategy, self).OnStarted2(time)

        self.Volume = float(self.TradeVolume)

        subscription = self.SubscribeCandles(self.SignalCandleType)
        subscription.Bind(self._process_both_candle).Start()

    def _process_both_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._process_pivot_candle(candle)
        self._process_signal_candle(candle)

    def _process_pivot_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        trade_session = (high + low + open_p + close) / 4.0
        pivot_range = high - low

        self._trade_session_level = trade_session
        self._extended_buy_level = trade_session + 0.382 * pivot_range
        self._extended_sell_level = trade_session - 0.382 * pivot_range
        self._take_profit_buy_level = trade_session + 0.618 * pivot_range
        self._take_profit_sell_level = trade_session - 0.618 * pivot_range
        self._take_profit_buy_level2 = trade_session + pivot_range
        self._take_profit_sell_level2 = trade_session - pivot_range

    def _process_signal_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_open_position(candle)

        if self._take_profit_sell_level is None:
            return

        trigger_level = self._take_profit_sell_level

        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position <= 0 and low < trigger_level and close > trigger_level:
            self._enter_long(close)
        elif self.Position >= 0 and low > trigger_level and close < trigger_level:
            self._enter_short(close)

    def _enter_long(self, close_price):
        price_step = 1.0
        if self.Security is not None and float(self.Security.PriceStep or 0) > 0:
            price_step = float(self.Security.PriceStep)

        volume = float(self.TradeVolume) + max(0.0, -float(self.Position))
        self.BuyMarket(volume)

        offset = price_step * float(self.ProtectiveOffsetTicks)
        self._entry_price = close_price
        self._take_price = close_price + offset
        self._stop_price = close_price - offset

    def _enter_short(self, close_price):
        price_step = 1.0
        if self.Security is not None and float(self.Security.PriceStep or 0) > 0:
            price_step = float(self.Security.PriceStep)

        volume = float(self.TradeVolume) + max(0.0, float(self.Position))
        self.SellMarket(volume)

        offset = price_step * float(self.ProtectiveOffsetTicks)
        self._entry_price = close_price
        self._take_price = close_price - offset
        self._stop_price = close_price + offset

    def _manage_open_position(self, candle):
        if self.Position == 0:
            self._reset_protection()
            return

        if self._entry_price is None or self._take_price is None or self._stop_price is None:
            return

        entry = self._entry_price
        take = self._take_price
        stop = self._stop_price

        if self.Position > 0:
            if float(candle.LowPrice) <= stop:
                self.SellMarket(float(self.Position))
                self._reset_protection()
                return
            if float(candle.HighPrice) >= take:
                self.SellMarket(float(self.Position))
                self._reset_protection()
                return
        else:
            volume = abs(float(self.Position))
            if float(candle.HighPrice) >= stop:
                self.BuyMarket(volume)
                self._reset_protection()
                return
            if float(candle.LowPrice) <= take:
                self.BuyMarket(volume)
                self._reset_protection()

    def CreateClone(self):
        return raymond_cloudy_day_strategy()
