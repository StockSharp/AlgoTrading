# SR Rate Indicator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein C#-Port des MetaTrader 5-Experten **Exp_SR-RateIndicator**. Sie reproduziert die ursprüngliche Handelslogik mit der High-Level-API von StockSharp und einer benutzerdefinierten Implementierung des SR Rate Oszillators. Der Indikator misst, wie weit der gewichtete Kerzenpris innerhalb eines geglätteten Unterstützungs-/Widerstands-Kanals liegt, und malt einen Farbcode, der extreme Lesungen hervorhebt.

Der Algorithmus verarbeitet abgeschlossene Kerzen aus einem konfigurierbaren Zeitrahmen. Wenn die Oszillatorfarbe zum bullischen oder bearischen Extrem springt, schließt die Strategie jede entgegengesetzte Position und eröffnet einen neuen Trade in Signalrichtung. Schutzende Stop-Loss- und Take-Profit-Niveaus werden mit denselben Punktabständen angewendet, die in der MetaTrader-Version verwendet werden.

## SR Rate Oszillator

Der Indikator berechnet ein Gaußsches geglättetes Band um den Preis unter Verwendung einer konfigurierbaren Fensterlänge:

1. Für jede Kerze werden das Hoch, Tief und der gewichtete Schlusskurs mit einseitigen Gaußschen Gewichten der Länge sechs geglättet.
2. Das höchste geglättete Hoch und das niedrigste geglättete Tief über dem Fenster definieren einen dynamischen Bereich.
3. Der aktuelle geglättete gewichtete Schlusskurs wird innerhalb dieses Bereichs normiert und auf das Intervall `[-100, 100]` abgebildet.
4. Der finale Oszillatorwert wird in fünf Farbzustände konvertiert: `0` (stark bearisch), `1` (leicht bearisch), `2` (neutral), `3` (leicht bullisch) und `4` (stark bullisch).

Eine stark bullische Farbe (`4`) zeigt an, dass der Preis das obere Extrem des Bereichs erreicht hat, während eine stark bearische Farbe (`0`) einen Besuch am unteren Extrem signalisiert.

## Handelsregeln

1. Kerzen des konfigurierten Typs abonnieren und den SR Rate Oszillator auf jeder abgeschlossenen Kerze berechnen.
2. Die Signalauswertung um `SignalBar` geschlossene Kerzen verschieben (Standard: eine Kerze zurück), um das Expert-Advisor-Verhalten nachzuahmen.
3. Wenn die verschobene Farbe `4` wird und die vorherige Farbe unter `4` liegt:
   - Jede bestehende Short-Position schließen, wenn Long-Exits aktiviert sind.
   - Eine neue Long-Position eröffnen, wenn Long-Einträge aktiviert sind und keine andere Position aktiv ist.
4. Wenn die verschobene Farbe `0` wird und die vorherige Farbe über `0` liegt:
   - Jede bestehende Long-Position schließen, wenn Short-Exits aktiviert sind.
   - Eine neue Short-Position eröffnen, wenn Short-Einträge aktiviert sind und keine andere Position aktiv ist.
5. Nur eine Position kann jederzeit geöffnet sein. Neue Signale werden ignoriert, bis der vorherige Trade geschlossen ist.
6. Optionale Stop-Loss- und Take-Profit-Niveaus werden in Preispunkten ausgedrückt und automatisch unter Verwendung des Instrument-Kursschritts in absolute Preise umgerechnet.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `OrderVolume` | Trade-Volumen für jede Marktorder. |
| `EnableLongEntries` | Öffnen von Long-Positionen aktivieren/deaktivieren. |
| `EnableShortEntries` | Öffnen von Short-Positionen aktivieren/deaktivieren. |
| `EnableLongExits` | Long-Positionen schließen wenn eine stark bearische Farbe erscheint. |
| `EnableShortExits` | Short-Positionen schließen wenn eine stark bullische Farbe erscheint. |
| `StopLossPoints` | Stop-Loss-Abstand in Instrument-Punkten (konvertiert mit dem Kursschritt). |
| `TakeProfitPoints` | Take-Profit-Abstand in Instrument-Punkten (konvertiert mit dem Kursschritt). |
| `SlippagePoints` | Maximal tolerierter Slippage beim Schließen von Positionen. Für Kompatibilität beibehalten; kein explizites Slippage-Control wird von der High-Level-API angewendet. |
| `CandleType` | Kerzentyp und Zeitrahmen zur Berechnung des Indikators. |
| `SignalBar` | Anzahl der übersprungenen geschlossenen Kerzen vor der Histogrammanalyse (Standard 1). |
| `WindowSize` | Länge des rollenden Fensters für die SR Rate Normierung. |
| `HighLevel` | Oszillatorlevel der das bullische Extrem definiert (Standard +20). |
| `LowLevel` | Oszillatorlevel der das bearische Extrem definiert (Standard -20). |

## Hinweise

- Die Strategie funktioniert mit jedem Instrument, das Standard-OHLC-Kerzen liefert.
- Signale werden nur auf abgeschlossenen Kerzen verarbeitet; Intrabar-Neuberechnungen werden ignoriert, genauso wie in der MetaTrader-Implementierung.
- Die Slippage-Behandlung im ursprünglichen Experten hing von Ausführungseinstellungen ab. StockSharp-Marktorders respektieren bereits Exchange-Regeln, daher wird der `SlippagePoints`-Parameter nur für Dokumentationszwecke beibehalten.
- Der Indikator speichert nur die minimale Menge an Verlauf, die zur Auswertung des Fensters benötigt wird, um unnötigen Speicherverbrauch zu vermeiden.
- Die Python-Version wird gemäß den Projektrichtlinien absichtlich weggelassen.
