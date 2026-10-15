# EMA (barabashkakvn Edition)-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem MetaTrader-5-Expertenberater "EMA (barabashkakvn's edition)". Das System handelt den Kreuzungspunkt zweier exponentieller gleitender Durchschnitte, die auf dem Median-Preis berechnet werden, und verwendet virtuelle Take-Profit/Stop-Loss-Level in Pips. Positionen werden nur nach einer bestätigten Kreuzung und einem kleinen Rücksetzer zum vorigen Kerzenextrem eröffnet.

## Kernidee

1. Verfolgung von 5- und 10-Perioden-EMAs (Median-Preis) im gewählten Zeitrahmen.
2. Wenn die schnelle EMA die langsame EMA kreuzt, ein ausstehiges Signal setzen statt sofort zu handeln.
3. Warten, bis der Preis `MoveBackPips` vom vorigen Kerzenextremum zurückgesetzt hat, während der EMA-Spread `2 * pipSize` übersteigt.
4. In Richtung der Kreuzung einsteigen, sobald der Rücksetzer eintritt.
5. Die offene Position mit virtuellen Zielen und Stops in Pips vom Einstandspreis verwalten.

Dieses Verhalten spiegelt die ursprüngliche MQL-Implementierung wider: Der Expertenberater wartete auf das Kreuzungsflag (`check`) und erforderte dann einen EMA-Spread plus einen Preisrücksetzer relativ zur vorherigen Kerze, um den Trade auszulösen. Die Ausstiegsregeln folgen ebenfalls dem „virtuellen" Ansatz, indem Positionen geschlossen werden, wenn Bid/Ask die angegebenen Abstände berührt hätte.

## Indikatoren & Daten

- 5-Perioden-EMA auf Median-Preis (high + low) / 2.
- 10-Perioden-EMA auf Median-Preis.
- Hoch/Tief der vorherigen fertigen Kerze für Rücksetzerprüfungen.
- Alle Verarbeitung verwendet fertige Kerzen aus dem konfigurierten `CandleType`-Abonnement.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Handelsvolumen in Lots/Kontrakten für jeden Einstieg. |
| `VirtualProfitPips` | `5` | Abstand (in Pips) zwischen Einstandspreis und virtuellem Take-Profit. |
| `MoveBackPips` | `3` | Nach der Kreuzung erforderlicher Rücksetzer, gemessen vom vorigen Kerzenextremum. |
| `StopLossPips` | `20` | Abstand (in Pips) zwischen Einstandspreis und virtuellem Stop-Loss. |
| `PipSize` | `0.0001` | Pip-Größe in Preiseinheiten. Überschreiben bei Handelssymbolen mit anderer Pip-Definition. |
| `FastLength` | `5` | Länge der schnellen EMA. |
| `SlowLength` | `10` | Länge der langsamen EMA. |
| `CandleType` | `TimeFrame(1m)` | Für Berechnungen verwendete Kerzenquelle. |

Alle pip-basierten Werte werden mit `pipValue = PipSize` in Preisabstände umgewandelt. Wenn der Parameter auf null oder eine negative Zahl gesetzt wird, fällt die Strategie auf `Security.PriceStep` zurück (wenn vom Board bereitgestellt).

## Handelslogik

### Einstiegsbedingungen

- **Signalanrüstung**: ein ausstehiges Signal speichern, wann immer eine Kreuzung auftritt (`FastEMA` kreuzt über `SlowEMA` oder umgekehrt). Es wird noch kein Trade platziert.
- **Short-Einstieg**: erfordert
  - Ausstehendes Signal vorhanden.
  - `SlowEMA - FastEMA > 2 * pipSize`.
  - Kerzenhoch aktuell ≥ Kerzentief vorherig + `MoveBackPips * pipSize` (Preis hat sich vom vorigen Tief nach oben zurückgesetzt).
- **Long-Einstieg**: erfordert
  - Ausstehendes Signal vorhanden.
  - `FastEMA - SlowEMA > 2 * pipSize`.
  - Kerzentief aktuell ≤ Kerzenhoch vorherig - `MoveBackPips * pipSize` (Preis hat sich vom vorigen Hoch nach unten zurückgesetzt).

Nach dem Öffnen einer Position setzt sich das ausstehige Flag zurück, um Doppeleinstiege zu vermeiden.

### Ausstiegsbedingungen

Virtuelle Ziele emulieren das MQL-Verhalten, indem Kerzenextreme mit den voreingestellten Abständen verglichen werden:

- **Long-Position**:
  - Schließen wenn Kerzenhoch ≥ Einstandspreis + `VirtualProfitPips * pipSize`.
  - Schließen wenn Kerzentief ≤ Einstandspreis - `StopLossPips * pipSize`.
- **Short-Position**:
  - Schließen wenn Kerzentief ≤ Einstandspreis - `VirtualProfitPips * pipSize`.
  - Schließen wenn Kerzenhoch ≥ Einstandspreis + `StopLossPips * pipSize`.

Nach jedem Ausstieg setzen sich die virtuellen Level zurück und die Strategie wartet auf die nächste Kreuzung.

## Implementierungshinweise

- Verwendet das High-Level-Kerzenabonnement (`SubscribeCandles`) und zeichnet EMAs plus Trades im optionalen Chartbereich.
- Median-Preis wird direkt aus dem Kerzenhoch/-tief berechnet, um `PRICE_MEDIAN` von MetaTrader abzugleichen.
- Das Kreuzungsflag (`_hasCrossSignal`) reproduziert die ursprüngliche `check`-Variable und stellt sicher, dass Trades nur nach Kreuzungs- und Rücksetzerprüfungen stattfinden.
- `StartProtection()` wird in `OnStarted` aufgerufen, um eingebaute Risikoüberwachung zu aktivieren, obwohl die Strategie Ausstiege manuell behandelt.
- Der Code behält alle Kommentare auf Englisch, wie angefordert, und stützt sich ausschließlich auf fertige Kerzen ohne direkten Zugriff auf Indikatorpuffer.

## Verwendungstipps

- `PipSize` anpassen bei Instrumenten mit nicht-standardmäßigen Pip-Definitionen (z.B. JPY-Paare, Indizes, Krypto-Kurse).
- Da Ausstiege auf Kerzenextremen basieren, hält die Verwendung kürzerer Zeitrahmen (1–5 Minuten) das Verhalten näher am ursprünglichen tick-basierten Expertenberater.
- Optimierung kann EMA-Längen, Pip-Abstände und Rücksetzer-Werte mit den bereitgestellten Parameter-Metadaten erkunden.
- Die Strategie handelt jeweils eine Position; externe Positionen auf demselben Instrument können das virtuelle Ausstiegs-Tracking beeinträchtigen.

## Risiken

- Kerzenbasierte Simulation kann Intrabar-Berührungen der virtuellen Level verpassen; bei Präzisionsbedarf höher aufgelöste Daten in Betracht ziehen.
- Virtuelle Ausstiege platzieren keine echten Schutzorders, daher können Verbindungsunterbrechungen oder Slippage zu größeren Verlusten als erwartet im Live-Handel führen.
- Wie bei jedem Kreuzungssystem verschlechtert sich die Performance in Seitwärtsmärkten; bei Bedarf Filter kombinieren.
