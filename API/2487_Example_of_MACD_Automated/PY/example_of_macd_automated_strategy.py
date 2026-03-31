import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class example_of_macd_automated_strategy(Strategy):
    def __init__(self):
        super(example_of_macd_automated_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 1.0)
        self._stop_loss_points = self.Param("StopLossPoints", 50.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 30.0)
        self._macd_fast_length = self.Param("MacdFastLength", 12)
        self._macd_slow_length = self.Param("MacdSlowLength", 26)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._last_entry_macd = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_direction = 0

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def MacdFastLength(self):
        return self._macd_fast_length.Value

    @MacdFastLength.setter
    def MacdFastLength(self, value):
        self._macd_fast_length.Value = value

    @property
    def MacdSlowLength(self):
        return self._macd_slow_length.Value

    @MacdSlowLength.setter
    def MacdSlowLength(self, value):
        self._macd_slow_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(example_of_macd_automated_strategy, self).OnStarted2(time)

        self._last_entry_macd = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_direction = 0

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.MacdFastLength
        macd.LongMa.Length = self.MacdSlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        current_macd = float(macd_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._handle_protection(candle):
            self._last_entry_macd = current_macd
            return

        if self.Position != 0:
            self._last_entry_macd = current_macd
            return

        if self._last_entry_macd is None:
            self._last_entry_macd = current_macd
            return

        prev_macd = self._last_entry_macd

        if prev_macd <= 0.0 and current_macd > 0.0:
            self._enter_position(close, True)
        elif prev_macd >= 0.0 and current_macd < 0.0:
            self._enter_position(close, False)

        self._last_entry_macd = current_macd

    def _enter_position(self, price, is_long):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        sl_pts = float(self.StopLossPoints)
        tp_pts = float(self.TakeProfitPoints)

        if is_long:
            self.BuyMarket()
            self._entry_price = price
            self._entry_direction = 1
            self._stop_price = price - sl_pts * step if sl_pts > 0.0 else None
            self._take_profit_price = price + tp_pts * step if tp_pts > 0.0 else None
        else:
            self.SellMarket()
            self._entry_price = price
            self._entry_direction = -1
            self._stop_price = price + sl_pts * step if sl_pts > 0.0 else None
            self._take_profit_price = price - tp_pts * step if tp_pts > 0.0 else None

    def _handle_protection(self, candle):
        if self.Position == 0 or self._entry_direction == 0:
            return False

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._entry_direction > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_state()
                return True
            if self._take_profit_price is not None and high >= self._take_profit_price:
                self.SellMarket()
                self._reset_state()
                return True
        else:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_state()
                return True
            if self._take_profit_price is not None and low <= self._take_profit_price:
                self.BuyMarket()
                self._reset_state()
                return True

        return False

    def _reset_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_direction = 0

    def OnReseted(self):
        super(example_of_macd_automated_strategy, self).OnReseted()
        self._last_entry_macd = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_direction = 0

    def CreateClone(self):
        return example_of_macd_automated_strategy()
