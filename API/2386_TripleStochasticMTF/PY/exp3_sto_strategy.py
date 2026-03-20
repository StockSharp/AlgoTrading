import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class exp3_sto_strategy(Strategy):
    def __init__(self):
        super(exp3_sto_strategy, self).__init__()
        self._candle_type1 = self.Param("CandleType1", DataType.TimeFrame(TimeSpan.FromDays(1)))
        self._candle_type2 = self.Param("CandleType2", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._candle_type3 = self.Param("CandleType3", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._k_period = self.Param("KPeriod", 5)
        self._d_period = self.Param("DPeriod", 3)
        self._slowing = self.Param("Slowing", 3)
        self._buy_pos_open = self.Param("BuyPosOpen", True)
        self._sell_pos_open = self.Param("SellPosOpen", True)
        self._buy_pos_close1 = self.Param("BuyPosClose1", False)
        self._sell_pos_close1 = self.Param("SellPosClose1", False)
        self._buy_pos_close2 = self.Param("BuyPosClose2", False)
        self._sell_pos_close2 = self.Param("SellPosClose2", False)
        self._buy_pos_close3 = self.Param("BuyPosClose3", False)
        self._sell_pos_close3 = self.Param("SellPosClose3", False)
        self._trend1 = 0
        self._trend2 = 0
        self._trend3 = 0
        self._prev_k3 = None
        self._prev_d3 = None
        self._buy_open_signal = False
        self._sell_open_signal = False
        self._buy_close_signal = False
        self._sell_close_signal = False

    @property
    def CandleType1(self): return self._candle_type1.Value
    @CandleType1.setter
    def CandleType1(self, v): self._candle_type1.Value = v
    @property
    def CandleType2(self): return self._candle_type2.Value
    @CandleType2.setter
    def CandleType2(self, v): self._candle_type2.Value = v
    @property
    def CandleType3(self): return self._candle_type3.Value
    @CandleType3.setter
    def CandleType3(self, v): self._candle_type3.Value = v
    @property
    def KPeriod(self): return self._k_period.Value
    @KPeriod.setter
    def KPeriod(self, v): self._k_period.Value = v
    @property
    def DPeriod(self): return self._d_period.Value
    @DPeriod.setter
    def DPeriod(self, v): self._d_period.Value = v
    @property
    def Slowing(self): return self._slowing.Value
    @Slowing.setter
    def Slowing(self, v): self._slowing.Value = v
    @property
    def BuyPosOpen(self): return self._buy_pos_open.Value
    @BuyPosOpen.setter
    def BuyPosOpen(self, v): self._buy_pos_open.Value = v
    @property
    def SellPosOpen(self): return self._sell_pos_open.Value
    @SellPosOpen.setter
    def SellPosOpen(self, v): self._sell_pos_open.Value = v
    @property
    def BuyPosClose1(self): return self._buy_pos_close1.Value
    @BuyPosClose1.setter
    def BuyPosClose1(self, v): self._buy_pos_close1.Value = v
    @property
    def SellPosClose1(self): return self._sell_pos_close1.Value
    @SellPosClose1.setter
    def SellPosClose1(self, v): self._sell_pos_close1.Value = v
    @property
    def BuyPosClose2(self): return self._buy_pos_close2.Value
    @BuyPosClose2.setter
    def BuyPosClose2(self, v): self._buy_pos_close2.Value = v
    @property
    def SellPosClose2(self): return self._sell_pos_close2.Value
    @SellPosClose2.setter
    def SellPosClose2(self, v): self._sell_pos_close2.Value = v
    @property
    def BuyPosClose3(self): return self._buy_pos_close3.Value
    @BuyPosClose3.setter
    def BuyPosClose3(self, v): self._buy_pos_close3.Value = v
    @property
    def SellPosClose3(self): return self._sell_pos_close3.Value
    @SellPosClose3.setter
    def SellPosClose3(self, v): self._sell_pos_close3.Value = v

    def OnStarted(self, time):
        super(exp3_sto_strategy, self).OnStarted(time)
        self._stoch1 = StochasticOscillator()
        self._stoch1.K.Length = self.KPeriod
        self._stoch1.D.Length = self.DPeriod
        self._stoch2 = StochasticOscillator()
        self._stoch2.K.Length = self.KPeriod
        self._stoch2.D.Length = self.DPeriod
        self._stoch3 = StochasticOscillator()
        self._stoch3.K.Length = self.KPeriod
        self._stoch3.D.Length = self.DPeriod
        sub1 = self.SubscribeCandles(self.CandleType1)
        sub1.BindEx(self._stoch1, self.ProcessTf1).Start()
        sub2 = self.SubscribeCandles(self.CandleType2)
        sub2.BindEx(self._stoch2, self.ProcessTf2).Start()
        sub3 = self.SubscribeCandles(self.CandleType3)
        sub3.BindEx(self._stoch3, self.ProcessTf3).Start()

    def ProcessTf1(self, candle, sv):
        if candle.State != CandleStates.Finished: return
        if sv.K is None or sv.D is None: return
        k, d = float(sv.K), float(sv.D)
        self._trend1 = 0
        if k > d and self.BuyPosOpen: self._trend1 = 1
        elif k < d and self.SellPosOpen: self._trend1 = -1
        self._update_signals()

    def ProcessTf2(self, candle, sv):
        if candle.State != CandleStates.Finished: return
        if sv.K is None or sv.D is None: return
        k, d = float(sv.K), float(sv.D)
        self._trend2 = 0
        if k > d and self.BuyPosOpen: self._trend2 = 1
        elif k < d and self.SellPosOpen: self._trend2 = -1
        self._update_signals()

    def ProcessTf3(self, candle, sv):
        if candle.State != CandleStates.Finished: return
        if sv.K is None or sv.D is None: return
        k, d = float(sv.K), float(sv.D)
        if self._prev_k3 is None or self._prev_d3 is None:
            self._prev_k3 = k
            self._prev_d3 = d
            return
        pk, pdv = self._prev_k3, self._prev_d3
        self._trend3 = 0
        if pk > pdv:
            self._trend3 = 1
            if self.BuyPosOpen and k <= d and self._trend1 > 0 and self._trend2 > 0:
                self._buy_open_signal = True
        elif pk < pdv:
            self._trend3 = -1
            if self.SellPosOpen and k >= d and self._trend1 < 0 and self._trend2 < 0:
                self._sell_open_signal = True
        self._prev_k3 = k
        self._prev_d3 = d
        self._update_signals()

    def _update_signals(self):
        self._buy_close_signal = False
        self._sell_close_signal = False
        if self._trend1 > 0 and self.SellPosClose1: self._sell_close_signal = True
        if self._trend1 < 0 and self.BuyPosClose1: self._buy_close_signal = True
        if self._trend2 > 0 and self.SellPosClose2: self._sell_close_signal = True
        if self._trend2 < 0 and self.BuyPosClose2: self._buy_close_signal = True
        if self._trend3 > 0 and self.SellPosClose3: self._sell_close_signal = True
        if self._trend3 < 0 and self.BuyPosClose3: self._buy_close_signal = True
        self._execute_trades()

    def _execute_trades(self):
        if self._buy_open_signal and self.Position <= 0:
            self.BuyMarket()
            self._buy_open_signal = False
        elif self._sell_open_signal and self.Position >= 0:
            self.SellMarket()
            self._sell_open_signal = False
        elif self._buy_close_signal and self.Position > 0:
            self.SellMarket()
        elif self._sell_close_signal and self.Position < 0:
            self.BuyMarket()
        self._buy_close_signal = False
        self._sell_close_signal = False

    def OnReseted(self):
        super(exp3_sto_strategy, self).OnReseted()
        self._trend1 = 0
        self._trend2 = 0
        self._trend3 = 0
        self._prev_k3 = None
        self._prev_d3 = None
        self._buy_open_signal = False
        self._sell_open_signal = False
        self._buy_close_signal = False
        self._sell_close_signal = False

    def CreateClone(self):
        return exp3_sto_strategy()
