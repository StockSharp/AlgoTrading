import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class boll_trade_strategy(Strategy):
    def __init__(self):
        super(boll_trade_strategy, self).__init__()

        self._take_profit = self.Param("TakeProfit", 3.0)
        self._stop_loss = self.Param("StopLoss", 20.0)
        self._band_offset = self.Param("BollingerDistance", 0.0)
        self._bollinger_period = self.Param("BollingerPeriod", 20)
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.0)
        self._lots = self.Param("Lots", 1.0)
        self._lot_increase = self.Param("LotIncrease", True)
        self._max_volume = self.Param("MaxVolume", 500.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._pip_size = 1.0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def BollingerDistance(self):
        return self._band_offset.Value

    @BollingerDistance.setter
    def BollingerDistance(self, value):
        self._band_offset.Value = value

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
    def Lots(self):
        return self._lots.Value

    @Lots.setter
    def Lots(self, value):
        self._lots.Value = value

    @property
    def LotIncrease(self):
        return self._lot_increase.Value

    @LotIncrease.setter
    def LotIncrease(self, value):
        self._lot_increase.Value = value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @MaxVolume.setter
    def MaxVolume(self, value):
        self._max_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(boll_trade_strategy, self).OnStarted(time)

        self.Volume = self._lots.Value

        self._pip_size = self._calculate_pip_size()
        self._lot_baseline = 0.0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None

        if self.LotIncrease and float(self.Lots) > 0.0:
            portfolio = self.Portfolio
            balance = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
            if balance > 0.0:
                self._lot_baseline = balance / float(self.Lots)

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, self.ProcessCandle).Start()

    def _calculate_pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        if step < 0.01:
            step *= 10.0
        return step

    def ProcessCandle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        upper_band = value.UpBand
        lower_band = value.LowBand

        if upper_band is None or lower_band is None:
            return

        upper = float(upper_band)
        lower = float(lower_band)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        offset = self._pip_size * float(self.BollingerDistance)
        upper_threshold = upper + offset
        lower_threshold = lower - offset

        should_buy = close < lower_threshold
        should_sell = close > upper_threshold

        if self.Position == 0:
            if should_buy:
                self._enter_long(close)
            elif should_sell:
                self._enter_short(close)
            return

        if self.Position > 0:
            if (self._long_stop is not None and low <= self._long_stop) or \
               (self._long_target is not None and high >= self._long_target):
                self.SellMarket()
                self._reset_stops()
        elif self.Position < 0:
            if (self._short_stop is not None and high >= self._short_stop) or \
               (self._short_target is not None and low <= self._short_target):
                self.BuyMarket()
                self._reset_stops()

    def _calculate_volume(self):
        base_volume = float(self.Lots)
        if not self.LotIncrease or self._lot_baseline <= 0.0:
            return base_volume
        portfolio = self.Portfolio
        balance = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
        if balance <= 0.0:
            return base_volume
        scaled = base_volume * (balance / self._lot_baseline)
        return min(scaled, float(self.MaxVolume))

    def _enter_long(self, close):
        volume = self._calculate_volume()
        if volume <= 0.0:
            return

        self.BuyMarket(volume)

        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)

        self._long_stop = close - self._pip_size * sl if sl > 0.0 else None
        self._long_target = close + self._pip_size * tp if tp > 0.0 else None
        self._short_stop = None
        self._short_target = None

    def _enter_short(self, close):
        volume = self._calculate_volume()
        if volume <= 0.0:
            return

        self.SellMarket(volume)

        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)

        self._short_stop = close + self._pip_size * sl if sl > 0.0 else None
        self._short_target = close - self._pip_size * tp if tp > 0.0 else None
        self._long_stop = None
        self._long_target = None

    def _reset_stops(self):
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None

    def OnReseted(self):
        super(boll_trade_strategy, self).OnReseted()
        self._pip_size = 1.0
        self._reset_stops()

    def CreateClone(self):
        return boll_trade_strategy()
