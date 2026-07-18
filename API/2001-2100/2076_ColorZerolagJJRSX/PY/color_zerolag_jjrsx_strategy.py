import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_jjrsx_strategy(Strategy):
    def __init__(self):
        super(color_zerolag_jjrsx_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast Period", "Fast RSI period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow Period", "Slow RSI period", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for indicator", "General")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_zerolag_jjrsx_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(color_zerolag_jjrsx_strategy, self).OnStarted2(time)
        fast_rsi = RelativeStrengthIndex()
        fast_rsi.Length = self.fast_period
        slow_rsi = RelativeStrengthIndex()
        slow_rsi.Length = self.slow_period
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_rsi, slow_rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_rsi)
            self.DrawIndicator(area, slow_rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        cross_down = self._prev_fast > self._prev_slow and fast < slow
        cross_up = self._prev_fast < self._prev_slow and fast > slow

        if cross_down and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_up and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return color_zerolag_jjrsx_strategy()
