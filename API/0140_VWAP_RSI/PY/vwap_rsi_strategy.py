import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class vwap_rsi_strategy(Strategy):
    """
    VWAP + RSI strategy.
    Enters when price is below VWAP and RSI oversold (longs)
    or above VWAP and RSI overbought (shorts).
    """

    def __init__(self):
        super(vwap_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._vwap_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_rsi_strategy, self).OnReseted()
        self._vwap_value = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vwap_rsi_strategy, self).OnStarted2(time)

        self._vwap_value = 0.0
        self._cooldown = 0

        vwap = VolumeWeightedMovingAverage()
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)

        # Bind VWAP with BindEx to capture value
        subscription.BindEx(vwap, self._on_vwap)

        # Bind RSI for main logic
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def _on_vwap(self, candle, vwap_iv):
        if vwap_iv.IsFormed:
            self._vwap_value = float(vwap_iv.Value)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if self._vwap_value == 0:
            return

        close = float(candle.ClosePrice)
        rsi = float(rsi_val)
        vwap = self._vwap_value
        cd = self._cooldown_bars.Value
        oversold = float(self._rsi_oversold.Value)
        overbought = float(self._rsi_overbought.Value)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Long: price below VWAP + RSI oversold
        if close < vwap and rsi < oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Short: price above VWAP + RSI overbought
        elif close > vwap and rsi > overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price above VWAP
        if self.Position > 0 and close > vwap:
            self.SellMarket()
            self._cooldown = cd
        # Exit short: price below VWAP
        elif self.Position < 0 and close < vwap:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return vwap_rsi_strategy()
