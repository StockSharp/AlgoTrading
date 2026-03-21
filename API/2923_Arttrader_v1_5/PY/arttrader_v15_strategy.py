import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class arttrader_v15_strategy(Strategy):
    def __init__(self):
        super(arttrader_v15_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Trading candles", "General")
        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Trend Candle Type", "Trend candles for EMA", "General")
        self._ema_period = self.Param("EmaPeriod", 11) \
            .SetDisplay("EMA Period", "EMA period on trend timeframe", "Indicators")

        self._current_ema = 0.0
        self._previous_ema = 0.0
        self._has_current_ema = False
        self._has_previous_ema = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def TrendCandleType(self):
        return self._trend_candle_type.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(arttrader_v15_strategy, self).OnReseted()
        self._current_ema = 0.0
        self._previous_ema = 0.0
        self._has_current_ema = False
        self._has_previous_ema = False

    def OnStarted(self, time):
        super(arttrader_v15_strategy, self).OnStarted(time)
        self._current_ema = 0.0
        self._previous_ema = 0.0
        self._has_current_ema = False
        self._has_previous_ema = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        trend_sub = self.SubscribeCandles(self.TrendCandleType)
        trend_sub.Bind(ema, self._on_trend_candle).Start()

        trade_sub = self.SubscribeCandles(self.CandleType)
        trade_sub.Bind(self._on_trade_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, trade_sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_trend_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        ev = float(ema_value)
        if self._has_current_ema:
            self._previous_ema = self._current_ema
            self._has_previous_ema = True
        self._current_ema = ev
        self._has_current_ema = True

    def _on_trade_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_previous_ema:
            return
        ema_slope = self._current_ema - self._previous_ema
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        if ema_slope > 0 and close <= open_p and self.Position <= 0:
            self.BuyMarket()
        elif ema_slope < 0 and close >= open_p and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return arttrader_v15_strategy()
