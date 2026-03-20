import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


SIGNAL_POINT = 0
SIGNAL_DIRECT = 1
SIGNAL_WITHOUT = 2


class color_j_fatl_st_dev_strategy(Strategy):
    def __init__(self):
        super(color_j_fatl_st_dev_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "JMA period", "Indicators")
        self._jma_phase = self.Param("JmaPhase", -100) \
            .SetDisplay("JMA Phase", "JMA phase", "Indicators")
        self._std_period = self.Param("StdPeriod", 9) \
            .SetDisplay("Std Period", "Standard deviation period", "Indicators")
        self._k1 = self.Param("K1", 0.5) \
            .SetDisplay("K1", "First deviation multiplier", "Parameters")
        self._k2 = self.Param("K2", 1.0) \
            .SetDisplay("K2", "Second deviation multiplier", "Parameters")
        self._buy_open_mode = self.Param("BuyOpenMode", SIGNAL_POINT) \
            .SetDisplay("Buy Open", "Mode for opening long", "Signals")
        self._sell_open_mode = self.Param("SellOpenMode", SIGNAL_POINT) \
            .SetDisplay("Sell Open", "Mode for opening short", "Signals")
        self._buy_close_mode = self.Param("BuyCloseMode", SIGNAL_POINT) \
            .SetDisplay("Buy Close", "Mode for closing long", "Signals")
        self._sell_close_mode = self.Param("SellCloseMode", SIGNAL_POINT) \
            .SetDisplay("Sell Close", "Mode for closing short", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_jma = None
        self._prev_prev_jma = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def jma_phase(self):
        return self._jma_phase.Value

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def k1(self):
        return self._k1.Value

    @property
    def k2(self):
        return self._k2.Value

    @property
    def buy_open_mode(self):
        return self._buy_open_mode.Value

    @property
    def sell_open_mode(self):
        return self._sell_open_mode.Value

    @property
    def buy_close_mode(self):
        return self._buy_close_mode.Value

    @property
    def sell_close_mode(self):
        return self._sell_close_mode.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_j_fatl_st_dev_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_prev_jma = None

    def OnStarted(self, time):
        super(color_j_fatl_st_dev_strategy, self).OnStarted(time)
        self._prev_jma = None
        self._prev_prev_jma = None
        jma = JurikMovingAverage()
        jma.Length = self.jma_length
        jma.Phase = self.jma_phase
        std = StandardDeviation()
        std.Length = self.std_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, std, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, jma_value, std_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        std_value = float(std_value)
        if self._prev_jma is None or self._prev_prev_jma is None:
            self._prev_prev_jma = self._prev_jma
            self._prev_jma = jma_value
            return
        if std_value == 0:
            self._prev_prev_jma = self._prev_jma
            self._prev_jma = jma_value
            return
        k1_val = float(self.k1)
        k2_val = float(self.k2)
        upper1 = jma_value + k1_val * std_value
        upper2 = jma_value + k2_val * std_value
        lower1 = jma_value - k1_val * std_value
        lower2 = jma_value - k2_val * std_value
        close_price = float(candle.ClosePrice)
        buy_open = False
        sell_open = False
        buy_close = False
        sell_close = False
        bom = int(self.buy_open_mode)
        som = int(self.sell_open_mode)
        bcm = int(self.buy_close_mode)
        scm = int(self.sell_close_mode)
        if bom == SIGNAL_POINT:
            buy_open = close_price > upper1 or close_price > upper2
        elif bom == SIGNAL_DIRECT:
            buy_open = jma_value > self._prev_jma and self._prev_jma < self._prev_prev_jma
        if som == SIGNAL_POINT:
            sell_open = close_price < lower1 or close_price < lower2
        elif som == SIGNAL_DIRECT:
            sell_open = jma_value < self._prev_jma and self._prev_jma > self._prev_prev_jma
        if bcm == SIGNAL_POINT:
            buy_close = close_price < lower1 or close_price < lower2
        elif bcm == SIGNAL_DIRECT:
            buy_close = jma_value > self._prev_jma
        if scm == SIGNAL_POINT:
            sell_close = close_price > upper1 or close_price > upper2
        elif scm == SIGNAL_DIRECT:
            sell_close = jma_value < self._prev_jma
        if buy_close and self.Position > 0:
            self.SellMarket()
        elif sell_close and self.Position < 0:
            self.BuyMarket()
        elif buy_open and self.Position <= 0:
            self.BuyMarket()
        elif sell_open and self.Position >= 0:
            self.SellMarket()
        self._prev_prev_jma = self._prev_jma
        self._prev_jma = jma_value

    def CreateClone(self):
        return color_j_fatl_st_dev_strategy()
