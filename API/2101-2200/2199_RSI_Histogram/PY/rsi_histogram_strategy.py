import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_histogram_strategy(Strategy):
    def __init__(self):
        super(rsi_histogram_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Length of the RSI indicator", "RSI")
        self._high_level = self.Param("HighLevel", 60.0).SetDisplay("High Level", "Overbought threshold", "RSI")
        self._low_level = self.Param("LowLevel", 40.0).SetDisplay("Low Level", "Oversold threshold", "RSI")
        self._buy_pos_open = self.Param("BuyPosOpen", True).SetDisplay("Enable Buy Entry", "Allow long entries when signal appears", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True).SetDisplay("Enable Sell Entry", "Allow short entries when signal appears", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True).SetDisplay("Close Buy Positions", "Allow closing longs on opposite signal", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True).SetDisplay("Close Sell Positions", "Allow closing shorts on opposite signal", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe for RSI calculation", "General")
        self._prev_class = -1
        self._prev_prev_class = -1
    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def high_level(self): return self._high_level.Value
    @property
    def low_level(self): return self._low_level.Value
    @property
    def buy_pos_open(self): return self._buy_pos_open.Value
    @property
    def sell_pos_open(self): return self._sell_pos_open.Value
    @property
    def buy_pos_close(self): return self._buy_pos_close.Value
    @property
    def sell_pos_close(self): return self._sell_pos_close.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(rsi_histogram_strategy, self).OnReseted()
        self._prev_class = -1
        self._prev_prev_class = -1
    def OnStarted2(self, time):
        super(rsi_histogram_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished: return
        rv = float(rsi_value)
        hl = float(self.high_level)
        ll = float(self.low_level)
        if rv > hl: current_class = 0
        elif rv < ll: current_class = 2
        else: current_class = 1
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_prev_class = self._prev_class
            self._prev_class = current_class
            return
        if self._prev_prev_class == 0 and self._prev_class > 0:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
        elif self._prev_prev_class == 2 and self._prev_class < 2:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()
        self._prev_prev_class = self._prev_class
        self._prev_class = current_class
    def CreateClone(self): return rsi_histogram_strategy()
