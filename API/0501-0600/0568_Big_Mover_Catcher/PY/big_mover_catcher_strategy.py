import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

from System import Array
from StockSharp.Messages import Level1Fields
from StockSharp.Algo.Indicators import (BollingerBands, RelativeStrengthIndex, AverageDirectionalIndex,
    AverageTrueRange, EMA, MovingAverageConvergenceDivergenceSignal, IIndicator)

class big_mover_catcher_strategy(Strategy):
    def __init__(self):
        super(big_mover_catcher_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 40).SetGreaterThanZero().SetDisplay(
            "BollingerPeriod", "Rolling close window", "Parameters")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay(
            "BollingerDeviation", "Population deviation multiplier", "Parameters")
        self._is_rsi_filter_enabled = self.Param("IsRsiFilterEnabled", True).SetDisplay(
            "IsRsiFilterEnabled", "Enable directional RSI confirmation", "Parameters")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay(
            "RsiPeriod", "Wilder RSI length", "Parameters")
        self._rsi_long_threshold = self.Param("RsiLongThreshold", 50.0).SetGreaterThanZero().SetDisplay(
            "RsiLongThreshold", "Strict lower RSI bound for longs", "Parameters")
        self._rsi_short_threshold = self.Param("RsiShortThreshold", 50.0).SetGreaterThanZero().SetDisplay(
            "RsiShortThreshold", "Strict upper RSI bound for shorts", "Parameters")
        self._is_adx_filter_enabled = self.Param("IsAdxFilterEnabled", True).SetDisplay(
            "IsAdxFilterEnabled", "Enable minimum ADX strength", "Parameters")
        self._adx_period = self.Param("AdxPeriod", 28).SetGreaterThanZero().SetDisplay(
            "AdxPeriod", "Wilder directional movement and ADX lengths", "Parameters")
        self._minimum_adx = self.Param("MinimumAdx", 20.0).SetGreaterThanZero().SetDisplay(
            "MinimumAdx", "Minimum ADX strength", "Parameters")
        self._is_atr_filter_enabled = self.Param("IsAtrFilterEnabled", True).SetDisplay(
            "IsAtrFilterEnabled", "Enable minimum ATR percentage", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay(
            "AtrPeriod", "Wilder ATR length", "Parameters")
        self._minimum_atr_percent = self.Param("MinimumAtrPercent", 0.05).SetGreaterThanZero().SetDisplay(
            "MinimumAtrPercent", "Minimum ATR divided by close in percent", "Parameters")
        self._is_ema_filter_enabled = self.Param("IsEmaFilterEnabled", True).SetDisplay(
            "IsEmaFilterEnabled", "Enable close versus EMA confirmation", "Parameters")
        self._ema_period = self.Param("EmaPeriod", 350).SetGreaterThanZero().SetDisplay(
            "EmaPeriod", "Trend EMA length", "Parameters")
        self._is_macd_filter_enabled = self.Param("IsMacdFilterEnabled", True).SetDisplay(
            "IsMacdFilterEnabled", "Enable directional MACD histogram", "Parameters")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay(
            "MacdFast", "Fast MACD EMA length", "Parameters")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay(
            "MacdSlow", "Slow MACD EMA length", "Parameters")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay(
            "MacdSignal", "MACD signal EMA length", "Parameters")
        self._trade_direction = self.Param[Nullable[Sides]]("TradeDirection", Nullable[Sides]()).SetDisplay(
            "TradeDirection", "Allowed entry side; null allows both", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetGreaterThanZero().SetDisplay(
            "StopLossPercent", "Native filled-entry percentage stop", "Parameters")
        self._is_force_take_profit_enabled = self.Param("IsForceTakeProfitEnabled", False).SetDisplay(
            "IsForceTakeProfitEnabled", "Enable favorable large-body exits", "Parameters")
        self._force_take_profit_percent = self.Param("ForceTakeProfitPercent", 5.0).SetGreaterThanZero().SetDisplay(
            "ForceTakeProfitPercent", "Minimum favorable candle body percentage", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay(
            "CandleType", "Finished signal candle interval", "Parameters")
        self._indices = {}
        self._pending = None
        self.OrderRegistering += self._remember_order

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def IsRsiFilterEnabled(self):
        return self._is_rsi_filter_enabled.Value

    @IsRsiFilterEnabled.setter
    def IsRsiFilterEnabled(self, value):
        self._is_rsi_filter_enabled.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiLongThreshold(self):
        return self._rsi_long_threshold.Value

    @RsiLongThreshold.setter
    def RsiLongThreshold(self, value):
        self._rsi_long_threshold.Value = value

    @property
    def RsiShortThreshold(self):
        return self._rsi_short_threshold.Value

    @RsiShortThreshold.setter
    def RsiShortThreshold(self, value):
        self._rsi_short_threshold.Value = value

    @property
    def IsAdxFilterEnabled(self):
        return self._is_adx_filter_enabled.Value

    @IsAdxFilterEnabled.setter
    def IsAdxFilterEnabled(self, value):
        self._is_adx_filter_enabled.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def MinimumAdx(self):
        return self._minimum_adx.Value

    @MinimumAdx.setter
    def MinimumAdx(self, value):
        self._minimum_adx.Value = value

    @property
    def IsAtrFilterEnabled(self):
        return self._is_atr_filter_enabled.Value

    @IsAtrFilterEnabled.setter
    def IsAtrFilterEnabled(self, value):
        self._is_atr_filter_enabled.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def MinimumAtrPercent(self):
        return self._minimum_atr_percent.Value

    @MinimumAtrPercent.setter
    def MinimumAtrPercent(self, value):
        self._minimum_atr_percent.Value = value

    @property
    def IsEmaFilterEnabled(self):
        return self._is_ema_filter_enabled.Value

    @IsEmaFilterEnabled.setter
    def IsEmaFilterEnabled(self, value):
        self._is_ema_filter_enabled.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def IsMacdFilterEnabled(self):
        return self._is_macd_filter_enabled.Value

    @IsMacdFilterEnabled.setter
    def IsMacdFilterEnabled(self, value):
        self._is_macd_filter_enabled.Value = value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macd_fast.Value = value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macd_slow.Value = value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macd_signal.Value = value

    @property
    def TradeDirection(self):
        return self._trade_direction.Value

    @TradeDirection.setter
    def TradeDirection(self, value):
        self._trade_direction.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def IsForceTakeProfitEnabled(self):
        return self._is_force_take_profit_enabled.Value

    @IsForceTakeProfitEnabled.setter
    def IsForceTakeProfitEnabled(self, value):
        self._is_force_take_profit_enabled.Value = value

    @property
    def ForceTakeProfitPercent(self):
        return self._force_take_profit_percent.Value

    @ForceTakeProfitPercent.setter
    def ForceTakeProfitPercent(self, value):
        self._force_take_profit_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(big_mover_catcher_strategy, self).OnReseted()
        self._indices = {}
        self._pending = None

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        if self.MacdFast >= self.MacdSlow or self.RsiLongThreshold >= 100 or self.RsiShortThreshold >= 100:
            raise ValueError("MACD lengths must be ordered and RSI thresholds must lie strictly between zero and 100.")
        super(big_mover_catcher_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length, bb.Width = self.BollingerPeriod, self.BollingerDeviation
        indicators = [bb]
        self._indices = {}
        def add(name, indicator):
            self._indices[name] = len(indicators)
            indicators.append(indicator)
        if self.IsRsiFilterEnabled:
            rsi = RelativeStrengthIndex()
            rsi.Length = self.RsiPeriod
            add("rsi", rsi)
        if self.IsAdxFilterEnabled:
            adx = AverageDirectionalIndex()
            adx.Length = self.AdxPeriod
            add("adx", adx)
        if self.IsAtrFilterEnabled:
            atr = AverageTrueRange()
            atr.Length = self.AtrPeriod
            add("atr", atr)
        if self.IsEmaFilterEnabled:
            ema = EMA()
            ema.Length = self.EmaPeriod
            add("ema", ema)
        if self.IsMacdFilterEnabled:
            macd = MovingAverageConvergenceDivergenceSignal()
            macd.Macd.ShortMa.Length, macd.Macd.LongMa.Length, macd.SignalMa.Length = self.MacdFast, self.MacdSlow, self.MacdSignal
            add("macd", macd)
        self.StartProtection(Unit(), Unit(float(self.StopLossPercent), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        candles = self.SubscribeCandles(self.CandleType)
        candles.BindEx(Array[IIndicator](indicators), self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, bb)
            if "ema" in self._indices:
                self.DrawIndicator(area, indicators[self._indices["ema"]])
            self.DrawOwnTrades(area)

    def _process(self, candle, values):
        if candle.State != CandleStates.Finished or not values[0].IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        bb = values[0]
        if bb.MovingAverage is None or bb.UpBand is None or bb.LowBand is None or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        close = float(candle.ClosePrice)
        if self.Position != 0:
            body = (close - float(candle.OpenPrice) if self.Position > 0 else float(candle.OpenPrice) - close) / float(candle.OpenPrice) * 100.0 if candle.OpenPrice > 0 else 0.0
            middle_exit = close <= float(bb.MovingAverage) if self.Position > 0 else close >= float(bb.MovingAverage)
            if middle_exit or self.IsForceTakeProfitEnabled and body >= float(self.ForceTakeProfitPercent):
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return
        if not all(v.IsFormed and not v.IsEmpty for v in values) or close <= 0:
            return
        side = Sides.Buy if close > float(bb.UpBand) else Sides.Sell if close < float(bb.LowBand) else None
        if side is None or self.TradeDirection is not None and self.TradeDirection != side:
            return
        buy = side == Sides.Buy
        if "rsi" in self._indices:
            rsi = float(values[self._indices["rsi"]].GetValue[Decimal](None))
            if rsi <= float(self.RsiLongThreshold) if buy else rsi >= float(self.RsiShortThreshold):
                return
        if "adx" in self._indices:
            value = values[self._indices["adx"]].MovingAverage
            if value is None or float(value) < float(self.MinimumAdx):
                return
        if "atr" in self._indices and float(values[self._indices["atr"]].GetValue[Decimal](None)) / close * 100.0 < float(self.MinimumAtrPercent):
            return
        if "ema" in self._indices:
            ema = float(values[self._indices["ema"]].GetValue[Decimal](None))
            if close <= ema if buy else close >= ema:
                return
        if "macd" in self._indices:
            macd = values[self._indices["macd"]]
            if macd.Macd is None or macd.Signal is None:
                return
            histogram = float(macd.Macd - macd.Signal)
            if histogram <= 0.0 if buy else histogram >= 0.0:
                return
        if buy:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return big_mover_catcher_strategy()
