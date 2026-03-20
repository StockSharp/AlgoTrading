import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class volatility_arbitrage_spread_oscillator_model_strategy(Strategy):
    def __init__(self):
        super(volatility_arbitrage_spread_oscillator_model_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of RSI", "Parameters")
        self._long_threshold = self.Param("LongThreshold", 35) \
            .SetDisplay("Long Threshold", "RSI level to enter long", "Parameters")
        self._exit_threshold = self.Param("ExitThreshold", 65) \
            .SetDisplay("Exit Threshold", "RSI level to exit", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._front_close = 0.0
        self._second_close = 0.0
        self._rsi_val = 0.0
        self._prev_rsi = 0.0
        self._cooldown = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def long_threshold(self):
        return self._long_threshold.Value

    @property
    def exit_threshold(self):
        return self._exit_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_arbitrage_spread_oscillator_model_strategy, self).OnReseted()
        self._front_close = 0.0
        self._second_close = 0.0
        self._rsi_val = 0.0
        self._prev_rsi = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(volatility_arbitrage_spread_oscillator_model_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        self._front_close = float(candle.ClosePrice)
        self._prev_rsi = self._rsi_val
        self._rsi_val = float(rsi_value)
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        if self._prev_rsi == 0:
            return
        # Long entry when RSI crosses below threshold
        if self._rsi_val < self.long_threshold and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 45
        # Exit long / short entry when RSI crosses above threshold
        elif self._rsi_val > self.exit_threshold and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 45

    def CreateClone(self):
        return volatility_arbitrage_spread_oscillator_model_strategy()
