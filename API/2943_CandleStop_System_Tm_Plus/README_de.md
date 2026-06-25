# CandleStop System Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie, die auf dem benutzerdefinierten Kanalindikator CandleStop aufgebaut ist. Das System berechnet kontinuierlich verzögerte Highest-High- und Lowest-Low-Bänder, wartet auf eine abgeschlossene Kerze, die jenseits dieser Bänder schließt, und reagiert dann auf dem folgenden Balken. Es erzwingt optional eine maximale Positionslebensdauer und verwendet punktbasierte Schutzstops.

## Details
- **Einstiegskriterien**: Die vorherige abgeschlossene Kerze schließt über dem verzögerten oberen Kanal (für Longs) oder unter dem verzögerten unteren Kanal (für Shorts), während der aktuelle Balken wieder innerhalb des Kanals bleibt, um Doppelauslöser zu vermeiden.
- **Long/Short**: Symmetrische Logik für Long- und Short-Trades mit unabhängigen Aktivierungs-Flags.
- **Ausstiegskriterien**: Gegenläufige CandleStop-Ausbrüche schließen bestehende Positionen; ein optionaler zeitbasierter Ausstieg schließt Trades, die über die konfigurierte Minutenanzahl hinaus offen bleiben.
- **Stops**: Verwendet börsenschrittbasierte Stop-Loss- und Take-Profit-Level über `StartProtection`.
- **Standardwerte**:
  - `OrderVolume` = 1
  - `UpTrailPeriods` = 5, `UpTrailShift` = 5
  - `DownTrailPeriods` = 5, `DownTrailShift` = 5
  - `SignalBar` = 1
  - `StopLossPoints` = 1000, `TakeProfitPoints` = 2000
  - `MaxPositionMinutes` = 1920
  - `CandleType` = 8-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Verzögerte CandleStop-Kanäle
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mehrstündig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Parameter
- `OrderVolume`: Menge für jeden Markteinstieg, wenn eine neue Position geöffnet wird.
- `EnableLongEntry` / `EnableShortEntry`: Schalter, die das unabhängige Deaktivieren neuer Longs oder Shorts ermöglichen.
- `CloseLongOnBearishBreak` / `CloseShortOnBullishBreak`: Ob bestehende Positionen geschlossen werden sollen, wenn die entgegengesetzte CandleStop-Ausbruchsfarbe erscheint.
- `EnableTimeExit`: Aktiviert den Filter für die maximale Haltezeit.
- `MaxPositionMinutes`: Anzahl der Minuten, bevor ein offener Trade zwangsweise geschlossen wird; auf null setzen, um auch bei aktiviertem `EnableTimeExit` zu deaktivieren.
- `UpTrailPeriods` und `UpTrailShift`: Lookback-Länge und Rückverschiebung für den bullischen CandleStop-Kanal. Die Verschiebung verzögert das Donchian-Band um mehrere Balken, um das ursprüngliche Indikator-Timing nachzuahmen.
- `DownTrailPeriods` und `DownTrailShift`: Entsprechende Parameter für den bärischen Kanal.
- `SignalBar`: Index des Balkens, der auf Ausbruchsfarbe geprüft wird (1 = vorherige abgeschlossene Kerze). Der nächst ältere Balken wird zur Bestätigung verwendet, wie in der MQL-Version.
- `StopLossPoints` / `TakeProfitPoints`: Schutzstop-Abstände in Preisschritten. An `StartProtection` übergeben, um Ausstiege automatisch zu verwalten.
- `CandleType`: Primäre Kerzenreihe für die Strategie. Standardmäßig 8 Stunden, um dem Quellskript zu entsprechen.

## Implementierungshinweise
- Die Kanalwerte werden mit `Highest`- und `Lowest`-Indikatoren kombiniert mit `Shift` berechnet, um die verzögerten Bänder des ursprünglichen CandleStop-Indikators zu reproduzieren.
- Signalfarben werden in einem Rolling-Buffer gespeichert, um die `CopyBuffer`-Aufrufe der MQL-Strategie nachzuahmen und doppelte Einstiege auf aufeinanderfolgenden Kerzen zu vermeiden.
- Vor der Orderplatzierung prüft die Strategie auf zeitbasierte Ausstiege, schließt gegenläufige Positionen falls erforderlich und gibt dann neue Marktorders mit dem konfigurierten Volumen aus.
