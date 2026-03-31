import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class jma_quantum_edge_strategy(Strategy):
    def __init__(self):
        super(jma_quantum_edge_strategy, self).__init__()

        self._jma_length = self.Param("JmaLength", 20) \
            .SetDisplay("JMA Length", "Period for main JMA", "Parameters")
        self._higher_jma_length = self.Param("HigherJmaLength", 40) \
            .SetDisplay("Higher JMA Length", "Period for higher timeframe JMA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Main timeframe", "Parameters")
        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Higher Candle Type", "Higher timeframe", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
        self._enable_stop_loss = self.Param("EnableStopLoss", True) \
            .SetDisplay("Enable Stop Loss", "Use stop loss", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 360) \
            .SetDisplay("Cooldown Bars", "Minimum bars between signals", "Risk Management")

        self._prev_jma = None
        self._prev_prev_jma = None
        self._higher_jma = None
        self._bars_since_signal = 0

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def higher_jma_length(self):
        return self._higher_jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def higher_candle_type(self):
        return self._higher_candle_type.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(jma_quantum_edge_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_prev_jma = None
        self._higher_jma = None
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(jma_quantum_edge_strategy, self).OnStarted2(time)

        jma = JurikMovingAverage()
        jma.Length = self.jma_length
        higher_jma = JurikMovingAverage()
        higher_jma.Length = self.higher_jma_length

        candle_sub = self.SubscribeCandles(self.candle_type)
        candle_sub.Bind(jma, self._process_candle).Start()

        self.SubscribeCandles(self.higher_candle_type) \
            .Bind(higher_jma, self._process_higher_candle).Start()

    def _process_higher_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        self._higher_jma = float(jma_value)

    def _process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return

        jma_val = float(jma_value)
        self._bars_since_signal += 1

        if self._prev_jma is not None and self._prev_prev_jma is not None and self._higher_jma is not None:
            prev = self._prev_jma
            prev2 = self._prev_prev_jma
            higher = self._higher_jma

            turn_up = prev < prev2 and jma_val >= prev
            turn_down = prev > prev2 and jma_val <= prev

            if self._bars_since_signal >= int(self.cooldown_bars):
                if turn_up and jma_val > higher:
                    if self.Position < 0:
                        self.BuyMarket(abs(self.Position))
                    elif self.Position == 0:
                        self.BuyMarket()
                    self._bars_since_signal = 0
                elif turn_down and jma_val < higher:
                    if self.Position > 0:
                        self.SellMarket(abs(self.Position))
                    elif self.Position == 0:
                        self.SellMarket()
                    self._bars_since_signal = 0

        self._prev_prev_jma = self._prev_jma
        self._prev_jma = jma_val

    def CreateClone(self):
        return jma_quantum_edge_strategy()
