import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class jk_bull_p_auto_trader_strategy(Strategy):
    """
    JK BullP AutoTrader: RSI momentum with EMA filter and ATR-based stops.
    Buys when RSI crosses above 55 with price above EMA,
    sells when RSI crosses below 45 with price below EMA.
    """

    def __init__(self):
        super(jk_bull_p_auto_trader_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 13) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_rsi = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(jk_bull_p_auto_trader_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(jk_bull_p_auto_trader_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_val)
        ema = float(ema_val)
        atr = float(atr_val)

        if self._prev_rsi == 0 or atr <= 0:
            self._prev_rsi = rsi
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close >= self._entry_price + atr * 2.5 or close <= self._entry_price - atr * 1.5 or rsi > 75:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - atr * 2.5 or close >= self._entry_price + atr * 1.5 or rsi < 25:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if rsi > 55 and self._prev_rsi <= 55 and close > ema:
                self._entry_price = close
                self.BuyMarket()
            elif rsi < 45 and self._prev_rsi >= 45 and close < ema:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rsi

    def CreateClone(self):
        return jk_bull_p_auto_trader_strategy()
