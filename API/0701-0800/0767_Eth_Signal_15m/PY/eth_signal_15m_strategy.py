import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

STOP_ATR = 4.0
LONG_TAKE_ATR = 2.0
SHORT_TAKE_ATR = 2.237


class eth_signal_15m_strategy(Strategy):
    """
    ETH signal 15m strategy.
    A SuperTrend flip to an uptrend goes long when RSI is below RsiOverbought, a flip to a downtrend goes short when RSI is above
    RsiOversold, reversing an opposite position. The stop lies 4 ATR from the entry, the take profit 2 ATR for longs and 2.237 ATR
    for shorts.
    """

    def __init__(self):
        super(eth_signal_15m_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 12).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period of SuperTrend and the exits", "SuperTrend")
        self._factor = self.Param("Factor", 2.76).SetGreaterThanZero().SetDisplay("Factor", "SuperTrend factor", "SuperTrend")
        self._rsi_length = self.Param("RsiLength", 12).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level below which longs are allowed", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level above which shorts are allowed", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_up_trend = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(eth_signal_15m_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(eth_signal_15m_strategy, self).OnStarted2(time)

        self._prev_up_trend = None

        super_trend = SuperTrend()
        super_trend.Length = self._atr_period.Value
        super_trend.Multiplier = Decimal(float(self._factor.Value))
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(super_trend, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, super_trend_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not super_trend_value.IsFormed or not rsi_value.IsFormed or not atr_value.IsFormed:
            return

        is_up = bool(super_trend_value.IsUpTrend)
        was_up = self._prev_up_trend
        self._prev_up_trend = is_up

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0 and (low <= self._stop_price or high >= self._take_price):
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and (high >= self._stop_price or low <= self._take_price):
            self.BuyMarket(-self.Position)
            return

        if was_up is None:
            return

        rsi = float(rsi_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        if not was_up and is_up and rsi < float(self._rsi_overbought.Value) and self.Position <= 0:
            self._stop_price = close - atr * STOP_ATR
            self._take_price = close + atr * LONG_TAKE_ATR
            self.BuyMarket(self.Volume + abs(self.Position))
        elif was_up and not is_up and rsi > float(self._rsi_oversold.Value) and self.Position >= 0:
            self._stop_price = close + atr * STOP_ATR
            self._take_price = close - atr * SHORT_TAKE_ATR
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return eth_signal_15m_strategy()
