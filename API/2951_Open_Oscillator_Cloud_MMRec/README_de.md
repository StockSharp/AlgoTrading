# Open Oscillator Cloud MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Expertenberater **Exp_Open_Oscillator_Cloud_MMRec** auf die StockSharp High-Level-API. Das System handelt den Kreuzungspunkt des Open Oscillator Cloud-Indikators, der den aktuellen Eröffnungskurs mit den Eröffnungskursen der höchsten und niedrigsten Balken innerhalb eines gleitenden Fensters vergleicht und das Ergebnis mit einem konfigurierbaren gleitenden Durchschnitt glättet.

## Strategie-Logik

### Indikator-Konstruktion
- Ein Rückblickfenster (`Oscillator Period`, Standard 20 Balken) abgeschlossener Kerzen vom gewählten Zeitrahmen wird erstellt.
- Der Balken mit dem höchsten Hoch wird gefunden und sein Eröffnungskurs gespeichert; der Balken mit dem niedrigsten Tief wird gefunden und sein Eröffnungskurs gespeichert.
- Zwei Rohwerte für die aktuelle Kerze werden berechnet:
  - **Oberes Band** = aktuelle Eröffnung − Eröffnungskurs beim höchsten Hoch.
  - **Unteres Band** = Eröffnungskurs beim niedrigsten Tief − aktuelle Eröffnung.
- Beide Serien werden mit dem gewählten gleitenden Durchschnitt geglättet (`Smoothing Method`, `Smoothing Length`). Unterstützte Typen sind Einfache, Exponentielle, Geglättete und Gewichtete gleitende Durchschnitte.
- Die geglättete Historie wird gespeichert und das Signal um `Signal Bar` vollständig geschlossene Kerzen (Standard 1) verzögert, um die ursprüngliche EA-Logik nachzuahmen, die auf dem vorherigen Balken agiert.

### Einstiegskriterien
- **Long-Einstieg**: Das obere Band des vorherigen Balkens lag über dem unteren Band, und der letzte verzögerte Wert kreuzt nach unten (`upper ≤ lower`). Kann über `Enable Long Entries` deaktiviert werden.
- **Short-Einstieg**: Das obere Band des vorherigen Balkens lag unter dem unteren Band, und der letzte verzögerte Wert kreuzt nach oben (`upper ≥ lower`). Kann über `Enable Short Entries` deaktiviert werden.

### Ausstiegskriterien
- **Long-Ausstieg**: Das obere Band des vorherigen Balkens lag unter dem unteren Band, was ein bearishes Regime signalisiert. Gesteuert durch `Enable Long Exits`.
- **Short-Ausstieg**: Das obere Band des vorherigen Balkens lag über dem unteren Band, was ein bullishes Regime signalisiert. Gesteuert durch `Enable Short Exits`.
- **Risikomanagement**: Wenn `Stop Loss Points` oder `Take Profit Points` größer als null sind, schließt die Strategie die Position automatisch, sobald der Preis diese Abstände (gemessen in Instrument-Preisschritten) vom Einstieg erreicht.

### Order-Handling
- Es werden nur Marktorders verwendet. Vor dem Öffnen einer neuen Position wird die entgegengesetzte Seite geglättet, um mit dem Einzelpositions-Verhalten des MetaTrader-Roboters ausgerichtet zu bleiben.
- Der Parameter `Trade Volume` legt die Basis-Positionsgröße für jeden Einstieg fest.

## Parameter
- `Candle Type` – Zeitrahmen der für den Oszillator verwendeten Kerzen (Standard 1 Stunde).
- `Oscillator Period` – Anzahl der Kerzen im gleitenden Fenster (Standard 20).
- `Smoothing Method` – Auf die Eröffnungslücken angewandter gleitender Durchschnitt (Simple, Exponential, Smoothed, Weighted).
- `Smoothing Length` – Länge des glättenden gleitenden Durchschnitts (Standard 10).
- `Signal Bar` – Anzahl vollständig geschlossener Balken zur Verzögerung der Signalauswertung (Standard 1).
- `Enable Long Entries` / `Enable Short Entries` – Erlaubt oder blockiert das Öffnen von Trades in jede Richtung.
- `Enable Long Exits` / `Enable Short Exits` – Erlaubt oder blockiert automatische Ausstiege für die jeweilige Richtung.
- `Trade Volume` – Größe jeder Marktorder (Standard 1 Kontrakt/Lot).
- `Stop Loss Points` – Schützender Stop-Abstand in Preisschritten (0 deaktiviert den Stop, Standard 1000).
- `Take Profit Points` – Gewinnziel-Abstand in Preisschritten (0 deaktiviert das Ziel, Standard 2000).

## Implementierungshinweise
- Die Glättungsmethoden entsprechen den gängigsten Optionen des ursprünglichen EA. Exotische Modi wie JJMA, T3, VIDYA oder AMA werden nicht portiert, da StockSharp bereits reichhaltige Alternativen für Optimierung und Robustheit bietet.
- Signale werden nur bei `CandleStates.Finished`-Ereignissen ausgewertet, um nicht auf unvollständige Daten zu reagieren.
- Die Strategie hält eine interne Historie geglätteter Werte, anstatt Indikator-Puffer abzufragen, was dem empfohlenen High-Level-StockSharp-Workflow entspricht.
- Schutzlevels werden automatisch gelöscht, wenn die Position flach wird, um zu verhindern, dass veraltete Stops Trades wieder eröffnen.

## Standardverhalten
- Trendfolge in beide Richtungen mit verzögerter Bestätigung zur Rauschreduzierung.
- Verwendet festes Money-Management (konstantes `Trade Volume`) unter Einhaltung von Stop-Loss- und Take-Profit-Abständen ähnlich der MetaTrader-Version.
- Geeignet als Vorlage zum Experimentieren mit verschiedenen Glättungstypen oder zur Kombination des Oszillators mit zusätzlichen Filtern.
