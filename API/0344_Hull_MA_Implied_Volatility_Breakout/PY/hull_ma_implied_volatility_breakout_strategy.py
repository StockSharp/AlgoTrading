import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class hull_ma_implied_volatility_breakout_strategy(Strategy):
    """
    Hull MA with Implied Volatility Breakout strategy.
    """

    def __init__(self):
        super(hull_ma_implied_volatility_breakout_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Period", "Hull Moving Average period", "HMA Settings")

        self._iv_period = self.Param("IVPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("IV Period", "Implied Volatility averaging period", "Volatility Settings")

        self._iv_multiplier = self.Param("IVMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("IV StdDev Multiplier", "Multiplier for IV standard deviation", "Volatility Settings")

        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._is_long = False
        self._is_short = False
        self._iv_history = []
        self._iv_average = 0.0
        self._iv_std_dev = 0.0
        self._current_iv = 0.0
        self._prev_hma = 0.0
        self._current_atr = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(hull_ma_implied_volatility_breakout_strategy, self).OnReseted()
        self._is_long = False
        self._is_short = False
        self._iv_history = []
        self._iv_average = 0.0
        self._iv_std_dev = 0.0
        self._current_iv = 0.0
        self._prev_hma = 0.0
        self._current_atr = 0.0

    def OnStarted(self, time):
        super(hull_ma_implied_volatility_breakout_strategy, self).OnStarted(time)

        hma = HullMovingAverage()
        hma.Length = int(self._hma_period.Value)
        atr = AverageTrueRange()
        atr.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, hma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hma_value = float(hma_val)
        atr_value = float(atr_val)
        self._current_atr = atr_value

        self.UpdateImpliedVolatility(candle)

        if self._prev_hma == 0.0:
            self._prev_hma = hma_value
            return

        price = float(candle.ClosePrice)
        hma_rising = hma_value > self._prev_hma
        hma_falling = hma_value < self._prev_hma

        iv_mult = float(self._iv_multiplier.Value)
        iv_threshold = self._iv_average + iv_mult * self._iv_std_dev
        iv_breakout = self._current_iv > iv_threshold

        if hma_rising and iv_breakout and not self._is_long and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self._is_long = True
            self._is_short = False
        elif hma_falling and iv_breakout and not self._is_short and self.Position >= 0:
            self.SellMarket(self.Volume)
            self._is_short = True
            self._is_long = False

        if self._is_long and hma_falling and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._is_long = False
        elif self._is_short and hma_rising and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._is_short = False

        self.ApplyAtrStopLoss(price)
        self._prev_hma = hma_value

    def UpdateImpliedVolatility(self, candle):
        range_val = float((candle.HighPrice - candle.LowPrice) / candle.LowPrice)
        vol = float(candle.TotalVolume) if candle.TotalVolume > 0 else 1.0

        iv = float(range_val * 100.0)
        iv *= min(1.5, 1.0 + Math.Log10(vol) * 0.1)

        self._current_iv = iv
        self._iv_history.append(iv)
        period = int(self._iv_period.Value)
        if len(self._iv_history) > period:
            self._iv_history.pop(0)

        n = len(self._iv_history)
        if n > 0:
            total = 0.0
            for v in self._iv_history:
                total += v
            self._iv_average = total / n
        else:
            self._iv_average = 0.0

        if n > 1:
            sq_sum = 0.0
            for v in self._iv_history:
                diff = v - self._iv_average
                sq_sum += diff * diff
            self._iv_std_dev = Math.Sqrt(sq_sum / (n - 1))
        else:
            self._iv_std_dev = 0.5

        self.LogInfo("IV: {0}, Avg: {1}, StdDev: {2}".format(self._current_iv, self._iv_average, self._iv_std_dev))

    def ApplyAtrStopLoss(self, price):
        if self._current_atr <= 0 or self.Position == 0:
            return

        sl_mult = float(self._stop_loss_atr.Value)
        if self.Position > 0:
            stop_level = price - sl_mult * self._current_atr
            if price <= stop_level:
                self.SellMarket(Math.Abs(self.Position))
                self._is_long = False
        elif self.Position < 0:
            stop_level = price + sl_mult * self._current_atr
            if price >= stop_level:
                self.BuyMarket(Math.Abs(self.Position))
                self._is_short = False

    def CreateClone(self):
        return hull_ma_implied_volatility_breakout_strategy()
