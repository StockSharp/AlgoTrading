# Exp AFIRMA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Exp AFIRMA Strategie** reproduziert den MetaTrader Expert Advisor `Exp_AFIRMA.mq5` mit StockSharp's High-Level-API.
Das Originalsystem basiert auf dem AFIRMA-Indikator (Adaptive Finite Impulse Response Moving Average), der einen gefensterten
FIR-Glätter mit einer kurzen ARMA-Prognose kombiniert. Die StockSharp-Version behält die gleiche Marktlogik bei: öffnet
Long-Positionen, wenn die ARMA-Komponente nach oben dreht, und schließt oder kehrt um, wenn die Prognose auf die bearische
Seite fällt.

Handelsentscheidungen werden auf abgeschlossenen Kerzen eines konfigurierbaren Zeitrahmens getroffen (Standard: H4). Die
Strategie wertet ARMA-Werte mehrerer geschlossener Bars aus, um eine Steigungsänderung zu bestätigen. Aufträge werden zum
Markt platziert mit optionalen Schutz-Stops und -Zielen, die durch StockSharp's Risikomanagement implementiert werden.

## Handelslogik

1. **Indikatorberechnung**
   - Der eingebettete `AfirmaIndicator` recreiert den zweistufigen AFIRMA-Filter. Ein gefensterter FIR-Glätter
     (Länge = `Taps`, Bandbreite = `Periods`) erzeugt einen Basis-gleitenden Durchschnitt.
   - Die ARMA-Prognose wird über dieselben Least-Squares-Koeffizienten wie im MQL-Quellcode berechnet. Der Indikator
     exponiert FIR- und ARMA-Werte; die Strategie konsumiert nur die ARMA-Komponente.
2. **Signalauswertung**
   - Bei jeder abgeschlossenen Kerze wird der neueste ARMA-Wert gespeichert. Der Parameter `SignalBar` (Standard: 1)
     gibt an, wie viele bereits geschlossene Bars übersprungen werden sollen.
   - **Bullisches Setup**: der vorherige ARMA-Wert ist kleiner als sein Vorgänger (`ARMA[t-2] < ARMA[t-3]`) und der
     neueste Wert liegt über dem vorherigen (`ARMA[t-1] > ARMA[t-2]`). Dies schließt Short-Exposition und öffnet/erweitert
     eine Long-Position, wenn erlaubt.
   - **Bearisches Setup**: der vorherige ARMA-Wert ist größer als sein Vorgänger, während der neueste Wert darunter liegt.
     Dies schließt Long-Exposition und öffnet/erweitert eine Short-Position, wenn erlaubt.
3. **Positionsverwaltung**
   - Es wird nur eine Position gehalten. Neue Einträge bewegen die Position Richtung `±TradeVolume`. Bestehende Exposition
     wird vor dem Umkehren geschlossen.
   - Optionaler Risikoschutz verwendet `StartProtection` mit preisbasierten Stop-Loss- und Take-Profit-Abständen.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `TradeVolume` | Basis-Positionsgröße für Long- und Short-Einträge. |
| `CandleType` | Zeitrahmen/Datentyp, der vom Marktdaten-Adapter angefordert wird (Standard: 4-Stunden-Kerzen). |
| `Periods` | Reziproke Bandbreite der FIR-Stufe (`1 / (2 * Periods)`), identisch zur Eingabe des Original-EA. |
| `Taps` | Anzahl der FIR-Koeffizienten. Intern auf den nächsten ungeraden Wert angepasst, wenn nötig. |
| `Window` | Fensterfunktion für den FIR-Filter (`Rectangular`, `Hanning1`, `Hanning2`, `Blackman`, `BlackmanHarris`). |
| `SignalBar` | Anzahl bereits geschlossener Bars für die Bestätigungsrückschau. `1` entspricht der zuletzt vollständig geschlossenen Bar. |
| `EnableBuyEntries` / `EnableSellEntries` | Long/Short-Positionseröffnungen erlauben. |
| `EnableBuyExits` / `EnableSellExits` | Long/Short-Positionsschließungen bei entgegengesetzten Signalen erlauben. |
| `StopLossPoints` | Optionaler Schutz-Stop, ausgedrückt in Preiseinheiten. |
| `TakeProfitPoints` | Optionales Schutzziel, ausgedrückt in Preiseinheiten. |

## Konvertierungshinweise

- Die Geldverwaltungsoptionen (`MM`, `MMMode`, `Deviation_`) der MetaTrader-Version werden durch den einfacheren
  `TradeVolume`-Parameter ersetzt.
- Der Original-EA sendet Stop-Loss- und Take-Profit-Werte in Punkten. Hier werden sie in absoluten Preiseinheiten
  angegeben. Punkte in Preis umrechnen durch Multiplikation mit dem entsprechenden Preisschritt.
- Wenn `SignalBar = 1`, liest die Strategie die letzten drei **abgeschlossenen** ARMA-Werte und öffnet Aufträge auf der
  nächsten Bar. `SignalBar = 0` funktioniert auch, verwendet aber die zuletzt geschlossene Bar.
- Die AFIRMA-Indikatorimplementierung entspricht der Originalmathematik, einschließlich der unterstützten Fenstertypen
  und Koeffizientenformeln.

## Verwendungstipps

1. Strategie mit einem Instrument und Portfolio verbinden, `TradeVolume` konfigurieren und den Zeitrahmen über `CandleType`
   auswählen.
2. Long/Short-Richtungen entsprechend dem Handelsplan aktivieren oder deaktivieren.
3. `StopLossPoints` und `TakeProfitPoints` setzen, wenn automatisiertes Risikomanagement gewünscht; sonst auf null
   lassen für Handel ohne feste Ausstiege.
4. Das generierte Diagramm überwachen, um AFIRMA-Linien und ausgeführte Trades beim Anpassen von `Periods`, `Taps`
   und `SignalBar` zu überprüfen.
