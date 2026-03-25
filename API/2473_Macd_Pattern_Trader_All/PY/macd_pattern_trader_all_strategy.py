import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
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
        macd.ShortMa.Length = int(self._fast_ema_period.Value)
        macd.LongMa.Length = int(self._slow_ema_period.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_curr = float(macd_value)
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        pos = float(self.Position)

        # Manage existing position
        if pos > 0:
            if low <= self._stop_loss_price or high >= self._take_profit_price:
                self.SellMarket(pos)
                self._stop_loss_price = 0.0
                self._take_profit_price = 0.0
        elif pos < 0:
            if high >= self._stop_loss_price or low <= self._take_profit_price:
                self.BuyMarket(abs(pos))
                self._stop_loss_price = 0.0
                self._take_profit_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._macd_prev2 = self._macd_prev
            self._macd_prev = macd_curr
            return

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        offset = float(self._offset_points.Value) * price_step
        stop_distance = offset * 2.0
        take_distance = offset * 4.0

        macd_last = self._macd_prev
        macd_last3 = self._macd_prev2
        ratio_thresh = float(self._ratio_threshold.Value)

        if macd_last != 0.0:
            ratio1 = abs(macd_last3 / macd_last)
            ratio2 = abs(macd_curr / macd_last)

            pos = float(self.Position)
            vol = float(self.Volume)

            if (macd_last3 > 0.0 or macd_curr < 0.0) and ratio1 >= ratio_thresh and ratio2 >= ratio_thresh and pos >= 0:
                sl = close + stop_distance
                tp = close - take_distance
                volume = vol + abs(pos)
                self.SellMarket(volume)
                self._stop_loss_price = sl
                self._take_profit_price = tp
            elif (macd_last3 < 0.0 or macd_curr > 0.0) and ratio1 >= ratio_thresh and ratio2 >= ratio_thresh and pos <= 0:
                sl = close - stop_distance
                tp = close + take_distance
                volume = vol + abs(pos)
                self.BuyMarket(volume)
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
