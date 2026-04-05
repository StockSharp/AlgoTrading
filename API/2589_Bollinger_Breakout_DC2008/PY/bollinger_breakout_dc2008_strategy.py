import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

PRICE_CLOSE = 0
PRICE_OPEN = 1
PRICE_HIGH = 2
PRICE_LOW = 3
PRICE_MEDIAN = 4
PRICE_TYPICAL = 5
PRICE_WEIGHTED = 6
PRICE_AVERAGE = 7

class bollinger_breakout_dc2008_strategy(Strategy):
    def __init__(self):
        super(bollinger_breakout_dc2008_strategy, self).__init__()

        self._bands_period = self.Param("BandsPeriod", 80)
        self._bands_deviation = self.Param("BandsDeviation", 3.0)
        self._applied_price = self.Param("AppliedPrice", PRICE_CLOSE)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._entry_price = 0.0

    @property
    def BandsPeriod(self):
        return self._bands_period.Value

    @BandsPeriod.setter
    def BandsPeriod(self, value):
        self._bands_period.Value = value

    @property
    def BandsDeviation(self):
        return self._bands_deviation.Value

    @BandsDeviation.setter
    def BandsDeviation(self, value):
        self._bands_deviation.Value = value

    @property
    def AppliedPrice(self):
        return self._applied_price.Value

    @AppliedPrice.setter
    def AppliedPrice(self, value):
        self._applied_price.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _get_applied_price(self, candle):
        ap = int(self.AppliedPrice)
        if ap == PRICE_OPEN:
            return candle.OpenPrice
        elif ap == PRICE_HIGH:
            return candle.HighPrice
        elif ap == PRICE_LOW:
            return candle.LowPrice
        elif ap == PRICE_MEDIAN:
            return (candle.HighPrice + candle.LowPrice) / 2
        elif ap == PRICE_TYPICAL:
            return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3
        elif ap == PRICE_WEIGHTED:
            return (candle.HighPrice + candle.LowPrice + 2 * candle.ClosePrice) / 4
        elif ap == PRICE_AVERAGE:
            return (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4
        else:
            return candle.ClosePrice

    def OnStarted2(self, time):
        super(bollinger_breakout_dc2008_strategy, self).OnStarted2(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BandsPeriod
        self._bollinger.Width = self.BandsDeviation

        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price_value = self._get_applied_price(candle)
        indicator_value = process_float(self._bollinger, price_value, candle.OpenTime, True)

        if not indicator_value.IsFinal:
            return

        up_band = indicator_value.UpBand
        low_band = indicator_value.LowBand
        moving_average = indicator_value.MovingAverage

        if up_band is None or low_band is None or moving_average is None:
            return

        upper = float(up_band)
        lower = float(low_band)
        middle = float(moving_average)

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        buy_signal = low < lower and high < middle
        sell_signal = high > upper and low > middle

        if not buy_signal and not sell_signal:
            return

        unrealized_pnl = 0.0
        if self.Position != 0:
            unrealized_pnl = self.Position * (close - self._entry_price)

        if buy_signal:
            if self.Position == 0:
                self.BuyMarket()
                self._entry_price = close
            else:
                if unrealized_pnl < 0.0:
                    return
                if self.Position < 0:
                    self.BuyMarket()
                    self._entry_price = close
            return

        if sell_signal:
            if self.Position == 0:
                self.SellMarket()
                self._entry_price = close
            else:
                if unrealized_pnl < 0.0:
                    return
                if self.Position > 0:
                    self.SellMarket()
                    self._entry_price = close

    def OnReseted(self):
        super(bollinger_breakout_dc2008_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return bollinger_breakout_dc2008_strategy()
