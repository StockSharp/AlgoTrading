# ColorXPWMA Digit Multi-Zeitrahmen Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie konvertiert den MetaTrader 5 Expert Advisor **Exp_ColorXPWMA_Digit_NN3_MMRec** in die StockSharp High-Level-API. Der ursprüngliche Roboter betreibt drei unabhängige Module, die auf verschiedenen Zeitrahmen handeln, indem sie die digitale Färbung des ColorXPWMA gleitenden Durchschnitts analysieren. Der StockSharp-Port behält das gleiche Verhalten bei: Jedes Modul beobachtet seine eigene Kerzenserie, schließt Positionen wenn der Indikator die Farbe wechselt und öffnet optional einen neuen Trade in der erkannten Richtung.

Die Standardkonfiguration folgt der MT5-Vorlage:

| Modul | Zeitrahmen | Stop Loss (Punkte) | Take Profit (Punkte) |
| ----- | ---------- | ------------------ | -------------------- |
| A | 8 Stunden | 3000 | 10000 |
| B | 4 Stunden | 2000 | 6000 |
| C | 1 Stunde | 1000 | 3000 |

Jedes Modul kann für Long- und Short-Einstiege oder -Ausstiege durch dedizierte boolesche Parameter aktiviert oder deaktiviert werden. Die Implementierung hält individuelles Positions-Tracking pro Modul aufrecht, damit gleichzeitige Long- und Short-Trades coexistieren können, ohne die Volumen-Abrechnung der anderen Zeitrahmen zu stören.

## ColorXPWMA Digit Indikator
Der ColorXPWMA Digit Indikator emuliert den benutzerdefinierten MT5-Indikator. Für jede abgeschlossene Kerze führt der Algorithmus:

1. Baut einen potenzgewichteten Durchschnitt des ausgewählten angewendeten Preises (`Period` und `Power`).
2. Glättet den Wert mit dem gewählten gleitenden Durchschnitt (`SmoothMethods` und `SmoothLength`).
3. Rundet das Ergebnis auf die konfigurierte Anzahl von Dezimalstellen (`Digit`).
4. Weist einen Farbcode zu: **2** wenn der geglättete Wert zunimmt, **0** wenn er abnimmt, andernfalls wird die vorherige Farbe wiederverwendet.

`SignalBar` steuert, welcher historische Balken inspiziert wird. Wert `0` verwendet die zuletzt geschlossene Kerze, Wert `1` die vorherige Kerze, etc. Eine Kaufgelegenheit erscheint, wenn der beobachtete Balken auf Farbe `2` wechselt, nachdem er auf dem vorherigen Balken anders war. Eine Verkaufsgelegenheit wird generiert, wenn die Farbe `0` wird, nachdem sie auf dem vorherigen Balken anders war.

Glättungsmethoden werden wie folgt auf StockSharp-Indikatoren abgebildet:

- `Sma`, `Ema`, `Smma`, `Lwma`, `Jjma` → entsprechende StockSharp gleitende Durchschnitte.
- `T3` → interne Tillson T3 Implementierung.
- `Vidya` → interne VIDYA Implementierung, angetrieben durch den Chande Momentum Oszillator.
- `Ama` → Kaufman Adaptive Moving Average.
- Nicht unterstützte Optionen (`JurX`, `Parabolic`) fallen auf den einfachen gleitenden Durchschnitt zurück, was dem Verhalten der ursprünglichen Vorlage entspricht, wenn exotische Glätter nicht verfügbar sind.

## Trade-Management und Geldmanagement
Für jedes Modul hält die Strategie zwei unabhängige virtuelle Positionen (Long und Short). Wenn ein Modul ein Schließsignal erhält, sendet die Strategie eine Marktorder gleich dem verbleibenden Volumen dieser virtuellen Position. Eröffnungsorders werden ignoriert, während eine entgegengesetzte Position noch offen ist.

Das Positions-Sizing kopiert den MT5 Money-Management-Helfer:

- `NormalMM` definiert das Basisvolumen.
- `SmallMM` ersetzt das Basisvolumen, wenn jüngste Trades mindestens `LossTrigger` Verluste innerhalb der letzten `TotalTrigger` Trades für diese Richtung aufgezeichnet haben.

Die Logik wird separat für Long- und Short-Sequenzen ausgewertet. Handelsergebnisse werden vom durchschnittlichen gefüllten Preis berechnet, wenn ein Modul seine virtuelle Position vollständig schließt.

Das Risikomanagement spiegelt die MT5-Stops in Preispunkten wider:

- Wenn eine Long-Position offen ist und Kerzen-Tiefs `entry - StopLoss * PriceStep` kreuzen, wird die Long-Position sofort geschlossen.
- Wenn Kerzen-Hochs `entry + TakeProfit * PriceStep` berühren, werden Gewinne genommen.
- Die Regeln werden für Shorts gespiegelt (`entry + StopLoss` für Schutz, `entry - TakeProfit` für Ziele).

## Parameter
Alle Parameter werden durch `StrategyParam<T>` Objekte exponiert und können vom StockSharp-Designer aus optimiert werden. Sie sind pro Modul (A, B, C) gruppiert. Die folgende Tabelle listet die Einstellungen für ein beliebiges Modul **X** auf:

| Parameter | Beschreibung |
| --------- | ----------- |
| `X_CandleType` | Zu abonnierende Kerzenserie (Standard-Zeitrahmen oben gezeigt). |
| `X_Period`, `X_Power` | Potenzgewichtetes Fenster zum Aufbau des Basis-XPWMA-Werts. |
| `X_SmoothMethod`, `X_SmoothLength`, `X_SmoothPhase` | Auf den gewichteten Preis angewendeter Glätter. `SmoothPhase` wird für Kompatibilität mit MT5 JJMA-Benutzern beibehalten. |
| `X_AppliedPrice` | Preisquelle (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow, DeMark). |
| `X_Digit` | Rundungspräzision auf den geglätteten Wert angewendet. |
| `X_SignalBar` | Historischer Balken für Signalauswertung. |
| `X_BuyMagic`, `X_SellMagic` | Für Nachverfolgbarkeit beibehalten (in Order-Kommentaren verwendet). |
| `X_BuyTotalTrigger`, `X_BuyLossTrigger` | Long-seitige Geldmanagement-Schwellenwerte. |
| `X_SellTotalTrigger`, `X_SellLossTrigger` | Short-seitige Geldmanagement-Schwellenwerte. |
| `X_SmallMM`, `X_NormalMM` | Vom Geldmanagement-Regelwerk verwendete Volumen. |
| `X_MarginMode`, `X_Deviation` | Für Feature-Parität beigehaltene reservierte Felder; ändern StockSharp-Orders nicht. |
| `X_StopLoss`, `X_TakeProfit` | In Preisschritten auf die virtuelle Modulposition angewendete Abstände. |
| `X_BuyOpen`, `X_SellOpen`, `X_SellClose`, `X_BuyClose` | Berechtigungsschalter für Modulaktionen. |

## Hinweise
- Jede Marktorder ist mit `A|BuyOpen`, `B|SellClose`, etc. annotiert, damit Fills auf ihr Modul zurückverfolgt werden können.
- Die Strategie operiert ausschließlich auf fertigen Kerzen und reproduziert daher automatisch den von der High-Level-API bereitgestellten MT5 `IsNewBar`-Schutz.
- Wenn mehrere Module auf demselben Balken auslösen, werden ihre Volumen sequenziell mit den virtuellen Positionspuffern pro Modul verarbeitet.
