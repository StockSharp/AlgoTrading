import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_breakout_momentum_strategy(Strategy):
    def __init__(self):
        super(bollinger_breakout_momentum_strategy, self).__init__()
        self._bollinger_length = self.Param("BollingerLength", 18) \
            .SetDisplay("BB Length", "Bollinger Bands length", "Parameters")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Bollinger Bands deviation", "Parameters")
        self._take_profit_pips = self.Param("TakeProfitPips", 200) \
            .SetDisplay("Take Profit (pips)", "Distance for profit target", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of working candles", "General")
        self._breakout_percent = self.Param("BreakoutPercent", 0.002) \
            .SetDisplay("Breakout %", "Minimum breakout beyond the Bollinger boundary", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_upper = None
        self._prev_lower = None
        self._prev_middle = None
        self._cooldown_remaining = 0

    @property
    def bollinger_length(self):
        return self._bollinger_length.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def take_profit_pips(self):
        return self._take_profit_pips.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def breakout_percent(self):
        return self._breakout_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_breakout_momentum_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_upper = None
        self._prev_lower = None
        self._prev_middle = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_breakout_momentum_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_length
        bollinger.Width = self.bollinger_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished or not bb_value.IsFinal:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        middle = bb_value.MovingAverage
        if upper is None or lower is None or middle is None:
            return
        upper = float(upper)
        lower = float(lower)
        middle = float(middle)

        step = self.Security.PriceStep
        step = float(step) if step is not None else 1.0
        price = float(candle.ClosePrice)

        if self.Position > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            else:
                self._stop_price = max(self._stop_price, middle)
        elif self.Position < 0:
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            else:
                self._stop_price = min(self._stop_price, middle)
        elif self._prev_upper is not None and self._prev_lower is not None and self._prev_middle is not None and self._cooldown_remaining == 0:
            upper_rising = upper > self._prev_upper and middle > self._prev_middle
            lower_falling = lower < self._prev_lower and middle < self._prev_middle
            buy_signal = upper_rising and price > upper * (1.0 + float(self.breakout_percent))
            sell_signal = lower_falling and price < lower * (1.0 - float(self.breakout_percent))

            if buy_signal:
                self.BuyMarket()
                self._stop_price = middle
                self._take_price = price + float(self.take_profit_pips) * step
                self._cooldown_remaining = self.cooldown_bars
            elif sell_signal:
                self.SellMarket()
                self._stop_price = middle
                self._take_price = price - float(self.take_profit_pips) * step
                self._cooldown_remaining = self.cooldown_bars

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_middle = middle

    def CreateClone(self):
        return bollinger_breakout_momentum_strategy()
