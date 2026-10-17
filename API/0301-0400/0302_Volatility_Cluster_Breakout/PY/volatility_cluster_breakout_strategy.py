import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class volatility_cluster_breakout_strategy(Strategy):
    """
    Breakout strategy that trades only when ATR expands into a high-volatility cluster.
    """

    def __init__(self):
        super(volatility_cluster_breakout_strategy, self).__init__()

        self._price_avg_period = self.Param("PriceAvgPeriod", 20) \
            .SetDisplay("Price Average Period", "Period for moving average and standard deviation", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")

        self._std_dev_multiplier = self.Param("StdDevMultiplier", 2.0) \
            .SetDisplay("StdDev Multiplier", "Multiplier for breakout levels", "Signals")

        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetDisplay("Stop ATR Multiplier", "ATR multiplier used for stop distance", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._sma = None
        self._std_dev = None
        self._atr = None
        self._atr_avg = None
        self._stop_price = 0.0
        self._pending_order = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_cluster_breakout_strategy, self).OnReseted()
        self._sma = None
        self._std_dev = None
        self._atr = None
        self._atr_avg = None
        self._stop_price = 0.0
        self._pending_order = None
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volatility_cluster_breakout_strategy, self).OnStarted2(time)

        atr_period = int(self._atr_period.Value)
        price_period = int(self._price_avg_period.Value)

        self._sma = SimpleMovingAverage()
        self._sma.Length = price_period
        self._std_dev = StandardDeviation()
        self._std_dev.Length = price_period
        self._atr = AverageTrueRange()
        self._atr.Length = atr_period
        self._atr_avg = SimpleMovingAverage()
        self._atr_avg.Length = max(atr_period * 2, 10)
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._std_dev, self._atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    @property
    def _is_order_pending(self):
        return self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_candle(self, candle, sma_value, std_dev_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_value)
        atr_avg_value = float(process_float(self._atr_avg, Decimal(av), candle.OpenTime, True))

        if not self._sma.IsFormed or not self._std_dev.IsFormed or not self._atr.IsFormed or not self._atr_avg.IsFormed:
            return

        is_cooling_down = self._cooldown > 0
        if is_cooling_down:
            self._cooldown -= 1

        if not self.IsFormedAndOnlineAndAllowTrading() or self._is_order_pending:
            return

        sv = float(sma_value)
        sdv = float(std_dev_value)
        sdm = float(self._std_dev_multiplier.Value)
        upper_level = sv + sdm * sdv
        lower_level = sv - sdm * sdv
        is_high_volatility = av >= atr_avg_value * 1.15
        price = float(candle.ClosePrice)
        cd = int(self._cooldown_bars.Value)
        sm = float(self._stop_multiplier.Value)

        if self.Position == 0:
            if is_cooling_down or not is_high_volatility or av <= 0:
                return

            if price >= upper_level:
                self._stop_price = price - av * sm
                self._pending_order = self.BuyMarket()
                self._cooldown = cd
            elif price <= lower_level:
                self._stop_price = price + av * sm
                self._pending_order = self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0:
            if price <= sv or not is_high_volatility or price <= self._stop_price:
                self._pending_order = self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if price >= sv or not is_high_volatility or price >= self._stop_price:
                self._pending_order = self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

    def CreateClone(self):
        return volatility_cluster_breakout_strategy()
