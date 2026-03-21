import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (ExponentialMovingAverage, MovingAverageConvergenceDivergence,
    BollingerBands, SimpleMovingAverage, RelativeStrengthIndex, ParabolicSar)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class natuseko_protrader_4h_strategy(Strategy):
    def __init__(self):
        super(natuseko_protrader_4h_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(5)).SetDisplay("Candle type", "Primary timeframe.", "General")
        self._trade_volume = self.Param("TradeVolume", 0.1).SetGreaterThanZero().SetDisplay("Trade volume", "Default order size.", "Trading")
        self._fast_ema_period = self.Param("FastEmaPeriod", 13).SetGreaterThanZero().SetDisplay("Fast EMA period", "Fast EMA length.", "Indicator")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21).SetGreaterThanZero().SetDisplay("Slow EMA period", "Slow EMA length.", "Indicator")
        self._trend_ema_period = self.Param("TrendEmaPeriod", 55).SetGreaterThanZero().SetDisplay("Trend EMA period", "Trend EMA length.", "Indicator")
        self._macd_fast_period = self.Param("MacdFastPeriod", 5).SetGreaterThanZero().SetDisplay("MACD fast period", "MACD fast EMA.", "Indicator")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26).SetGreaterThanZero().SetDisplay("MACD slow period", "MACD slow EMA.", "Indicator")
        self._rsi_period = self.Param("RsiPeriod", 21).SetGreaterThanZero().SetDisplay("RSI period", "RSI length.", "Indicator")
        self._rsi_entry_level = self.Param("RsiEntryLevel", 50.0).SetDisplay("RSI neutral level", "Central RSI threshold.", "Trading")
        self._rsi_tp_long = self.Param("RsiTakeProfitLong", 65.0).SetDisplay("RSI TP long", "RSI partial exit long.", "Trading")
        self._rsi_tp_short = self.Param("RsiTakeProfitShort", 35.0).SetDisplay("RSI TP short", "RSI partial exit short.", "Trading")
        self._distance_threshold = self.Param("DistanceThresholdPoints", 100.0).SetNotNegative().SetDisplay("Distance threshold", "Max distance from trend EMA.", "Trading")
        self._sar_step = self.Param("SarStep", 0.02).SetGreaterThanZero().SetDisplay("SAR step", "Parabolic SAR step.", "Indicator")
        self._sar_maximum = self.Param("SarMaximum", 0.2).SetGreaterThanZero().SetDisplay("SAR maximum", "Parabolic SAR max.", "Indicator")
        self._use_sar_sl = self.Param("UseSarStopLoss", False).SetDisplay("Use SAR stop loss", "SAR defines stop.", "Risk")
        self._use_trend_sl = self.Param("UseTrendStopLoss", True).SetDisplay("Use trend stop loss", "Trend EMA defines stop.", "Risk")
        self._stop_offset = self.Param("StopOffsetPoints", 0).SetNotNegative().SetDisplay("Stop offset", "Additional stop offset.", "Risk")
        self._use_sar_tp = self.Param("UseSarTakeProfit", True).SetDisplay("Use SAR take profit", "SAR partial exits.", "Risk")
        self._use_rsi_tp = self.Param("UseRsiTakeProfit", True).SetDisplay("Use RSI take profit", "RSI partial exits.", "Risk")
        self._min_profit = self.Param("MinimumProfitPoints", 5.0).SetNotNegative().SetDisplay("Minimum profit", "Min profit for TP.", "Risk")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value
    @property
    def TradeVolume(self): return self._trade_volume.Value
    @TradeVolume.setter
    def TradeVolume(self, value): self._trade_volume.Value = value

    def OnReseted(self):
        super(natuseko_protrader_4h_strategy, self).OnReseted()
        self._waiting_long = False
        self._waiting_short = False
        self._long_partial = False
        self._short_partial = False
        self._long_stop = None
        self._short_stop = None
        self._long_entry = None
        self._short_entry = None

    def OnStarted(self, time):
        super(natuseko_protrader_4h_strategy, self).OnStarted(time)
        self.Volume = self.TradeVolume
        self._waiting_long = False
        self._waiting_short = False
        self._long_partial = False
        self._short_partial = False
        self._long_stop = None
        self._short_stop = None
        self._long_entry = None
        self._short_entry = None

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_ema_period.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_ema_period.Value
        self._trend_ema = ExponentialMovingAverage()
        self._trend_ema.Length = self._trend_ema_period.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        self._macd = MovingAverageConvergenceDivergence()
        self._macd.ShortMa.Length = self._macd_fast_period.Value
        self._macd.LongMa.Length = self._macd_slow_period.Value
        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.Acceleration = self._sar_step.Value
        self._parabolic_sar.AccelerationMax = self._sar_maximum.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_ema, self._slow_ema, self._trend_ema, self._rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawIndicator(area, self._trend_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, trend_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            price_step = self.Security.PriceStep
        stop_offset = self._stop_offset.Value * price_step

        # Entry logic
        base_long = fast_val > slow_val and fast_val > trend_val and rsi_val > self._rsi_entry_level.Value
        base_short = fast_val < slow_val and fast_val < trend_val and rsi_val < self._rsi_entry_level.Value

        if self.Position == 0:
            if base_long:
                dist_limit = self._distance_threshold.Value * price_step
                if dist_limit > 0 and (close - trend_val) >= dist_limit:
                    self._waiting_long = True
                else:
                    self.BuyMarket()
                    self._long_entry = close
                    self._long_partial = False
            elif base_short:
                dist_limit = self._distance_threshold.Value * price_step
                if dist_limit > 0 and (trend_val - close) >= dist_limit:
                    self._waiting_short = True
                else:
                    self.SellMarket()
                    self._short_entry = close
                    self._short_partial = False

        # Exit logic
        if self.Position > 0:
            profit_threshold = self._min_profit.Value * price_step
            entry = self._long_entry if self._long_entry is not None else close
            profit = close - entry
            if profit_threshold <= 0 or profit >= profit_threshold:
                if self._use_rsi_tp.Value and rsi_val >= self._rsi_tp_long.Value and not self._long_partial:
                    self.SellMarket()
                    self._long_partial = True
                elif rsi_val <= self._rsi_entry_level.Value:
                    self.SellMarket()
                    self._long_entry = None
        elif self.Position < 0:
            profit_threshold = self._min_profit.Value * price_step
            entry = self._short_entry if self._short_entry is not None else close
            profit = entry - close
            if profit_threshold <= 0 or profit >= profit_threshold:
                if self._use_rsi_tp.Value and rsi_val <= self._rsi_tp_short.Value and not self._short_partial:
                    self.BuyMarket()
                    self._short_partial = True
                elif rsi_val >= self._rsi_entry_level.Value:
                    self.BuyMarket()
                    self._short_entry = None

    def CreateClone(self):
        return natuseko_protrader_4h_strategy()
