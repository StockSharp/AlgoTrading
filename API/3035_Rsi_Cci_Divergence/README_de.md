# RSI & CCI Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **RSI & CCI Divergenz-Strategie** ist eine Konvertierung des MetaTrader-Expert Advisors `RSI&CCI_DIVERGENCE.mq4` (MQL ID 22266). Das System sucht nach bärischen oder bullischen Divergenzen zwischen Kurs-Hochs und zwei Oszillatoren (Commodity Channel Index und Relative Strength Index), filtert sie mit einem linearen gewichteten gleitenden Durchschnitts-Trendfilter, validiert das Signal mit MACD-Ausrichtung auf drei verschiedenen Zeitrahmen und bestätigt die Momentum-Stärke mithilfe eines Momentum-Oszillators auf einem höheren Zeitrahmen. Optionale absolute Stop-Loss- und Take-Profit-Ziele können angewendet werden, um offene Positionen zu verwalten.

Die StockSharp-Implementierung konzentriert sich auf die High-Level-API. Indikatoren sind direkt an Kerzenabonnements gebunden und alle Berechnungen werden durch Streaming-Kerzenaktualisierungen ohne manuelle Indikatorwertabfrage angetrieben.

## Handelslogik
1. **Trendfilter**
   - Schnelle und langsame lineare gewichtete gleitende Durchschnitte (LWMA) auf dem primären Zeitrahmen definieren die vorherrschende Richtung.
   - Bullischer Kontext erfordert, dass der schnelle LWMA über dem langsamen LWMA liegt; bärischer Kontext erfordert das Gegenteil.

2. **Divergenzerkennung**
   - Die zuletzt geschlossene Kerze wird mit bis zu `CandlesToRetrace` vorherigen Kerzen verglichen.
   - Ein bullisches Signal tritt auf, wenn CCI oder RSI ein höheres Tief macht, während die entsprechende frühere Kerze ein höheres Hoch als das letzte Hoch zeigt (bullische Divergenz).
   - Ein bärisches Signal tritt auf, wenn CCI oder RSI ein niedrigeres Hoch macht, während die entsprechende frühere Kerze ein niedrigeres Hoch als das letzte Hoch zeigt (bärische Divergenz).

3. **MACD-Bestätigung**
   - MACD (standardmäßig 12, 26, 9) wird auf primären, höheren und Makro-Zeitrahmen ausgewertet.
   - Long-Trades erfordern, dass MACD auf allen Zeitrahmen über der Signallinie liegt.
   - Short-Trades erfordern, dass MACD auf allen Zeitrahmen unter der Signallinie liegt.

4. **Momentum-Bestätigung**
   - Ein Momentum-Oszillator (standardmäßig Länge 14) wird auf einem höheren Zeitrahmen (standardmäßig 1 Stunde) abgetastet.
   - Die absolute Abweichung der jüngsten Momentum-Lesungen vom neutralen Niveau 100 muss die konfigurierten Kauf-/Verkaufsschwellenwerte überschreiten, um den Trade zu genehmigen.

5. **Preisstruktursicherung**
   - Die Strategie prüft jüngste Hochs/Tiefs, um die ursprünglichen EA-Einschränkungen nachzuahmen (`Low[2] < High[1]` für Longs und `Low[1] < High[2]` für Shorts).

6. **Orderausführung**
   - Wenn alle Filter übereinstimmen, tritt die Strategie mit `BuyMarket` oder `SellMarket` ein, mit einem Volumen gleich dem Basis-Strategie-Volumen plus dem Absolutwert der aktuellen Position, was eine sofortige Umkehr ermöglicht.

7. **Risikomanagement**
   - Optionale absolute Stop-Loss- und Take-Profit-Abstände werden bei jeder abgeschlossenen Kerze ausgewertet.
   - Wenn konfiguriert, sendet die Strategie eine Marktorder, um die Position zu glätten, wenn der Stop oder das Ziel berührt wird.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `FastMaLength` | 6 | Periode für den schnellen LWMA-Trendfilter. |
