import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class xauusd_trend_strategy(Strategy):
    def __init__(self):
        super(xauusd_trend_strategy, self).__init__()
        self._ema_short = self.Param("EmaShort", 20) \
            .SetDisplay("EMA Short", "Fast EMA period", "Indicators")
        self._ema_long = self.Param("EmaLong", 50) \
            .SetDisplay("EMA Long", "Slow EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._stop_pct = self.Param("StopPct", 1.5) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp_risk_ratio = self.Param("TpRiskRatio", 2) \
            .SetDisplay("TP/SL Ratio", "Take profit to stop ratio", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stop_price = 0.0
        self._take_price = 0.0
        self._entry_price = 0.0

    @property
    def ema_short(self):
        return self._ema_short.Value

    @property
    def ema_long(self):
        return self._ema_long.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def tp_risk_ratio(self):
        return self._tp_risk_ratio.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xauusd_trend_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_price = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(xauusd_trend_strategy, self).OnStarted2(time)
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.ema_short
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.ema_long
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        std_dev = StandardDeviation()
        std_dev.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_fast, ema_slow, rsi, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_fast_val, ema_slow_val, rsi_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        # TP/SL management
        if self.Position > 0 and self._entry_price > 0:
            if candle.ClosePrice <= self._stop_price or candle.ClosePrice >= self._take_price:
                self.SellMarket()
                self._entry_price = 0
                return
        elif self.Position < 0 and self._entry_price > 0:
            if candle.ClosePrice >= self._stop_price or candle.ClosePrice <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0
                return
        if std_val <= 0:
            return
        # EMA trend + RSI filter + price above/below band
        upper_band = ema_slow_val + 2 * std_val
        lower_band = ema_slow_val - 2 * std_val
        long_cond = ema_fast_val > ema_slow_val and rsi_val < self.rsi_oversold
        short_cond = ema_fast_val < ema_slow_val and rsi_val > self.rsi_overbought
        if long_cond and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            sl = self._entry_price * self.stop_pct / 100
            self._stop_price = self._entry_price - sl
            self._take_price = self._entry_price + sl * self.tp_risk_ratio
        elif short_cond and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            sl = self._entry_price * self.stop_pct / 100
            self._stop_price = self._entry_price + sl
            self._take_price = self._entry_price - sl * self.tp_risk_ratio

    def CreateClone(self):
        return xauusd_trend_strategy()
