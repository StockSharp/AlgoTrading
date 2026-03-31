import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AccumulationDistributionLine
from StockSharp.Algo.Strategies import Strategy


class spectr_analysis_chaikin_strategy(Strategy):
    def __init__(self):
        super(spectr_analysis_chaikin_strategy, self).__init__()
        self._fast_ma_period = self.Param("FastMaPeriod", 3) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")
        self._slow_ma_period = self.Param("SlowMaPeriod", 10) \
            .SetDisplay("Slow MA", "Slow EMA period", "Indicator")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Position Open", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Position Open", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Position Close", "Allow closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Position Close", "Allow closing short positions", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Data")
        self._bar_count = 0
        self._fast_ema = None
        self._slow_ema = None
        self._prev = None
        self._prev2 = None

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(spectr_analysis_chaikin_strategy, self).OnReseted()
        self._bar_count = 0
        self._fast_ema = None
        self._slow_ema = None
        self._prev = None
        self._prev2 = None

    def OnStarted2(self, time):
        super(spectr_analysis_chaikin_strategy, self).OnStarted2(time)
        self._bar_count = 0
        self._fast_ema = None
        self._slow_ema = None
        self._prev = None
        self._prev2 = None
        ad = AccumulationDistributionLine()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ad, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ad)
            self.DrawOwnTrades(area)

    def _update_ema(self, current, value, length):
        if current is None:
            return value
        multiplier = 2.0 / (length + 1)
        return current + ((value - current) * multiplier)

    def _update_history(self, oscillator):
        self._prev2 = self._prev
        self._prev = oscillator

    def process_candle(self, candle, ad_value):
        if candle.State != CandleStates.Finished:
            return
        ad_value = float(ad_value)
        self._bar_count += 1
        fast_period = int(self.fast_ma_period)
        slow_period = int(self.slow_ma_period)
        self._fast_ema = self._update_ema(self._fast_ema, ad_value, fast_period)
        self._slow_ema = self._update_ema(self._slow_ema, ad_value, slow_period)
        if self._fast_ema is None or self._slow_ema is None:
            return
        oscillator = self._fast_ema - self._slow_ema
        if self._bar_count < slow_period:
            self._update_history(oscillator)
            return
        if self._prev is not None and self._prev2 is not None:
            if self._prev < self._prev2 and oscillator >= self._prev and oscillator > 0:
                if self.buy_pos_open and self.Position <= 0:
                    self.BuyMarket()
                elif self.sell_pos_close and self.Position < 0:
                    self.BuyMarket()
            elif self._prev > self._prev2 and oscillator <= self._prev and oscillator < 0:
                if self.sell_pos_open and self.Position >= 0:
                    self.SellMarket()
                elif self.buy_pos_close and self.Position > 0:
                    self.SellMarket()
        self._update_history(oscillator)

    def CreateClone(self):
        return spectr_analysis_chaikin_strategy()
