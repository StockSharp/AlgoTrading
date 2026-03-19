import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class hull_ma_rsi_strategy(Strategy):
    """
    Hull MA + RSI. Buy when HMA rising and RSI oversold, sell when HMA falling and RSI overbought.
    """

    def __init__(self):
        super(hull_ma_rsi_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetDisplay("HMA Period", "Hull MA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 130).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._hma_value = 0.0
        self._prev_hma = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_rsi_strategy, self).OnReseted()
        self._hma_value = 0.0
        self._prev_hma = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(hull_ma_rsi_strategy, self).OnStarted(time)
        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, self._on_hma)
        subscription.Bind(rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _on_hma(self, candle, hma_val):
        self._hma_value = float(hma_val)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._hma_value == 0:
            return
        if self._prev_hma == 0:
            self._prev_hma = self._hma_value
            return
        is_rising = self._hma_value > self._prev_hma
        is_falling = self._hma_value < self._prev_hma
        rsi = float(rsi_val)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_hma = self._hma_value
            return
        if is_rising and rsi < self._rsi_oversold.Value and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif is_falling and rsi > self._rsi_overbought.Value and self.Position == 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif is_falling and self.Position > 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif is_rising and self.Position < 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_hma = self._hma_value

    def CreateClone(self):
        return hull_ma_rsi_strategy()
