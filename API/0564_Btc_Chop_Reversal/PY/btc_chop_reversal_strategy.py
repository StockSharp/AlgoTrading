import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class btc_chop_reversal_strategy(Strategy):
    def __init__(self):
        super(btc_chop_reversal_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 23) \
            .SetDisplay("EMA Period", "Length of EMA", "Indicators")
        self._atr_length = self.Param("AtrLength", 55) \
            .SetDisplay("ATR Length", "ATR calculation length", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.5) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR bands", "Indicators")
        self._rsi_length = self.Param("RsiLength", 9) \
            .SetDisplay("RSI Length", "Length for RSI", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 63) \
            .SetDisplay("RSI Overbought", "Overbought level for RSI", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 37) \
            .SetDisplay("RSI Oversold", "Oversold level for RSI", "Indicators")
        self._macd_fast = self.Param("MacdFast", 14) \
            .SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 44) \
            .SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 3) \
            .SetDisplay("MACD Signal", "Signal length for MACD", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.75) \
            .SetDisplay("Take Profit (%)", "Take profit percent", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.4) \
            .SetDisplay("Stop Loss (%)", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_short_setup = False
        self._prev_long_setup = False
        self._prev_macd_hist = 0.0

    @property
    def ema_period(self):
        return self._ema_period.Value
    @property
    def atr_length(self):
        return self._atr_length.Value
    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value
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
    def macd_fast(self):
        return self._macd_fast.Value
    @property
    def macd_slow(self):
        return self._macd_slow.Value
    @property
    def macd_signal(self):
        return self._macd_signal.Value
    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(btc_chop_reversal_strategy, self).OnReseted()
        self._prev_short_setup = False
        self._prev_long_setup = False
        self._prev_macd_hist = 0.0

    def OnStarted(self, time):
        super(btc_chop_reversal_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = AverageTrueRange()
        atr.Length = self.atr_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast
        macd.Macd.LongMa.Length = self.macd_slow
        macd.SignalMa.Length = self.macd_signal
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_percent), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_percent), UnitTypes.Percent)
        )
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, ema, atr, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, macd_value, ema_value, atr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        ema_val = float(ema_value)
        atr_val = float(atr_value)
        rsi_val = float(rsi_value)

        macd_line = float(macd_value.Macd) if macd_value.Macd is not None else 0.0
        signal_line = float(macd_value.Signal) if macd_value.Signal is not None else 0.0
        macd_hist = macd_line - signal_line

        if atr_val <= 0:
            return

        upper_band = ema_val + float(self.atr_multiplier) * atr_val
        lower_band = ema_val - float(self.atr_multiplier) * atr_val

        short_setup = (float(candle.HighPrice) > upper_band and
                       rsi_val > float(self.rsi_overbought) and
                       macd_hist < self._prev_macd_hist and
                       float(candle.ClosePrice) < float(candle.OpenPrice))

        long_setup = (float(candle.LowPrice) < lower_band and
                      rsi_val < float(self.rsi_oversold) and
                      macd_hist > self._prev_macd_hist and
                      float(candle.ClosePrice) > float(candle.OpenPrice))

        short_confirmed = short_setup and not self._prev_short_setup
        long_confirmed = long_setup and not self._prev_long_setup

        if short_confirmed and self.Position >= 0:
            self.SellMarket()
        elif long_confirmed and self.Position <= 0:
            self.BuyMarket()

        self._prev_short_setup = short_setup
        self._prev_long_setup = long_setup
        self._prev_macd_hist = macd_hist

    def CreateClone(self):
        return btc_chop_reversal_strategy()
