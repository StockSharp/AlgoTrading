import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class risk_management_atr_strategy(Strategy):
    """ATR risk management with MA crossover, buy-only strategy with virtual stop-loss."""

    def __init__(self):
        super(risk_management_atr_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR period", "Number of candles used to smooth the ATR volatility measure", "Indicator")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR multiplier", "Distance multiplier applied to the ATR for stop-loss placement", "Risk")
        self._use_atr_stop_loss = self.Param("UseAtrStopLoss", True) \
            .SetDisplay("Use ATR stop", "Switch between ATR-based and fixed-distance stop-loss modes", "Risk")
        self._fixed_stop_loss_points = self.Param("FixedStopLossPoints", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Fixed stop (points)", "Stop-loss distance expressed in price steps when ATR mode is disabled", "Risk")
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA period", "Length of the fast moving average used for signals", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA period", "Length of the slow moving average used for signals", "Indicators")

        self._last_atr_value = None
        self._price_step = 0.0
        self._virtual_stop_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @property
    def UseAtrStopLoss(self):
        return self._use_atr_stop_loss.Value

    @property
    def FixedStopLossPoints(self):
        return self._fixed_stop_loss_points.Value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    def OnReseted(self):
        super(risk_management_atr_strategy, self).OnReseted()
        self._last_atr_value = None
        self._price_step = 0.0
        self._virtual_stop_price = None

    def OnStarted(self, time):
        super(risk_management_atr_strategy, self).OnStarted(time)

        self._price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._price_step = ps

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastMaPeriod

        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, atr_value, fast_ma_value, slow_ma_value):
        if candle.State != CandleStates.Finished:
            return

        atr_v = float(atr_value)
        fast_v = float(fast_ma_value)
        slow_v = float(slow_ma_value)

        self._last_atr_value = atr_v

        if self._virtual_stop_price is not None and self.Position > 0 and float(candle.LowPrice) <= self._virtual_stop_price:
            self.SellMarket(abs(self.Position))
            self._virtual_stop_price = None
            return

        if self.Position == 0:
            self._virtual_stop_price = None

        if fast_v <= slow_v:
            return

        if self.Position != 0:
            return

        self.BuyMarket()

        stop_distance = self._calculate_stop_distance(atr_v)
        if stop_distance > 0:
            stop_price = float(candle.ClosePrice) - stop_distance
            if stop_price > 0 and stop_price < float(candle.ClosePrice):
                self._virtual_stop_price = stop_price

    def _calculate_stop_distance(self, atr_value):
        if self.UseAtrStopLoss:
            if atr_value <= 0:
                return 0.0
            distance = atr_value * float(self.AtrMultiplier)
            return distance if distance > 0 else 0.0
        else:
            steps = self.FixedStopLossPoints
            if steps <= 0:
                return 0.0
            return steps * self._price_step

    def CreateClone(self):
        return risk_management_atr_strategy()
