import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class macd_pattern_trader_all_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_all_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 24)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 13)
        self._stop_loss_bars = self.Param("StopLossBars", 22)
        self._take_profit_bars = self.Param("TakeProfitBars", 32)
        self._offset_points = self.Param("OffsetPoints", 40)
        self._ratio_threshold = self.Param("RatioThreshold", 8.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._macd_prev = 0.0
        self._macd_prev2 = 0.0
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @FastEmaPeriod.setter
    def FastEmaPeriod(self, value):
        self._fast_ema_period.Value = value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @SlowEmaPeriod.setter
    def SlowEmaPeriod(self, value):
        self._slow_ema_period.Value = value

    @property
    def StopLossBars(self):
        return self._stop_loss_bars.Value

    @StopLossBars.setter
    def StopLossBars(self, value):
        self._stop_loss_bars.Value = value

    @property
    def TakeProfitBars(self):
        return self._take_profit_bars.Value

    @TakeProfitBars.setter
    def TakeProfitBars(self, value):
        self._take_profit_bars.Value = value

    @property
    def OffsetPoints(self):
        return self._offset_points.Value

    @OffsetPoints.setter
    def OffsetPoints(self, value):
        self._offset_points.Value = value

    @property
    def RatioThreshold(self):
        return self._ratio_threshold.Value

    @RatioThreshold.setter
    def RatioThreshold(self, value):
        self._ratio_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(macd_pattern_trader_all_strategy, self).OnStarted(time)

        self._macd_prev = 0.0
        self._macd_prev2 = 0.0
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastEmaPeriod
        macd.LongMa.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_curr = float(macd_value)
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        offset = float(self.OffsetPoints) * price_step
        stop_distance = offset * 2.0
        take_distance = offset * 4.0

        if self.Position > 0:
            if low <= self._stop_loss_price or high >= self._take_profit_price:
                self.SellMarket()
                self._stop_loss_price = 0.0
                self._take_profit_price = 0.0
        elif self.Position < 0:
            if high >= self._stop_loss_price or low <= self._take_profit_price:
                self.BuyMarket()
                self._stop_loss_price = 0.0
                self._take_profit_price = 0.0

        macd_last = self._macd_prev
        macd_last3 = self._macd_prev2
        ratio_thresh = float(self.RatioThreshold)

        if macd_last != 0.0:
            ratio1 = abs(macd_last3 / macd_last)
            ratio2 = abs(macd_curr / macd_last)

            if (macd_last3 > 0.0 or macd_curr < 0.0) and ratio1 >= ratio_thresh and ratio2 >= ratio_thresh and self.Position >= 0:
                sl = close + stop_distance
                tp = close - take_distance
                self.SellMarket()
                self._stop_loss_price = sl
                self._take_profit_price = tp
            elif (macd_last3 < 0.0 or macd_curr > 0.0) and ratio1 >= ratio_thresh and ratio2 >= ratio_thresh and self.Position <= 0:
                sl = close - stop_distance
                tp = close + take_distance
                self.BuyMarket()
                self._stop_loss_price = sl
                self._take_profit_price = tp

        self._macd_prev2 = self._macd_prev
        self._macd_prev = macd_curr

    def OnReseted(self):
        super(macd_pattern_trader_all_strategy, self).OnReseted()
        self._macd_prev = 0.0
        self._macd_prev2 = 0.0
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    def CreateClone(self):
        return macd_pattern_trader_all_strategy()
