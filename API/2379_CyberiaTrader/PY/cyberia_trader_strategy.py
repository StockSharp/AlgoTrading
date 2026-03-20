import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    SimpleMovingAverage as SMA,
    CommodityChannelIndex,
    AverageDirectionalIndex,
)
from StockSharp.Algo.Strategies import Strategy


class cyberia_trader_strategy(Strategy):
    def __init__(self):
        super(cyberia_trader_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12)
        self._macd_slow = self.Param("MacdSlow", 26)
        self._macd_signal = self.Param("MacdSignal", 9)
        self._ma_period = self.Param("MaPeriod", 20)
        self._cci_period = self.Param("CciPeriod", 14)
        self._adx_period = self.Param("AdxPeriod", 14)
        self._enable_macd = self.Param("EnableMacd", True)
        self._enable_ma = self.Param("EnableMa", True)
        self._enable_cci = self.Param("EnableCci", True)
        self._enable_adx = self.Param("EnableAdx", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._last_direction = 0
        self.Volume = 1

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
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def EnableMacd(self):
        return self._enable_macd.Value

    @EnableMacd.setter
    def EnableMacd(self, value):
        self._enable_macd.Value = value

    @property
    def EnableMa(self):
        return self._enable_ma.Value

    @EnableMa.setter
    def EnableMa(self, value):
        self._enable_ma.Value = value

    @property
    def EnableCci(self):
        return self._enable_cci.Value

    @EnableCci.setter
    def EnableCci(self, value):
        self._enable_cci.Value = value

    @property
    def EnableAdx(self):
        return self._enable_adx.Value

    @EnableAdx.setter
    def EnableAdx(self, value):
        self._enable_adx.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(cyberia_trader_strategy, self).OnStarted(time)

        self._last_direction = 0

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFast
        self._macd.Macd.LongMa.Length = self.MacdSlow
        self._macd.SignalMa.Length = self.MacdSignal

        self._ma = SMA()
        self._ma.Length = self.MaPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd, self._ma, self._cci, self._adx, self.ProcessCandle).Start()

        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))

    def ProcessCandle(self, candle, macd_val, ma_val, cci_val, adx_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_typed = macd_val
        adx_typed = adx_val

        disable_buy = False
        disable_sell = False

        if self.EnableMacd:
            macd = float(macd_typed.Macd) if macd_typed.Macd is not None else 0.0
            signal = float(macd_typed.Signal) if macd_typed.Signal is not None else 0.0
            if macd > signal:
                disable_sell = True
            elif macd < signal:
                disable_buy = True

        if self.EnableMa:
            ma = float(ma_val)
            if float(candle.ClosePrice) > ma:
                disable_sell = True
            elif float(candle.ClosePrice) < ma:
                disable_buy = True

        if self.EnableCci:
            cci = float(cci_val)
            if cci > 100.0:
                disable_buy = True
            elif cci < -100.0:
                disable_sell = True

        if self.EnableAdx:
            plus_val = float(adx_typed.Dx.Plus) if adx_typed.Dx.Plus is not None else 0.0
            minus_val = float(adx_typed.Dx.Minus) if adx_typed.Dx.Minus is not None else 0.0
            if plus_val > minus_val:
                disable_sell = True
            elif minus_val > plus_val:
                disable_buy = True

        if not disable_buy and disable_sell:
            direction = 1
        elif not disable_sell and disable_buy:
            direction = -1
        else:
            direction = 0

        if direction == 1 and self._last_direction != 1 and self.Position <= 0:
            self.BuyMarket()
        elif direction == -1 and self._last_direction != -1 and self.Position >= 0:
            self.SellMarket()

        self._last_direction = direction

    def OnReseted(self):
        super(cyberia_trader_strategy, self).OnReseted()
        self._last_direction = 0

    def CreateClone(self):
        return cyberia_trader_strategy()
