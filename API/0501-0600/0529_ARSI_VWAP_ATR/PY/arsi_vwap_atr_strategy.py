import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

BASE_BAND = 20.0


class arsi_vwap_atr_strategy(Strategy):
    """
    Adaptive RSI strategy with ATR or VWAP driven levels.
    The overbought line sits at 50 + 20 * BaseK and the oversold line at 50 - 20 * BaseK, each pushed further out by its
    multiplier times a volatility measure in percent of price: ATR / close for the "ATR" source, or the distance of the
    close from the daily VWAP for the "VWAP" source. RSI crossing above the oversold line goes long and crossing below the
    overbought line goes short, reversing an opposite position. A long closes when RSI crosses above 50 and a short when it
    crosses below 50. A StopLossPercent stop and a StopLossPercent * RiskReward take-profit protect every position.
    """

    def __init__(self):
        super(arsi_vwap_atr_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._base_k = self.Param("BaseK", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Base K", "Scale of the base distance of the levels from 50", "Levels")
        self._risk_percent = self.Param("RiskPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Risk %", "Percent of equity risked per trade", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.5) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Risk Reward", "Take-profit to stop-loss ratio", "Risk")
        self._source_ob = self.Param("SourceOb", "ATR") \
            .SetDisplay("OB Source", "Volatility source of the overbought line: ATR or VWAP", "Levels")
        self._source_os = self.Param("SourceOs", "ATR") \
            .SetDisplay("OS Source", "Volatility source of the oversold line: ATR or VWAP", "Levels")
        self._atr_length_ob = self.Param("AtrLengthOb", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("OB ATR Length", "ATR period of the overbought line", "Levels")
        self._atr_length_os = self.Param("AtrLengthOs", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("OS ATR Length", "ATR period of the oversold line", "Levels")
        self._ob_multiplier = self.Param("ObMultiplier", 10.0) \
            .SetNotNegative() \
            .SetDisplay("OB Multiplier", "Multiplier of the overbought adjustment", "Levels")
        self._os_multiplier = self.Param("OsMultiplier", 10.0) \
            .SetNotNegative() \
            .SetDisplay("OS Multiplier", "Multiplier of the oversold adjustment", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._vwap_day = None
        self._cum_price_volume = 0.0
        self._cum_volume = 0.0
        self._prev_rsi = None
        self._prev_ob = 0.0
        self._prev_os = 0.0

    def OnReseted(self):
        super(arsi_vwap_atr_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(arsi_vwap_atr_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr_ob = AverageTrueRange()
        atr_ob.Length = self._atr_length_ob.Value
        atr_os = AverageTrueRange()
        atr_os.Length = self._atr_length_os.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, atr_ob, atr_os, self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        take = stop * float(self._risk_reward.Value)
        self.StartProtection(
            Unit(Decimal(take), UnitTypes.Percent) if take > 0 else Unit(),
            Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    @staticmethod
    def _deviation(source, atr, close, vwap):
        if close <= 0:
            return 0.0
        if str(source).upper() == "VWAP":
            return abs(close - vwap) / close * 100.0
        return atr / close * 100.0

    def _process_candle(self, candle, rsi_value, atr_ob_value, atr_os_value):
        if candle.State != CandleStates.Finished:
            return

        # Daily VWAP of the typical price, restarted at each UTC day.
        day = candle.OpenTime.Date
        if self._vwap_day is None or day != self._vwap_day:
            self._vwap_day = day
            self._cum_price_volume = 0.0
            self._cum_volume = 0.0

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)
        typical = (high + low + close) / 3.0
        self._cum_price_volume += typical * volume
        self._cum_volume += volume

        vwap = self._cum_price_volume / self._cum_volume if self._cum_volume > 0 else close
        rsi = float(rsi_value)
        base = BASE_BAND * float(self._base_k.Value)

        ob = min(100.0, 50.0 + base + float(self._ob_multiplier.Value) * self._deviation(self._source_ob.Value, float(atr_ob_value), close, vwap))
        os = max(0.0, 50.0 - base - float(self._os_multiplier.Value) * self._deviation(self._source_os.Value, float(atr_os_value), close, vwap))

        prev = self._prev_rsi
        prev_ob = self._prev_ob
        prev_os = self._prev_os
        self._prev_rsi = rsi
        self._prev_ob = ob
        self._prev_os = os

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_signal = prev <= prev_os and rsi > os
        short_signal = prev >= prev_ob and rsi < ob

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and prev <= 50.0 and rsi > 50.0:
            self.SellMarket(self.Position)
        elif self.Position < 0 and prev >= 50.0 and rsi < 50.0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return arsi_vwap_atr_strategy()
