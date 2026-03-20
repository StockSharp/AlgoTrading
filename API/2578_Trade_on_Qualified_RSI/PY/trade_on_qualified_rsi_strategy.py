import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class trade_on_qualified_rsi_strategy(Strategy):
    def __init__(self):
        super(trade_on_qualified_rsi_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 28)
        self._upper_threshold = self.Param("UpperThreshold", 65.0)
        self._lower_threshold = self.Param("LowerThreshold", 35.0)
        self._count_bars = self.Param("CountBars", 8)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._stop_price = None
        self._entry_price = 0.0
        self._above_counter = 0
        self._below_counter = 0

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def UpperThreshold(self):
        return self._upper_threshold.Value

    @UpperThreshold.setter
    def UpperThreshold(self, value):
        self._upper_threshold.Value = value

    @property
    def LowerThreshold(self):
        return self._lower_threshold.Value

    @LowerThreshold.setter
    def LowerThreshold(self, value):
        self._lower_threshold.Value = value

    @property
    def CountBars(self):
        return self._count_bars.Value

    @CountBars.setter
    def CountBars(self, value):
        self._count_bars.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _calculate_stop_distance(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        return int(self.StopLossPoints) * step

    def OnStarted(self, time):
        super(trade_on_qualified_rsi_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._stop_price = None
        self._entry_price = 0.0
        self._above_counter = 0
        self._below_counter = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        if not self._rsi.IsFormed:
            self._above_counter = 0
            self._below_counter = 0
            return

        distance = self._calculate_stop_distance()
        if distance <= 0.0:
            return

        self._update_counters(rsi_val)

        required_bars = int(self.CountBars) + 1
        upper = float(self.UpperThreshold)
        lower = float(self.LowerThreshold)
        close = float(candle.ClosePrice)

        if self.Position == 0:
            self._stop_price = None
            self._entry_price = 0.0

            short_signal = rsi_val >= upper and self._above_counter >= required_bars
            long_signal = rsi_val <= lower and self._below_counter >= required_bars

            if short_signal:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + distance
                return

            if long_signal:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - distance
            return

        if self.Position > 0:
            if self._stop_price is None:
                self._stop_price = self._entry_price - distance

            new_stop = close - distance
            if self._stop_price is None or new_stop > self._stop_price:
                self._stop_price = new_stop

            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._stop_price = None
                self._entry_price = 0.0
            return

        if self.Position < 0:
            if self._stop_price is None:
                self._stop_price = self._entry_price + distance

            new_stop = close + distance
            if self._stop_price is None or new_stop < self._stop_price:
                self._stop_price = new_stop

            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._stop_price = None
                self._entry_price = 0.0

    def _update_counters(self, rsi_val):
        upper = float(self.UpperThreshold)
        lower = float(self.LowerThreshold)

        if rsi_val >= upper:
            self._above_counter += 1
        else:
            self._above_counter = 0

        if rsi_val <= lower:
            self._below_counter += 1
        else:
            self._below_counter = 0

    def OnReseted(self):
        super(trade_on_qualified_rsi_strategy, self).OnReseted()
        self._stop_price = None
        self._entry_price = 0.0
        self._above_counter = 0
        self._below_counter = 0

    def CreateClone(self):
        return trade_on_qualified_rsi_strategy()
