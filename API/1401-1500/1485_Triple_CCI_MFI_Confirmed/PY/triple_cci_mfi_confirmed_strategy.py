import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, CommodityChannelIndex, ExponentialMovingAverage, MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class triple_cci_mfi_confirmed_strategy(Strategy):
    def __init__(self):
        super(triple_cci_mfi_confirmed_strategy, self).__init__()
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 1.75) \
            .SetDisplay("ATR Stop Loss", "ATR multiplier for stop loss", "Risk Management")
        self._trailing_activation_multiplier = self.Param("TrailingActivationMultiplier", 2.25) \
            .SetDisplay("ATR Trailing Activation", "ATR multiplier to activate trailing", "Risk Management")
        self._fast_cci_period = self.Param("FastCciPeriod", 14) \
            .SetDisplay("CCI Fast Length", "Fast CCI period", "Indicators")
        self._middle_cci_period = self.Param("MiddleCciPeriod", 25) \
            .SetDisplay("CCI Middle Length", "Middle CCI period", "Indicators")
        self._slow_cci_period = self.Param("SlowCciPeriod", 50) \
            .SetDisplay("CCI Slow Length", "Slow CCI period", "Indicators")
        self._mfi_length = self.Param("MfiLength", 14) \
            .SetDisplay("MFI Length", "Money Flow Index length", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("ExponentialMovingAverage Length", "ExponentialMovingAverage filter length", "Indicators")
        self._trailing_ema_length = self.Param("TrailingEmaLength", 20) \
            .SetDisplay("Trailing ExponentialMovingAverage Length", "ExponentialMovingAverage length for trailing profit", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR calculation period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._trade_start = self.Param("TradeStart", DateTime(2023, 1, 1)) \
            .SetDisplay("Trade Start", "Start date for trading", "Time Range")
        self._trade_stop = self.Param("TradeStop", DateTime(2025, 1, 1)) \
            .SetDisplay("Trade Stop", "Stop date for trading", "Time Range")
        self._prev_fast_cci = 0.0
        self._stop_loss_level = 0.0
        self._activation_level = 0.0
        self._take_profit_level = 0.0
        self._trailing_activated = False
        self._last_signal = DateTime.MinValue

    @property
    def stop_loss_atr_multiplier(self):
        return self._stop_loss_atr_multiplier.Value

    @property
    def trailing_activation_multiplier(self):
        return self._trailing_activation_multiplier.Value

    @property
    def fast_cci_period(self):
        return self._fast_cci_period.Value

    @property
    def middle_cci_period(self):
        return self._middle_cci_period.Value

    @property
    def slow_cci_period(self):
        return self._slow_cci_period.Value

    @property
    def mfi_length(self):
        return self._mfi_length.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def trailing_ema_length(self):
        return self._trailing_ema_length.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def trade_start(self):
        return self._trade_start.Value

    @property
    def trade_stop(self):
        return self._trade_stop.Value

    def OnReseted(self):
        super(triple_cci_mfi_confirmed_strategy, self).OnReseted()
        self._prev_fast_cci = 0.0
        self._stop_loss_level = 0.0
        self._activation_level = 0.0
        self._take_profit_level = 0.0
        self._trailing_activated = False
        self._last_signal = DateTime.MinValue

    def OnStarted2(self, time):
        super(triple_cci_mfi_confirmed_strategy, self).OnStarted2(time)
        fast_cci = CommodityChannelIndex()
        fast_cci.Length = self.fast_cci_period
        middle_cci = CommodityChannelIndex()
        middle_cci.Length = self.middle_cci_period
        slow_cci = CommodityChannelIndex()
        slow_cci.Length = self.slow_cci_period
        mfi = MoneyFlowIndex()
        mfi.Length = self.mfi_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        trailing_ema = ExponentialMovingAverage()
        trailing_ema.Length = self.trailing_ema_length
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)

        def on_candle(candle, fast_cci_val, middle_cci_val, slow_cci_val, mfi_val, ema_val, trailing_ema_val, atr_val):
            if candle.State != CandleStates.Finished:
                return
            if candle.OpenTime < self.trade_start or candle.OpenTime > self.trade_stop:
                return
            fc = float(fast_cci_val)
            mc = float(middle_cci_val)
            sc = float(slow_cci_val)
            mv = float(mfi_val)
            ev = float(ema_val)
            tv = float(trailing_ema_val)
            av = float(atr_val)
            crossed_up = self._prev_fast_cci <= 0 and fc > 0
            self._prev_fast_cci = fc
            cooldown = TimeSpan.FromMinutes(1440)
            if crossed_up and float(candle.ClosePrice) > ev and mc > 0 and sc > 0 and mv > 65 and self.Position <= 0 and candle.OpenTime - self._last_signal >= cooldown:
                self.BuyMarket()
                self._last_signal = candle.OpenTime
                self._stop_loss_level = float(candle.ClosePrice) - float(self.stop_loss_atr_multiplier) * av
                self._activation_level = float(candle.ClosePrice) + float(self.trailing_activation_multiplier) * av
                self._trailing_activated = False
                self._take_profit_level = 0.0
                return
            if self.Position <= 0:
                return
            if not self._trailing_activated and float(candle.HighPrice) > self._activation_level:
                self._trailing_activated = True
            if self._trailing_activated:
                self._take_profit_level = tv
            if self._take_profit_level != 0.0 and float(candle.ClosePrice) < self._take_profit_level:
                self.SellMarket()
                self._last_signal = candle.OpenTime
                return
            if float(candle.LowPrice) <= self._stop_loss_level:
                self.SellMarket()
                self._last_signal = candle.OpenTime

        subscription.Bind(fast_cci, middle_cci, slow_cci, mfi, ema, trailing_ema, atr, on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_cci)
            self.DrawIndicator(area, middle_cci)
            self.DrawIndicator(area, slow_cci)
            self.DrawIndicator(area, mfi)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, trailing_ema)
            self.DrawOwnTrades(area)

    def CreateClone(self):
        return triple_cci_mfi_confirmed_strategy()
