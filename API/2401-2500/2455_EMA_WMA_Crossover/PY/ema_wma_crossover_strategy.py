import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ema_wma_crossover_strategy(Strategy):
    """
    EMA and WMA crossover strategy with fixed risk management.
    Goes long when EMA crosses below WMA and short when EMA crosses above WMA.
    Uses StartProtection for TP/SL.
    """

    def __init__(self):
        super(ema_wma_crossover_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 34) \
            .SetDisplay("EMA Period", "EMA period length", "Indicators")
        self._wma_period = self.Param("WmaPeriod", 13) \
            .SetDisplay("WMA Period", "WMA period length", "Indicators")
        self._stop_loss_ticks = self.Param("StopLossTicks", 50) \
            .SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 50) \
            .SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_ema = 0.0
        self._prev_wma = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_wma_crossover_strategy, self).OnReseted()
        self._has_prev = False
        self._prev_ema = 0.0
        self._prev_wma = 0.0

    def OnStarted2(self, time):
        super(ema_wma_crossover_strategy, self).OnStarted2(time)

        tick = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            tick = float(self.Security.PriceStep)
        if tick <= 0:
            tick = 1.0

        sl_dist = self._stop_loss_ticks.Value * tick
        tp_dist = self._take_profit_ticks.Value * tick

        self.StartProtection(
            Unit(float(tp_dist), UnitTypes.Absolute),
            Unit(float(sl_dist), UnitTypes.Absolute)
        )

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        wma = WeightedMovingAverage()
        wma.Length = self._wma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, wma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, wma_val):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_val)
        wma_val = float(wma_val)

        if not self._has_prev:
            self._prev_ema = ema_val
            self._prev_wma = wma_val
            self._has_prev = True
            return

        cross_down = ema_val < wma_val and self._prev_ema > self._prev_wma
        cross_up = ema_val > wma_val and self._prev_ema < self._prev_wma

        if cross_down and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_up and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_ema = ema_val
        self._prev_wma = wma_val

    def CreateClone(self):
        return ema_wma_crossover_strategy()
