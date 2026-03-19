import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class modular_range_trading_strategy(Strategy):
    """
    Modular range trading: RSI reversion with SMA context.
    """

    def __init__(self):
        super(modular_range_trading_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI period", "General")
        self._sma_period = self.Param("SmaPeriod", 30).SetDisplay("SMA Period", "SMA period", "General")
        self._rsi_ob = self.Param("RsiOverbought", 70.0).SetDisplay("RSI OB", "RSI overbought", "General")
        self._rsi_os = self.Param("RsiOversold", 30.0).SetDisplay("RSI OS", "RSI oversold", "General")
        self._cooldown_bars = self.Param("SignalCooldownBars", 10).SetDisplay("Cooldown", "Min bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_rsi = 0.0
        self._has_prev = False
        self._bars_from_signal = 10

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(modular_range_trading_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(modular_range_trading_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        rsi = float(rsi_val)
        sma = float(sma_val)
        close = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_rsi = rsi
            self._has_prev = True
            return
        self._bars_from_signal += 1
        ob = float(self._rsi_ob.Value)
        os_val = float(self._rsi_os.Value)
        long_signal = self._prev_rsi <= os_val and rsi > os_val and close < sma
        short_signal = self._prev_rsi >= ob and rsi < ob and close > sma
        if self._bars_from_signal >= self._cooldown_bars.Value and long_signal and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= self._cooldown_bars.Value and short_signal and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        elif self.Position > 0 and (rsi >= 55.0 or close >= sma):
            self.SellMarket()
        elif self.Position < 0 and (rsi <= 45.0 or close <= sma):
            self.BuyMarket()
        self._prev_rsi = rsi

    def CreateClone(self):
        return modular_range_trading_strategy()
