# BeerGod EMA-Timing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Experten BeerGodEA innerhalb von StockSharp. Sie handelt Mean-Reversion-Setups auf einem
einzelnen Symbol, indem sie einen exponentiellen gleitenden Durchschnitt (EMA) mit 60 Perioden überwacht und die aktuelle
Preisbewegung mit dem vorherigen Balken vergleicht. Signale werden nur einmal pro Balken bei einem konfigurierbaren Minuten-Offset
nach der Kerzeneröffnung ausgewertet, um den ursprünglichen EA nachzuahmen, der einige Minuten wartet, bevor er handelt.

Wenn der Preis vorübergehend vom EMA abweicht, während der Durchschnitt in die entgegengesetzte Richtung tendiert, eröffnet die
Strategie eine Marktposition in der Erwartung, dass sich die Bewegung umkehrt. Bestehende Positionen in die entgegengesetzte
Richtung werden sofort umgekehrt, indem die Ordergröße angepasst wird, sodass Shorts gedeckt werden, bevor eine neue Long-Position
aufgebaut wird (und umgekehrt).

## Funktionsweise

1. Abonnieren Sie Zeitrahmen-Kerzen (Standard 5 Minuten) und erstellen Sie einen 60-Perioden-EMA über die Schlusskurse.
2. Verfolgen Sie die aktuelle Kerze in Echtzeit. Beim ersten Tick jedes neuen Balkens speichern Sie den vorherigen EMA-Wert und
   den vorherigen Balken-Schluss, damit die Strategie sie später vergleichen kann.
3. Sobald die konfigurierte Anzahl von Minuten ab der Eröffnung verstrichen ist (Standard 3 Minuten), bewerten Sie die folgenden
   Bedingungen unter Verwendung des aktuellen Preises und der EMA-Steigung:
   - **Kauf-Setup**: aktueller Preis < aktueller EMA, EMA liegt unter seinem vorherigen Wert (fallend), und aktueller Preis <
     vorheriger Balken-Schluss.
   - **Verkauf-Setup**: aktueller Preis > aktueller EMA, EMA liegt über seinem vorherigen Wert (steigend), und aktueller Preis >
     vorheriger Balken-Schluss.
4. Wenn ein Kauf-Setup auftritt, während man noch nicht Long ist, senden Sie eine Market-Buy-Order, die so dimensioniert ist, dass
   offene Shorts geschlossen und das gewünschte Long-Volumen aufgebaut wird. Dieselbe Logik gilt symmetrisch für Verkauf-Setups.
5. Nachdem ein Trade ausgelöst wurde, gilt das Signal für diese Kerze als verarbeitet, um doppelte Einstiege zu verhindern.

## Parameter

- **Volume** – Ordergröße in Lots (Standard 1). Die Strategie addiert automatisch den Absolutwert der aktuellen Position, wenn
  sie die Richtung wechseln muss, sodass die neue Order die alte Exposure schließt und den neuen Trade in einer einzigen
  Transaktion eröffnet.
- **EMA Length** – Lookback-Periode für den exponentiellen gleitenden Durchschnitt (Standard 60).
- **Trigger Minutes** – Anzahl der Minuten nach der Balkeneröffnung, wenn die Eintrittsbedingungen geprüft werden (Standard 3).
  Wenn das Fenster verpasst wird, wartet die Strategie auf die nächste Kerze.
- **Candle Type** – Kerzendatentyp für Berechnungen (Standard 5-Minuten-Zeitrahmen).

## Handelshinweise

- Die Logik funktioniert auf jedem Symbol, solange Kerzendaten und Level-1-Preise verfügbar sind. Passen Sie die
  Kerzendauer an, wenn das Instrument in anderen Sessions als das ursprüngliche MetaTrader-Setup handelt.
- Es wird jeweils nur eine Position (Long oder Short) gehalten. Das Wechseln der Richtung erfolgt durch die Dimensionierung
  der neuen Marktorder, um die ausstehende Position zu decken und den neuen Trade in einem Schritt zu eröffnen.
- Im ursprünglichen EA sind keine expliziten Stop-Loss- oder Take-Profit-Niveaus definiert. Das Risikomanagement sollte extern
  hinzugefügt werden, falls erforderlich.
- Der Startschutz ist aktiviert, damit StockSharp automatisch Notfall-Positionsausstiege behandelt, wenn manuelle Eingriffe oder
  Verbindungsprobleme auftreten.
