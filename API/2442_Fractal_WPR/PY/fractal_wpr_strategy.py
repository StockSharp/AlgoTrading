import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class fractal_wpr_strategy(Strategy):
    """
    Fractal WPR: Williams %R level crossings with directional mode.
    Direct mode: buy on oversold crossing, sell on overbought.
    Against mode: reverse signals.
    Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(fractal_wpr_strategy, self).__init__()
        self._wpr_period = self.Param("WprPeriod", 30) \
            .SetDisplay("WPR Period", "Williams %R calculation period", "Indicators")
        self._high_level = self.Param("HighLevel", -30.0) \
            .SetDisplay("High Level", "Overbought threshold", "Levels")
        self._low_level = self.Param("LowLevel", -70.0) \
            .SetDisplay("Low Level", "Oversold threshold", "Levels")
        self._trend = self.Param("Trend", 0) \
            .SetDisplay("Trend Mode", "0=Direct, 1=Against", "General")
        self._stop_loss_ticks = self.Param("StopLossTicks", 1000) \
            .SetDisplay("Stop Loss", "Stop loss distance in ticks", "Protection")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 2000) \
            .SetDisplay("Take Profit", "Take profit distance in ticks", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_wpr = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fractal_wpr_strategy, self).OnReseted()
        self._prev_wpr = None

    def OnStarted(self, time):
        super(fractal_wpr_strategy, self).OnStarted(time)

        wpr = WilliamsR()
        wpr.Length = self._wpr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(wpr, self._process_candle).Start()

        ps = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 1.0

        sl_dist = ps * self._stop_loss_ticks.Value
        tp_dist = ps * self._take_profit_ticks.Value
        self.StartProtection(
            Unit(tp_dist, UnitTypes.Absolute),
            Unit(sl_dist, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        wpr = float(wpr_value.ToDecimal())

        if self._prev_wpr is not None and self.IsFormedAndOnlineAndAllowTrading():
            high_level = self._high_level.Value
            low_level = self._low_level.Value
            is_direct = self._trend.Value == 0

            if is_direct:
                if self._prev_wpr > low_level and wpr <= low_level:
                    if self.Position <= 0:
                        if self.Position < 0:
                            self.BuyMarket()
                        self.BuyMarket()
                if self._prev_wpr < high_level and wpr >= high_level:
                    if self.Position >= 0:
                        if self.Position > 0:
                            self.SellMarket()
                        self.SellMarket()
            else:
                if self._prev_wpr > low_level and wpr <= low_level:
                    if self.Position >= 0:
                        if self.Position > 0:
                            self.SellMarket()
                        self.SellMarket()
                if self._prev_wpr < high_level and wpr >= high_level:
                    if self.Position <= 0:
                        if self.Position < 0:
                            self.BuyMarket()
                        self.BuyMarket()

        self._prev_wpr = wpr

    def CreateClone(self):
        return fractal_wpr_strategy()
