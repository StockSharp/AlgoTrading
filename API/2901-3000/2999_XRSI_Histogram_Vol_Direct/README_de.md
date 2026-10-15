# XRSI-Histogramm-Vol-Direkt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- **Ursprüngliche Quelle**: `Exp_XRSI_Histogram_Vol_Direct.mq5`
- **Konvertierte Plattform**: StockSharp C# High-Level-Strategie-API
- **Idee**: Umkehrungen handeln, wenn das geglättete volumengewichtete RSI-Histogramm die Steigung wechselt
- **Daten**: einzelnes Wertpapier, einzelner Zeitrahmen (Standard H4)

Die Strategie wertet einen benutzerdefinierten Oszillator aus, der aus RSI-Werten multipliziert mit Volumen aufgebaut ist. Wenn die Steigung dieses geglätteten Oszillators kippt, dreht die Strategie entweder eine Position um oder öffnet einen neuen Trade in entgegengesetzter Richtung. Die Logik repliziert den Farb-Puffer-Ansatz des ursprünglichen Expert Advisors durch Verfolgung der Steigungsrichtung der letzten zwei abgeschlossenen Kerzen.

## Indikatorstapel und Berechnungen
1. **RSI** (`RsiPeriod`) wird auf der ausgewählten Kerzenserie berechnet und durch Subtraktion von 50 um null zentriert.
2. **Volumenauswahl** verwendet entweder Tick-Zählung oder gehandeltes Volumen, gesteuert durch den Parameter `Use Tick Volume`.
3. **Volumengewichteter Oszillator** multipliziert den zentrierten RSI mit dem gewählten Volumen, was Bewegungen bei höherer Aktivität verstärkt.
4. **Glättung** wendet den ausgewählten gleitenden Durchschnitt (`SMA`, `EMA`, `SMMA`, `WMA`) mit Periode `SmoothLength` auf sowohl den Oszillator als auch den Rohvolumenstrom an. Der Indikator gilt erst als bereit, nachdem beide geglätteten Werte gebildet wurden.
5. **Steigungserkennung** vergleicht den aktuellen geglätteten Oszillatorwert mit dem vorherigen:
   - Höherer Wert → Steigungsfarbe `0` (steigend)
   - Niedrigerer Wert → Steigungsfarbe `1` (fallend)
   - Flach → vorherige Farbe beibehalten

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| Candle Type | H4 Zeitrahmen | Ziel-Kerzenabonnement. |
| RSI Period | 14 | Rückblickzeitraum für die RSI-Berechnung. |
| Smoothing Length | 12 | Periode des gleitenden Durchschnitts, der auf Oszillator und Volumen angewendet wird. |
| Smoothing Method | SMA | Typ des gleitenden Durchschnitts (`SMA`, `EMA`, `SMMA`, `WMA`). |
| Use Tick Volume | `true` | Tick-Zählung (`true`) oder gehandeltes Volumen (`false`) verwenden. |
| Allow Buy Open | `true` | Eröffnung von Long-Positionen aktivieren. |
| Allow Sell Open | `true` | Eröffnung von Short-Positionen aktivieren. |
| Allow Buy Close | `true` | Schließen von Long-Positionen bei entgegengesetztem Signal erlauben. |
| Allow Sell Close | `true` | Schließen von Short-Positionen bei entgegengesetztem Signal erlauben. |

> **Hinweis**: Im Gegensatz zum ursprünglichen MQL-Indikator sind fortgeschrittene Glätter wie JJMA oder VIDYA im StockSharp-Framework nicht verfügbar. Die Strategie exponiert daher die nächsten verfügbaren integrierten Alternativen.

## Handelsregeln
1. Warten, bis beide Glättungsindikatoren genügend Daten haben.
2. Die Steigungsfarbe der letzten zwei abgeschlossenen Kerzen bestimmen.
3. **Wenn die ältere Farbe steigend ist (`0`)**:
   - Jede offene Short-Position schließen, wenn erlaubt.
   - Wenn die neueste Farbe fallend ist (`1`) und Long-Einstiege erlaubt sind, eine Long-Position öffnen (spiegelt die Umkehrlogik des EA wider).
4. **Wenn die ältere Farbe fallend ist (`1`)**:
   - Jede offene Long-Position schließen, wenn erlaubt.
   - Wenn die neueste Farbe steigend ist (`0`) und Short-Einstiege erlaubt sind, eine Short-Position öffnen.

Die Strategie handelt effektiv den "Farbwechsel" der Histogrammsteigung und führt am Schluss der neuesten fertigen Kerze aus.

## Praktische Tipps
- Die Logik ist empfindlich gegenüber dem gewählten Zeitrahmen. Teste mehrere Intervalle, um dem Verhalten des ursprünglichen EA zu entsprechen.
- Da nur die Steigungsrichtung verwendet wird, kann das Hinzufügen eines Stop-Loss oder Take-Profits über `StartProtection` die Risikokontrolle im Live-Trading verbessern.
- Verwende die Chartvisualisierung im Terminal, um die StockSharp-Oszillatorsteigung mit dem ursprünglichen MT5-Indikator beim Validieren des Ports zu vergleichen.

## Unterschiede zur MQL-Version
- Geldmanagement-Helfer (`TradeAlgorithms.mqh`) werden nicht portiert; die StockSharp-Implementierung stützt sich auf das Basis-Strategie-Volumen.
- Es werden nur von StockSharp unterstützte Glättungsmethoden exponiert. Nicht unterstützte Modi fallen auf SMA-Verhalten zurück.
- Orders werden sofort auf der fertigen Kerze gesendet, sodass explizite Zeitverschiebung (`SignalBar` / `TimeShiftSec`) nicht erforderlich ist.
- Schutz-Stops sind nicht fest kodiert; Benutzer können sie bei Bedarf über `StartProtection` hinzufügen.

## Einschränkungen
- Erfordert eine Kerzenquelle, die entweder Tick-Zählungen oder Volumensummen liefert, um die Oszillatoramplitude korrekt zu reproduzieren.
- Die Strategie zeichnet das benutzerdefinierte Histogramm selbst nicht; sie konzentriert sich auf die Handelslogik und optionale Chartüberlagerungen für RSI.
