# FT CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader 5 Expert Advisors "FT_CCI (barabashkakvn's edition)". Sie verwendet den Commodity Channel Index (CCI), um scharfe Umkehrungen zu erfassen, sobald der Oszillator weit von seinem Mittelwert entfernt ist. Das System spiegelt die ursprüngliche Logik wider: Wenn der CCI das untere Band durchbricht, wechselt er auf Long, und wenn er das obere Band durchbricht, wechselt er auf Short. Optionale Stop-Loss- und Take-Profit-Werte werden in Pips eingegeben und automatisch in Preisoffsets umgewandelt.

## Übersicht
- **Kernindikatoren**: Commodity Channel Index mit konfigurierbarer Mittelungsperiode (Standard 14).
- **Ausrichtung**: Symmetrisch Long/Short. Die Strategie hält höchstens eine Netto-Position und kehrt bei entgegengesetzten Signalen um.
- **Ausführung**: Market-Orders beim Schließen abgeschlossener Kerzen des ausgewählten Zeitrahmens.
- **Risikomanagement**: Optionale Stop-Loss- und Take-Profit-Abstände in Pips. Bei Nullwert wird der entsprechende Schutz deaktiviert.
- **Standard-Zeitrahmen**: 30-Minuten-Kerzen (entspricht der `Period()`-Auswahl im ursprünglichen Expert).

## Funktionsweise
### Long-Aufstellung
1. Abonnieren Sie abgeschlossene Kerzen des ausgewählten Zeitrahmens.
2. Aktualisieren Sie den CCI-Indikator mit typischen Preiswerten.
3. Wenn der neueste CCI-Wert am oder unterhalb des konfigurierten unteren Schwellenwerts liegt (Standard -210):
   - Schließen Sie jede offene Short-Exposure.
   - Eröffnen oder ergänzen Sie eine Long-Position mit dem konfigurierten Handelsvolumen.
4. Halten Sie die Position, bis entweder ein entgegengesetztes Short-Setup auslöst, ein Stop-Loss/Take-Profit-Ereignis auftritt oder die Strategie manuell gestoppt wird.

### Short-Aufstellung
1. Überwachen Sie dieselben CCI-Werte an abgeschlossenen Kerzen.
2. Wenn der Indikator am oder oberhalb des oberen Schwellenwerts liegt (Standard +210):
   - Schließen Sie jede offene Long-Exposure.
   - Eröffnen oder ergänzen Sie eine Short-Position mit dem konfigurierten Volumen.
3. Halten Sie den Short, bis eine entgegengesetzte Long-Bedingung ausgelöst wird oder Schutzorders den Trade schließen.

### Trade-Management
- Stop-Loss- und Take-Profit-Abstände werden in Pips definiert. Die Strategie multipliziert sie mit der erkannten Pip-Größe (Preisschritt, multipliziert mit 10 für 3- und 5-stellige Forex-Symbole), um einen absoluten Preisoffset zu erhalten, bevor StockSharp's integrierter `StartProtection` aktiviert wird.
- Da der Schutz einmalig beim Start angewendet wird, erbt jede neue Position sofort dieselben Stop- und Zielwerte relativ zum Ausführungspreis.
- Positionswechsel werden über Market-Orders ausgeführt, die auf `konfiguriertes Volumen + |aktuelle Position|` dimensioniert sind, wodurch das Umkehren einer Position sowohl die aktuelle Exposure schließt als auch die neue in einer einzigen Transaktion eröffnet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| **Candle Type** | Zeitrahmen für Berechnungen und Signalgenerierung. |
| **Trade Volume** | Lotgröße für neue Positionen. Wird zusammen mit dem aktuellen Positionswert zur Dimensionierung von Umkehrtrades verwendet. |
| **CCI Period** | Mittelungslänge des Commodity Channel Index. |
| **CCI Upper Threshold** | CCI-Niveau, das Short-Einstiege auslöst. |
| **CCI Lower Threshold** | CCI-Niveau, das Long-Einstiege auslöst. |
| **Stop Loss (pips)** | Abstand zum Schutz-Stop in Pips. Auf 0 setzen zum Deaktivieren. |
| **Take Profit (pips)** | Abstand zum Gewinnziel in Pips. Auf 0 setzen zum Deaktivieren. |

Alle Parameter unterstützen die Optimierung über StockSharp's Parametermanager.

## Empfohlene Nutzung
- Funktioniert am besten bei liquiden Forex-Paaren und Indizes, wo 30-Minuten- bis 4-Stunden-Kerzen ausgeprägte CCI-Extreme erzeugen.
- Schwellenwerte von ±210 recreieren die FT_CCI-Standardwerte. Niedrigere Werte machen das System reaktiver; höhere Werte fokussieren nur auf die extremsten Umkehrungen.
- Stellen Sie sicher, dass die Wertpapiermetadaten einen gültigen `PriceStep` aufweisen. Der Pip-Konverter stützt sich auf diesen Wert, um Pips in Preisoffsets umzurechnen.
- Die Strategie geht von einem Netting-Kontomodell aus (einzelne Netto-Position). Für Hedging-Konten setzen Sie das Handelsvolumen entsprechend, damit Umkehrungen den vorherigen Trade vollständig glätten.

## Hinweise
- Der Indikator muss vollständig gebildet sein, bevor ein Handelssignal berücksichtigt wird. Frühe Kerzen werden ignoriert, bis der CCI genügend Daten hat, um gültige Werte zu liefern.
- Stop-Loss- und Take-Profit-Orders sind optional. Sie auf null zu belassen, reproduziert das ursprüngliche Expert Advisor-Verhalten, das sich ausschließlich auf entgegengesetzte Signale für Ausstiege stützte.
- Fügen Sie die Strategie einem Chart in StockSharp hinzu, um Kerzen, den CCI-Indikator und ausgeführte Trades zu visualisieren; diese visuellen Hilfen werden automatisch in der C#-Implementierung aktiviert.
