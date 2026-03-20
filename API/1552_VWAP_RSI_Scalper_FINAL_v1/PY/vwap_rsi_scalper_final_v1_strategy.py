import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class vwap_rsi_scalper_final_v1_strategy(Strategy):
    def __init__(self):
        super(vwap_rsi_scalper_final_v1_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 7) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 25) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 75) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 2) \
            .SetDisplay("Max Trades", "Max trades per day", "Risk")
        self._stop_mult = self.Param("StopMult", 1) \
            .SetDisplay("Stop Mult", "StdDev multiplier for stop", "Risk")
        self._target_mult = self.Param("TargetMult", 2) \
            .SetDisplay("Target Mult", "StdDev multiplier for target", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._trades_today = 0
        self._current_day = None
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def max_trades_per_day(self):
        return self._max_trades_per_day.Value

    @property
    def stop_mult(self):
        return self._stop_mult.Value

    @property
    def target_mult(self):
        return self._target_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_rsi_scalper_final_v1_strategy, self).OnReseted()
        self._trades_today = 0
        self._current_day = None
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted(self, time):
        super(vwap_rsi_scalper_final_v1_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        std_dev = StandardDeviation()
        std_dev.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val, ema_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.Date
        if day != self._current_day:
            self._current_day = day
            self._trades_today = 0
        # TP/SL exit
        if self.Position > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_profit_price:
                self.SellMarket()
                return
        elif self.Position < 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_profit_price:
                self.BuyMarket()
                return
        if std_val <= 0 or self._trades_today >= self.max_trades_per_day:
            return
        # Entry signals
        if self.Position == 0:
            can_long = rsi_val < self.rsi_oversold and candle.ClosePrice > ema_val
            can_short = rsi_val > self.rsi_overbought and candle.ClosePrice < ema_val
            if can_long:
                self.BuyMarket()
                self._trades_today += 1
                self._stop_price = candle.ClosePrice - std_val * self.stop_mult
                self._take_profit_price = candle.ClosePrice + std_val * self.target_mult
            elif can_short:
                self.SellMarket()
                self._trades_today += 1
                self._stop_price = candle.ClosePrice + std_val * self.stop_mult
                self._take_profit_price = candle.ClosePrice - std_val * self.target_mult

    def CreateClone(self):
        return vwap_rsi_scalper_final_v1_strategy()
