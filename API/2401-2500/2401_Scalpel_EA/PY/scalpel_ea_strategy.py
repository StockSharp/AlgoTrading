import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class scalpel_ea_strategy(Strategy):
    def __init__(self):
        super(scalpel_ea_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 15)
        self._cci_limit = self.Param("CciLimit", 100.0)
        self._take_profit = self.Param("TakeProfit", 30.0)
        self._stop_loss = self.Param("StopLoss", 21.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._cci = None
        self._prev_high_main = 0.0
        self._prev_low_main = 0.0
        self._prev_high_h4 = 0.0
        self._prev_low_h4 = 0.0
        self._curr_high_h4 = 0.0
        self._curr_low_h4 = 0.0
        self._prev_high_h1 = 0.0
        self._prev_low_h1 = 0.0
        self._curr_high_h1 = 0.0
        self._curr_low_h1 = 0.0
        self._prev_high_m30 = 0.0
        self._prev_low_m30 = 0.0
        self._curr_high_m30 = 0.0
        self._curr_low_m30 = 0.0

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciLimit(self):
        return self._cci_limit.Value

    @CciLimit.setter
    def CciLimit(self, value):
        self._cci_limit.Value = value

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
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(scalpel_ea_strategy, self).OnStarted2(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod

        main = self.SubscribeCandles(self.CandleType)
        m30 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        h1 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        h4 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(4)))

        main.Bind(self._cci, self.ProcessMain).Start()
        m30.Bind(self.ProcessM30).Start()
        h1.Bind(self.ProcessH1).Start()
        h4.Bind(self.ProcessH4).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessM30(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._prev_high_m30 = self._curr_high_m30
        self._prev_low_m30 = self._curr_low_m30
        self._curr_high_m30 = float(candle.HighPrice)
        self._curr_low_m30 = float(candle.LowPrice)

    def ProcessH1(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._prev_high_h1 = self._curr_high_h1
        self._prev_low_h1 = self._curr_low_h1
        self._curr_high_h1 = float(candle.HighPrice)
        self._curr_low_h1 = float(candle.LowPrice)

    def ProcessH4(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._prev_high_h4 = self._curr_high_h4
        self._prev_low_h4 = self._curr_low_h4
        self._curr_high_h4 = float(candle.HighPrice)
        self._curr_low_h4 = float(candle.LowPrice)

    def ProcessMain(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_value)
        cci_buy = cci_val > 0.0 and cci_val < float(self.CciLimit)
        cci_sell = cci_val < 0.0 and -cci_val < float(self.CciLimit)

        breakout_high = float(candle.ClosePrice) > self._prev_high_main
        breakout_low = float(candle.ClosePrice) < self._prev_low_main

        if (cci_buy
                and self._curr_low_h4 > self._prev_low_h4
                and self._curr_low_h1 > self._prev_low_h1
                and self._curr_low_m30 > self._prev_low_m30
                and breakout_high
                and self.Position <= 0):
            self.BuyMarket()
        elif (cci_sell
              and self._curr_high_h4 < self._prev_high_h4
              and self._curr_high_h1 < self._prev_high_h1
              and self._curr_high_m30 < self._prev_high_m30
              and breakout_low
              and self.Position >= 0):
            self.SellMarket()

        self._prev_high_main = float(candle.HighPrice)
        self._prev_low_main = float(candle.LowPrice)

    def OnReseted(self):
        super(scalpel_ea_strategy, self).OnReseted()
        self._prev_high_main = 0.0
        self._prev_low_main = 0.0
        self._prev_high_h4 = 0.0
        self._prev_low_h4 = 0.0
        self._curr_high_h4 = 0.0
        self._curr_low_h4 = 0.0
        self._prev_high_h1 = 0.0
        self._prev_low_h1 = 0.0
        self._curr_high_h1 = 0.0
        self._curr_low_h1 = 0.0
        self._prev_high_m30 = 0.0
        self._prev_low_m30 = 0.0
        self._curr_high_m30 = 0.0
        self._curr_low_m30 = 0.0

    def CreateClone(self):
        return scalpel_ea_strategy()
