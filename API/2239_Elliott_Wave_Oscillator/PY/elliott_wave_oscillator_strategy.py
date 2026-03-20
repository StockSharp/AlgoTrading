import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class elliott_wave_oscillator_strategy(Strategy):
    def __init__(self):
        super(elliott_wave_oscillator_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast Length", "Length of the fast SMA", "Indicator")
        self._slow_length = self.Param("SlowLength", 35) \
            .SetDisplay("Slow Length", "Length of the slow SMA", "Indicator")
        self._take_profit_pct = self.Param("TakeProfitPct", 1.0) \
            .SetDisplay("Take Profit %", "Percentage take profit", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage stop loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_ewo = 0.0
        self._prev_prev_ewo = 0.0
        self._is_first_value = True

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(elliott_wave_oscillator_strategy, self).OnReseted()
        self._prev_ewo = 0.0
        self._prev_prev_ewo = 0.0
        self._is_first_value = True

    def OnStarted(self, time):
        super(elliott_wave_oscillator_strategy, self).OnStarted(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_length
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_pct) / 100.0, UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_pct) / 100.0, UnitTypes.Percent))

    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fast_value = float(fast_value)
        slow_value = float(slow_value)
        ewo_value = fast_value - slow_value
        if self._is_first_value:
            self._prev_ewo = ewo_value
            self._prev_prev_ewo = ewo_value
            self._is_first_value = False
            return
        if self._prev_ewo < self._prev_prev_ewo and ewo_value > self._prev_ewo:
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_ewo > self._prev_prev_ewo and ewo_value < self._prev_ewo:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_prev_ewo = self._prev_ewo
        self._prev_ewo = ewo_value

    def CreateClone(self):
        return elliott_wave_oscillator_strategy()
