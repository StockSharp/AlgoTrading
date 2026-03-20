import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class fractal_rsi_strategy(Strategy):
    def __init__(self):
        super(fractal_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._fractal_period = self.Param("FractalPeriod", 50)
        self._normal_speed = self.Param("NormalSpeed", 50)
        self._high_level = self.Param("HighLevel", 70.0)
        self._low_level = self.Param("LowLevel", 30.0)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)

        self._prices = []
        self._previous_value = None
        self._last_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FractalPeriod(self):
        return self._fractal_period.Value

    @FractalPeriod.setter
    def FractalPeriod(self, value):
        self._fractal_period.Value = value

    @property
    def NormalSpeed(self):
        return self._normal_speed.Value

    @NormalSpeed.setter
    def NormalSpeed(self, value):
        self._normal_speed.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    def OnStarted(self, time):
        super(fractal_rsi_strategy, self).OnStarted(time)

        self._prices = []
        self._previous_value = None
        self._last_signal = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def _compute_fractal_rsi(self):
        period = int(self.FractalPeriod)
        if len(self._prices) < period + 1:
            return None

        last_index = len(self._prices) - 1
        start_index = last_index - period + 1

        price_max = self._prices[start_index]
        price_min = self._prices[start_index]
        for i in range(start_index, last_index + 1):
            if self._prices[i] > price_max:
                price_max = self._prices[i]
            if self._prices[i] < price_min:
                price_min = self._prices[i]

        length = 0.0
        prior_diff = None

        if price_max - price_min > 0.0:
            for k in range(period):
                p = (self._prices[last_index - k] - price_min) / (price_max - price_min)
                if prior_diff is not None:
                    length += math.sqrt((p - prior_diff) ** 2 + 1.0 / (period * period))
                prior_diff = p

        log2 = math.log(2.0)
        if length > 0.0:
            fdi = 1.0 + (math.log(length) + log2) / math.log(2.0 * (period - 1))
        else:
            fdi = 0.0

        hurst = 2.0 - fdi
        trail_dim = 1.0 / hurst if hurst != 0.0 else 0.0
        speed = max(1, int(round(int(self.NormalSpeed) * trail_dim / 2.0)))

        if len(self._prices) <= speed:
            return None

        sum_up = 0.0
        sum_down = 0.0
        for i in range(last_index - speed + 1, last_index + 1):
            diff = self._prices[i] - self._prices[i - 1]
            if diff > 0.0:
                sum_up += diff
            else:
                sum_down -= diff

        pos = sum_up / speed
        neg = sum_down / speed

        if neg > 0.0:
            return 100.0 - (100.0 / (1.0 + pos / neg))
        return 100.0 if pos > 0.0 else 50.0

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._prices.append(float(candle.ClosePrice))
        if len(self._prices) > 500:
            self._prices.pop(0)

        value = self._compute_fractal_rsi()
        if value is None:
            return

        prev = self._previous_value
        self._previous_value = value

        if prev is None:
            return

        low_lvl = float(self.LowLevel)
        high_lvl = float(self.HighLevel)

        if prev > low_lvl and value <= low_lvl and self._last_signal != 1 and self.Position <= 0:
            self.BuyMarket()
            self._last_signal = 1
        elif prev < high_lvl and value >= high_lvl and self._last_signal != -1 and self.Position >= 0:
            self.SellMarket()
            self._last_signal = -1
        elif value > low_lvl and value < high_lvl:
            self._last_signal = 0

    def OnReseted(self):
        super(fractal_rsi_strategy, self).OnReseted()
        self._prices = []
        self._previous_value = None
        self._last_signal = 0

    def CreateClone(self):
        return fractal_rsi_strategy()
