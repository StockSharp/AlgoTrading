import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class macd_rsi_strategy(Strategy):
    """
    MACD + RSI: uses MACD for trend direction and RSI for entry timing at extreme levels.
    """

    def __init__(self):
        super(macd_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period for RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 150).SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._rsi_value = 50.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_rsi_strategy, self).OnReseted()
        self._rsi_value = 50.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(macd_rsi_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._on_rsi)
        subscription.BindEx(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def _on_rsi(self, candle, rsi_val):
        self._rsi_value = float(rsi_val)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        typed_val = macd_value
        if typed_val.Macd is None or typed_val.Signal is None:
            return
        macd_line = float(typed_val.Macd)
        signal_line = float(typed_val.Signal)
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        is_uptrend = macd_line > signal_line
        if is_uptrend and self._rsi_value < self._rsi_oversold.Value and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif not is_uptrend and self._rsi_value > self._rsi_overbought.Value and self.Position == 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        if self.Position > 0 and not is_uptrend:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and is_uptrend:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return macd_rsi_strategy()
