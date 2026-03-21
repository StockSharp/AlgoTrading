import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class exp_fisher_cg_oscillator_strategy(Strategy):
    def __init__(self):
        super(exp_fisher_cg_oscillator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._median_prices = []
        self._cg_values = []
        self._value_buffer = [0.0, 0.0, 0.0, 0.0]
        self._value_count = 0
        self._previous_fisher = None
        self._oscillator_history = []
        self._entry_price = None
        self._length = 10

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_fisher_cg_oscillator_strategy, self).OnReseted()
        self._median_prices = []
        self._cg_values = []
        self._value_buffer = [0.0, 0.0, 0.0, 0.0]
        self._value_count = 0
        self._previous_fisher = None
        self._oscillator_history = []
        self._entry_price = None

    def OnStarted(self, time):
        super(exp_fisher_cg_oscillator_strategy, self).OnStarted(time)
        self._median_prices = []
        self._cg_values = []
        self._value_buffer = [0.0, 0.0, 0.0, 0.0]
        self._value_count = 0
        self._previous_fisher = None
        self._oscillator_history = []
        self._entry_price = None
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        self._median_prices.append(price)
        while len(self._median_prices) > self._length:
            self._median_prices.pop(0)

        if len(self._median_prices) < self._length:
            return

        num = 0.0
        denom = 0.0
        weight = 1
        for i in range(len(self._median_prices) - 1, -1, -1):
            median = self._median_prices[i]
            num += weight * median
            denom += median
            weight += 1

        if denom != 0.0:
            cg = -num / denom + (self._length + 1.0) / 2.0
        else:
            cg = 0.0

        self._cg_values.append(cg)
        while len(self._cg_values) > self._length:
            self._cg_values.pop(0)

        high = cg
        low = cg
        for v in self._cg_values:
            if v > high:
                high = v
            if v < low:
                low = v

        if high != low:
            normalized = (cg - low) / (high - low)
        else:
            normalized = 0.0

        limit = min(self._value_count, 3)
        shift = limit
        while shift > 0:
            self._value_buffer[shift] = self._value_buffer[shift - 1]
            shift -= 1

        self._value_buffer[0] = normalized
        if self._value_count < 4:
            self._value_count += 1

        if self._value_count < 4:
            return

        value2 = (4.0 * self._value_buffer[0] + 3.0 * self._value_buffer[1] + 2.0 * self._value_buffer[2] + self._value_buffer[3]) / 10.0
        x = 1.98 * (value2 - 0.5)
        if x > 0.999:
            x = 0.999
        elif x < -0.999:
            x = -0.999

        numerator = 1.0 + x
        denominator = 1.0 - x
        if denominator == 0.0:
            denominator = 0.0000001

        ratio = numerator / denominator
        if ratio <= 0.0:
            ratio = 0.0000001

        fisher = 0.5 * math.log(ratio)
        trigger = self._previous_fisher if self._previous_fisher is not None else fisher
        self._previous_fisher = fisher

        self._oscillator_history.append((fisher, trigger))
        while len(self._oscillator_history) > 10:
            self._oscillator_history.pop(0)

        if len(self._oscillator_history) < 3:
            return

        self._handle_risk_management(float(candle.ClosePrice))

        current = self._oscillator_history[-1]
        previous = self._oscillator_history[-2]

        previous_above = previous[0] > previous[1]
        previous_below = previous[0] < previous[1]

        buy_open = previous_above and current[0] <= current[1]
        sell_open = previous_below and current[0] >= current[1]

        if previous_above and self.Position < 0:
            self.BuyMarket()
            self._entry_price = None

        if previous_below and self.Position > 0:
            self.SellMarket()
            self._entry_price = None

        if buy_open and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                self._entry_price = None
                return
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
        elif sell_open and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                self._entry_price = None
                return
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)

    def _handle_risk_management(self, close_price):
        if self._entry_price is None or self.Position == 0:
            return

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        stop_distance = 1000 * step
        take_distance = 2000 * step

        if self.Position > 0:
            if close_price <= self._entry_price - stop_distance:
                self.SellMarket()
                self._entry_price = None
                return
            if close_price >= self._entry_price + take_distance:
                self.SellMarket()
                self._entry_price = None
        elif self.Position < 0:
            if close_price >= self._entry_price + stop_distance:
                self.BuyMarket()
                self._entry_price = None
                return
            if close_price <= self._entry_price - take_distance:
                self.BuyMarket()
                self._entry_price = None

    def CreateClone(self):
        return exp_fisher_cg_oscillator_strategy()
