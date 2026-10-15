import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (IIndicator, BollingerBands, KeltnerChannels, Momentum, ExponentialMovingAverage,
    AverageTrueRange, RelativeStrengthIndex, StandardDeviation)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class adaptive_squeeze_momentum_strategy(Strategy):
    """
    Adaptive Squeeze Momentum strategy.
    Trades only when the squeeze is released (Bollinger Bands outside the Keltner Channel) and ATR is at least MinVolatility percent
    of price. A long needs momentum above MomentumMultiplier standard deviations of momentum, RSI above RsiOversold and a
    rising trend EMA; a short mirrors this with RSI below RsiOverbought and a falling EMA (both filters optional). The
    opposite signal reverses; ATR stop-loss and take-profit levels and a holding period of HoldingPeriodMultiplier * MomentumLength
    bars close positions.
    """

    def __init__(self):
        super(adaptive_squeeze_momentum_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Bollinger Bands period", "Squeeze")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Multiplier", "Bollinger Bands standard deviation multiplier", "Squeeze")
        self._keltner_period = self.Param("KeltnerPeriod", 20).SetGreaterThanZero().SetDisplay("Keltner Period", "Keltner Channel period", "Squeeze")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Keltner Multiplier", "Keltner Channel ATR multiplier", "Squeeze")
        self._momentum_length = self.Param("MomentumLength", 12).SetGreaterThanZero().SetDisplay("Momentum Length", "Momentum period, also the window of its standard deviation", "Momentum")
        self._trend_ma_length = self.Param("TrendMaLength", 50).SetGreaterThanZero().SetDisplay("Trend MA Length", "Period of the trend EMA", "Filters")
        self._use_atr_stops = self.Param("UseAtrStops", True).SetDisplay("Use ATR Stops", "Use ATR stop-loss and take-profit", "Risk")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSl", 1.5).SetNotNegative().SetDisplay("ATR Multiplier SL", "ATR multiplier of the stop-loss", "Risk")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTp", 2.5).SetNotNegative().SetDisplay("ATR Multiplier TP", "ATR multiplier of the take-profit", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._min_volatility = self.Param("MinVolatility", 0.5).SetNotNegative().SetDisplay("Min Volatility %", "Minimum ATR as a percent of the close required to trade", "Filters")
        self._holding_period_multiplier = self.Param("HoldingPeriodMultiplier", 1.5).SetNotNegative().SetDisplay("Holding Period Multiplier", "Holding period in multiples of MomentumLength bars", "Risk")
        self._use_trend_filter = self.Param("UseTrendFilter", True).SetDisplay("Use Trend Filter", "Require the trend EMA to slope in the trade direction", "Filters")
        self._use_rsi_filter = self.Param("UseRsiFilter", True).SetDisplay("Use RSI Filter", "Require RSI on the trade side of its level", "Filters")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Filters")
        self._rsi_oversold = self.Param("RsiOversold", 40.0).SetDisplay("RSI Oversold", "RSI level a long must be above", "Filters")
        self._rsi_overbought = self.Param("RsiOverbought", 60.0).SetDisplay("RSI Overbought", "RSI level a short must be below", "Filters")
        self._momentum_multiplier = self.Param("MomentumMultiplier", 1.5).SetNotNegative().SetDisplay("Momentum Multiplier", "Standard deviations of momentum that make the dynamic threshold", "Momentum")
        self._allow_long = self.Param("AllowLong", True).SetDisplay("Allow Long", "Allow long trades", "General")
        self._allow_short = self.Param("AllowShort", True).SetDisplay("Allow Short", "Allow short trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._momentum_std_dev = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_ema = None
        self._stop_price = None
        self._take_price = None
        self._bars_in_position = 0

    def OnReseted(self):
        super(adaptive_squeeze_momentum_strategy, self).OnReseted()
        self._momentum_std_dev = None
        self._reset_state()

    def OnStarted2(self, time):
        super(adaptive_squeeze_momentum_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_multiplier.Value)
        keltner = KeltnerChannels()
        keltner.Length = self._keltner_period.Value
        keltner.Multiplier = Decimal(self._keltner_multiplier.Value)
        momentum = Momentum()
        momentum.Length = self._momentum_length.Value
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self._trend_ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        self._momentum_std_dev = StandardDeviation()
        self._momentum_std_dev.Length = self._momentum_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([bollinger, keltner, momentum, trend_ema, atr, rsi]), self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, keltner)
            self.DrawIndicator(area, trend_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, values):
        if candle.State != CandleStates.Finished:
            return

        momentum_value = values[2]
        threshold = None
        if momentum_value.IsFormed:
            std_dev = process_value(self._momentum_std_dev, momentum_value.GetValue[Decimal](None), candle.ServerTime, True)
            if self._momentum_std_dev.IsFormed:
                threshold = std_dev.GetValue[Decimal](None) * Decimal(self._momentum_multiplier.Value)

        for value in values:
            if not value.IsFormed:
                return

        bb = values[0]
        kc = values[1]
        if bb.UpBand is None or bb.LowBand is None or kc.Upper is None or kc.Lower is None:
            return

        momentum = momentum_value.GetValue[Decimal](None)
        ema = values[3].GetValue[Decimal](None)
        atr = values[4].GetValue[Decimal](None)
        rsi = values[5].GetValue[Decimal](None)

        prev_ema = self._prev_ema
        self._prev_ema = ema

        if threshold is None or prev_ema is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position != 0 and self._manage_position(candle):
            return

        squeeze_released = bb.UpBand > kc.Upper and bb.LowBand < kc.Lower
        volatile = close > 0 and atr / close * Decimal(100) >= Decimal(self._min_volatility.Value)
        use_rsi = self._use_rsi_filter.Value
        use_trend = self._use_trend_filter.Value

        long_signal = (self._allow_long.Value and squeeze_released and volatile and momentum > threshold
            and (not use_rsi or rsi > Decimal(self._rsi_oversold.Value))
            and (not use_trend or ema > prev_ema))

        short_signal = (self._allow_short.Value and squeeze_released and volatile and momentum < -threshold
            and (not use_rsi or rsi < Decimal(self._rsi_overbought.Value))
            and (not use_trend or ema < prev_ema))

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, True)
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, False)

    def _manage_position(self, candle):
        self._bars_in_position += 1

        if self.Position > 0:
            hit_stop = ((self._stop_price is not None and candle.LowPrice <= self._stop_price)
                or (self._take_price is not None and candle.HighPrice >= self._take_price))
        else:
            hit_stop = ((self._stop_price is not None and candle.HighPrice >= self._stop_price)
                or (self._take_price is not None and candle.LowPrice <= self._take_price))

        holding_bars = int(round(self._momentum_length.Value * float(self._holding_period_multiplier.Value)))
        expired = holding_bars > 0 and self._bars_in_position >= holding_bars

        if not hit_stop and not expired:
            return False

        if self.Position > 0:
            self.SellMarket(self.Position)
        else:
            self.BuyMarket(-self.Position)

        self._stop_price = None
        self._take_price = None
        self._bars_in_position = 0
        return True

    def _set_levels(self, price, atr, is_long):
        self._bars_in_position = 0

        if not self._use_atr_stops.Value:
            self._stop_price = None
            self._take_price = None
            return

        stop = atr * Decimal(self._atr_multiplier_sl.Value)
        take = atr * Decimal(self._atr_multiplier_tp.Value)
        self._stop_price = (price - stop if is_long else price + stop) if stop > 0 else None
        self._take_price = (price + take if is_long else price - take) if take > 0 else None

    def CreateClone(self):
        return adaptive_squeeze_momentum_strategy()
