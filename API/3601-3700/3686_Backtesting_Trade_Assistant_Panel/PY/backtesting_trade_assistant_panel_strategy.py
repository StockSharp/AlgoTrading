import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class backtesting_trade_assistant_panel_strategy(Strategy):
    def __init__(self):
        super(backtesting_trade_assistant_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 100.0)

        self._sma = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    def OnReseted(self):
        super(backtesting_trade_assistant_panel_strategy, self).OnReseted()
        self._sma = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 0.0001
        step = sec.PriceStep
        if step is None or float(step) <= 0:
            return 0.0001
        step_val = float(step)
        decimals = sec.Decimals
        if decimals is not None and (int(decimals) == 5 or int(decimals) == 3):
            return step_val * 10.0
        return step_val

    def OnStarted2(self, time):
        super(backtesting_trade_assistant_panel_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()

        self._sma = SimpleMovingAverage()
        self._sma.Length = 20

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self._process_candle).Start()

    def _reset_position(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormed:
            return

        price = float(candle.ClosePrice)
        sma_val = float(sma_value)

        # Check stop-loss and take-profit
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                if self._stop_price is not None and price <= self._stop_price:
                    self.SellMarket(abs(float(self.Position)))
                    self._reset_position()
                    return
                if self._take_price is not None and price >= self._take_price:
                    self.SellMarket(abs(float(self.Position)))
                    self._reset_position()
                    return
            elif self.Position < 0:
                if self._stop_price is not None and price >= self._stop_price:
                    self.BuyMarket(abs(float(self.Position)))
                    self._reset_position()
                    return
                if self._take_price is not None and price <= self._take_price:
                    self.BuyMarket(abs(float(self.Position)))
                    self._reset_position()
                    return

        # Entry: SMA crossover
        if self.Position == 0:
            pip = self._pip_size if self._pip_size > 0 else 1.0
            sl_pips = float(self.StopLossPips)
            tp_pips = float(self.TakeProfitPips)

            if price > sma_val:
                self.BuyMarket()
                self._entry_price = price
                self._stop_price = price - sl_pips * pip if sl_pips > 0 else None
                self._take_price = price + tp_pips * pip if tp_pips > 0 else None
            elif price < sma_val:
                self.SellMarket()
                self._entry_price = price
                self._stop_price = price + sl_pips * pip if sl_pips > 0 else None
                self._take_price = price - tp_pips * pip if tp_pips > 0 else None

    def CreateClone(self):
        return backtesting_trade_assistant_panel_strategy()
