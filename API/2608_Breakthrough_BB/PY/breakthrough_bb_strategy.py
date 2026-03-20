import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class breakthrough_bb_strategy(Strategy):
    def __init__(self):
        super(breakthrough_bb_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 9)
        self._bands_period = self.Param("BandsPeriod", 28)
        self._deviation = self.Param("Deviation", 1.6)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._close_lag0 = None
        self._close_lag1 = None
        self._close_lag2 = None
        self._close_lag3 = None
        self._ma_lag0 = None
        self._ma_lag1 = None
        self._ma_lag2 = None
        self._ma_lag3 = None

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def BandsPeriod(self):
        return self._bands_period.Value

    @BandsPeriod.setter
    def BandsPeriod(self, value):
        self._bands_period.Value = value

    @property
    def Deviation(self):
        return self._deviation.Value

    @Deviation.setter
    def Deviation(self, value):
        self._deviation.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(breakthrough_bb_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MaPeriod
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BandsPeriod
        self._bollinger.Width = self.Deviation

        self._close_lag0 = None
        self._close_lag1 = None
        self._close_lag2 = None
        self._close_lag3 = None
        self._ma_lag0 = None
        self._ma_lag1 = None
        self._ma_lag2 = None
        self._ma_lag3 = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        sma_result = self._sma.Process(self._sma.CreateValue(candle.OpenTime, close))
        bb_result = self._bollinger.Process(self._bollinger.CreateValue(candle.OpenTime, close))

        if not sma_result.IsFinal or not bb_result.IsFinal:
            return

        sma_val = float(sma_result)

        up_band = bb_result.UpBand
        low_band = bb_result.LowBand
        mid_band = bb_result.MovingAverage

        if up_band is None or low_band is None or mid_band is None:
            self._update_history(close, sma_val)
            return

        upper = float(up_band)
        lower = float(low_band)
        middle = float(mid_band)

        if not self._sma.IsFormed or not self._bollinger.IsFormed:
            self._update_history(close, sma_val)
            return

        # Exit on middle band cross
        if self.Position > 0 and close < middle:
            self.SellMarket()
            self._update_history(close, sma_val)
            return

        if self.Position < 0 and close > middle:
            self.BuyMarket()
            self._update_history(close, sma_val)
            return

        ma_prev4 = self._ma_lag2
        close_prev4 = self._close_lag2

        if ma_prev4 is None or close_prev4 is None:
            self._update_history(close, sma_val)
            return

        if self.Position == 0:
            if close_prev4 < upper and close > upper and sma_val > ma_prev4:
                self.BuyMarket()
                self._update_history(close, sma_val)
                return

            if close_prev4 > lower and close < lower and sma_val < ma_prev4:
                self.SellMarket()
                self._update_history(close, sma_val)
                return

        self._update_history(close, sma_val)

    def _update_history(self, close, ma_value):
        self._ma_lag3 = self._ma_lag2
        self._ma_lag2 = self._ma_lag1
        self._ma_lag1 = self._ma_lag0
        self._ma_lag0 = ma_value

        self._close_lag3 = self._close_lag2
        self._close_lag2 = self._close_lag1
        self._close_lag1 = self._close_lag0
        self._close_lag0 = close

    def OnReseted(self):
        super(breakthrough_bb_strategy, self).OnReseted()
        self._close_lag0 = None
        self._close_lag1 = None
        self._close_lag2 = None
        self._close_lag3 = None
        self._ma_lag0 = None
        self._ma_lag1 = None
        self._ma_lag2 = None
        self._ma_lag3 = None

    def CreateClone(self):
        return breakthrough_bb_strategy()
