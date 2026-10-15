# Exp DEMA Kanalbereich Tm Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Exp DEMA Range Channel Tm Plus Strategie portiert den ursprünglichen MetaTrader-Expertenberater in die High-Level-API von StockSharp. Sie baut einen doppelten exponentiellen gleitenden Durchschnitt (DEMA)-Kanal um Preisextreme auf und interpretiert die vom Kanal erzeugten Kerzenfarben, um zu entscheiden, wann gehandelt werden soll. Die Implementierung hält die Geldverwaltungslogik einfach und verlässt sich auf die Plattform-Eigenschaft `Volume` und optionale Schutzorders, während die Ausbruchs- und Zeitlimitregeln des Quellcodes reproduziert werden.

## Kernlogik

- **Kanalaufbau**
  - Zwei DEMA-Indikatoren mit gleicher Periode werden berechnet: einer auf Kerzenhochs und einer auf Kerzenchiefs.
  - Ihre Ausgaben werden um eine konfigurierbare Anzahl von Balken (`Shift`) nach vorne verschoben, um zu entsprechen, wie der ursprüngliche benutzerdefinierte Indikator den Kanal zeichnet.
  - Ein Preisoffset in Punkten (`PriceShiftPoints`) kann hinzugefügt werden, um den Kanal zu erweitern oder zu verengen.
- **Signalfarben**
  - Eine Kerze, die über dem verschobenen oberen Band schließt, gilt als bullisch.
  - Eine Kerze, die unter dem verschobenen unteren Band schließt, gilt als bärisch.
  - Die Richtung des Kerzenkörpers (Schluss ≥ Eröffnung oder Schluss ≤ Eröffnung) wird beibehalten, um die vier möglichen Farben (0–3) des MQL-Indikators nachzuahmen.
- **Einstiegsbedingungen**
  - Die Strategie schaut `SignalBar` Balken zurück, um die letzte Ausbruchsfarbe zu bewerten, und bestätigt, dass der vorherige Balken nicht bereits dasselbe Signal zeigte. Dies erfasst den Moment, in dem ein neuer Ausbruch erscheint.
  - Long-Einstiege sind nur erlaubt, wenn `EnableBuyEntry` true ist und die erkannte Farbe einem Aufwärtsausbruch entspricht.
  - Short-Einstiege erfordern `EnableSellEntry` und einen Abwärtsausbruch.
- **Ausstiegsbedingungen**
  - Long-Positionen können bei jedem Abwärtsausbruch geschlossen werden, wenn `EnableBuyExit` aktiviert ist.
  - Short-Positionen können bei Aufwärtsausbrüchen geschlossen werden, wenn `EnableSellExit` aktiviert ist.
  - Positionen können auch nach einer konfigurierbaren Haltezeit (`HoldingMinutes`) geschlossen werden, wenn `UseHoldingLimit` true ist, und spiegeln den Zeitfilter des Expertenberaters wider.
- **Risikokontrolle**
  - Optionale Take-Profit- und Stop-Loss-Abstände (in Preispunkten) aktivieren `StartProtection`, das Schutzorders mit Marktausführung ausgibt, wenn die Schwellenwerte erreicht werden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `MaPeriod` | DEMA-Periode für obere und untere Kanallinie. |
| `Shift` | Anzahl der Balken, um die DEMA-Linien vor Vergleichen nach vorne verschoben werden. |
| `PriceShiftPoints` | Zusätzlicher Abstand in Preispunkten (Vielfache von `PriceStep`), der zur oberen Linie addiert und von der unteren Linie subtrahiert wird. |
| `SignalBar` | Anzahl der Balken zurück zur Bewertung der Ausbruchsfarbe. `0` bedeutet aktueller Balken, `1` der letzte geschlossene Balken usw. |
| `EnableBuyEntry` / `EnableSellEntry` | Schalter für Long- und Short-Ausbruchseinstiege. |
| `EnableBuyExit` / `EnableSellExit` | Schalter zum Schließen von Long- oder Short-Positionen bei entgegengesetzten Signalen. |
| `UseHoldingLimit` | Aktiviert das Schließen von Positionen nach `HoldingMinutes` Minuten im Markt. |
| `HoldingMinutes` | Maximale Haltezeit vor einem Zwangsschluss; auf `0` setzen, um bei aktiviertem Flag zu deaktivieren. |
| `StopLossPoints` / `TakeProfitPoints` | Schutzabstände in Preispunkten. Wenn größer als null werden sie in absolute Preisoffsets konvertiert und an `StartProtection` übergeben. |
| `CandleType` | Kerzentyp und Zeitrahmen für alle Berechnungen (Standard 8-Stunden-Kerzen wie im MQL-Skript). |

## Handels-Workflow

1. Kerzen nach `CandleType` abonnieren und DEMA-Indikatoren starten.
2. Die aktuellsten Kanalwerte in Warteschlangen speichern, damit der Algorithmus den Wert referenzieren kann, der `Shift` Balken früher existierte, und so die ursprüngliche Indikatorverschiebung reproduziert.
3. Wenn eine Kerze abgeschlossen wird, ihre Ausbruchsfarbe berechnen und in einen Rolling-Buffer schieben. Den Buffer nutzen, um neue Auf- oder Abwärtsausbrüche gemäß `SignalBar` zu identifizieren.
4. Bestehende Positionen schließen, wenn das entgegengesetzte Signal erscheint oder der Zeitfilter abläuft.
5. Neue Trades durch Marktorders der Größe `Volume + |Position|` eröffnen, um von der entgegengesetzten Seite bei Bedarf umzukehren.
6. Den internen Zeitstempel der aktiven Position aktualisieren, um den Haltezeitfilter akkurat zu halten.

## Hinweise

- Die Strategie setzt voraus, dass Chartdaten in chronologischer Reihenfolge verarbeitet werden. Bei Backtests oder Live-Trading sicherstellen, dass der Kerzenstrom geordnet ist, um das korrekte Verschiebungsverhalten zu erhalten.
- `Volume` muss vor dem Start auf der Strategie gesetzt werden (via UI oder Code), um die Positionsgröße zu kontrollieren. Geldverwaltungsmodi aus dem MQL-Experten werden absichtlich nicht repliziert.
- Da Schutzorders optional sind, daran denken, sowohl Stop-Loss- als auch Take-Profit-Werte zu konfigurieren, wenn in Produktionsumgebungen eingesetzt.
- Der Chart-Helfer zeichnet Kerzen und ausgeführte Trades automatisch, was eine visuelle Überprüfung ermöglicht, dass Kanalausbrüche die erwarteten Ein- und Ausstiege auslösen.
