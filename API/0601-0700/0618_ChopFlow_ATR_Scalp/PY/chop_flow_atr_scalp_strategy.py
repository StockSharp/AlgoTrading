import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, OnBalanceVolume, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class chop_flow_atr_scalp_strategy(Strategy):
    """
    ChopFlow ATR Scalp strategy.
    Inside the trading session, when the Choppiness Index is below ChopThreshold the market is trending: a flat position goes long
    with OBV above its EMA and short with OBV below it. The exit is a symmetric stop and target placed AtrMultiplier ATRs from the entry.
    """

    def __init__(self):
        super(chop_flow_atr_scalp_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiple for the stop and the target", "Risk")
        self._chop_length = self.Param("ChopLength", 14).SetRange(2, 1000).SetDisplay("Chop Length", "Choppiness Index period", "Indicators")
        self._chop_threshold = self.Param("ChopThreshold", 60.0).SetDisplay("Chop Threshold", "Choppiness level below which entries are allowed", "Indicators")
        self._obv_ema_length = self.Param("ObvEmaLength", 10).SetGreaterThanZero().SetDisplay("OBV EMA Length", "EMA period applied to OBV", "Indicators")
        self._session_input = self.Param("SessionInput", "1700-1600").SetDisplay("Session", "Trading session as HHMM-HHMM in UTC", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._obv_ema = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._chop_bars = []
        self._prev_close = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(chop_flow_atr_scalp_strategy, self).OnReseted()
        self._reset_state()
        self._obv_ema = None

    def OnStarted2(self, time):
        super(chop_flow_atr_scalp_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        obv = OnBalanceVolume()
        self._obv_ema = ExponentialMovingAverage()
        self._obv_ema.Length = self._obv_ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, obv, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            volume_area = self.CreateChartArea()
            if volume_area is not None:
                self.DrawIndicator(volume_area, obv)
                self.DrawIndicator(volume_area, self._obv_ema)

    def _process_candle(self, candle, atr_value, obv_value):
        if candle.State != CandleStates.Finished:
            return

        chop = self._update_choppiness(candle)

        if not obv_value.IsFormed:
            return

        obv = obv_value.GetValue[Decimal](None)
        obv_ema_value = process_value(self._obv_ema, obv, candle.OpenTime, True)

        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
            return

        if self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
            return

        if self.Position != 0 or not atr_value.IsFormed or not obv_ema_value.IsFormed or chop is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if chop >= float(self._chop_threshold.Value) or not self._in_session(candle.OpenTime):
            return

        obv_ema = obv_ema_value.GetValue[Decimal](None)
        distance = atr_value.GetValue[Decimal](None) * Decimal(self._atr_multiplier.Value)
        close = candle.ClosePrice

        if obv > obv_ema:
            self.BuyMarket(self.Volume)
            self._stop_price = close - distance
            self._take_price = close + distance
        elif obv < obv_ema:
            self.SellMarket(self.Volume)
            self._stop_price = close + distance
            self._take_price = close - distance

    def _update_choppiness(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self._prev_close is not None:
            tr = max(high, self._prev_close) - min(low, self._prev_close)
        else:
            tr = high - low
        self._prev_close = float(candle.ClosePrice)

        length = self._chop_length.Value
        self._chop_bars.append((tr, high, low))
        while len(self._chop_bars) > length:
            self._chop_bars.pop(0)

        if len(self._chop_bars) < length:
            return None

        price_range = max(b[1] for b in self._chop_bars) - min(b[2] for b in self._chop_bars)
        sum_tr = sum(b[0] for b in self._chop_bars)

        if price_range <= 0 or sum_tr <= 0:
            return None

        return 100.0 * math.log10(sum_tr / price_range) / math.log10(length)

    def _in_session(self, time):
        parts = (self._session_input.Value or "").split("-")
        try:
            start = int(parts[0].strip())
            end = int(parts[1].strip())
        except (ValueError, IndexError):
            return True
        if len(parts) != 2:
            return True

        start_minutes = start // 100 * 60 + start % 100
        end_minutes = end // 100 * 60 + end % 100
        minutes = time.Hour * 60 + time.Minute

        if start_minutes == end_minutes:
            return True

        # A session whose start is after its end runs overnight.
        if start_minutes < end_minutes:
            return start_minutes <= minutes < end_minutes
        return minutes >= start_minutes or minutes < end_minutes

    def CreateClone(self):
        return chop_flow_atr_scalp_strategy()