| `SlowMaLength` | 85 | Periode für den langsamen LWMA-Trendfilter. |
| `CciLength` | 14 | Rückblick für den Commodity Channel Index. |
| `RsiLength` | 14 | Rückblick für den Relative Strength Index. |
| `CandlesToRetrace` | 10 | Anzahl abgeschlossener Kerzen zur Divergenzerkennung. |
| `MacdFastPeriod` | 12 | Schnelle gleitende Durchschnittsperiode in der MACD-Berechnung. |
| `MacdSlowPeriod` | 26 | Langsame gleitende Durchschnittsperiode in der MACD-Berechnung. |
| `MacdSignalPeriod` | 9 | Signallinienperiode für MACD. |
| `MomentumLength` | 14 | Länge des Momentum-Oszillators auf höherem Zeitrahmen. |
| `MomentumBuyThreshold` | 0.3 | Minimale absolute Abweichung von 100 für bullische Momentum-Bestätigung. |
| `MomentumSellThreshold` | 0.3 | Minimale absolute Abweichung von 100 für bärische Momentum-Bestätigung. |
| `StopLoss` | 0 | Absoluter Preisabstand für einen optionalen Stop-Loss (0 deaktiviert den Stop). |
| `TakeProfit` | 0 | Absoluter Preisabstand für ein optionales Take-Profit (0 deaktiviert das Ziel). |
| `CandleType` | 15-Minuten-Zeitrahmen | Primärer Kerzentyp für Divergenz- und Trendanalyse. |
| `MomentumCandleType` | 1-Stunden-Zeitrahmen | Kerzentyp für die Momentum-Bestätigung. |
| `HigherMacdCandleType` | 1-Stunden-Zeitrahmen | Sekundärer Zeitrahmen für MACD-Bestätigung. |
| `MacroMacdCandleType` | 30-Tage-Zeitrahmen | Makro-Zeitrahmen für MACD-Bestätigung (an Datenverfügbarkeit des Instruments anpassen). |

## Nutzungshinweise
- Stellen Sie sicher, dass alle referenzierten Zeitrahmen vom Datenprovider verfügbar sind; passen Sie sonst die Kerzentyp-Parameter entsprechend an.
- Die Standard-Stop-Loss- und Take-Profit-Werte sind deaktiviert, um das ursprüngliche EA-Verhalten widerzuspiegeln, bei dem das Risiko über Trailing und Eigenkapital-Stops verwaltet wurde. Setzen Sie positive Dezimalwerte, um harte Stops zu aktivieren.
- Da die Momentum-Bestätigung Werte mit der 100er-Basislinie vergleicht, setzt sie voraus, dass der StockSharp-`Momentum`-Indikator die klassische Definition verwendet (`100 * Close / Close[N]`). Wenn eine andere Normalisierung bevorzugt wird, passen Sie die Schwellenwerte an die Volatilität des Instruments an.
- Die Strategie sendet Marktorders sowohl für Einstiege als auch für Ausstiege und spiegelt damit die unmittelbare Ausführungslogik des Quell-Expert-Advisors wider.

## Konvertierungshinweise
- Die Konvertierung verwendet StockSharp's High-Level-Indikator-Binding. Keine manuellen Aufrufe von `GetValue` erforderlich; Indikatorwerte werden durch die Binding-Callbacks bereitgestellt.
- Eigenkapital-basiertes Stop-Management, Trailing-Logik und E-Mail-/Benachrichtigungsfunktionen aus der MQL-Quelle werden nicht portiert. Stattdessen liegt der Fokus auf der primären Signalgenerierung und dem grundlegenden Stop/Ziel-Handling.
- Die Divergenzerkennung wird mit leichtgewichtigen Listen implementiert, um die jüngste Preis- und Indikatorhistorie für die Mustererkennung zu pflegen.
