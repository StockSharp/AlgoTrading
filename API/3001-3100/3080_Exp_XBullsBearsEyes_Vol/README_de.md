# Exp XBullsBearsEyes Vol Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader-Experts **Exp_XBullsBearsEyes_Vol**. Der ursprüngliche Advisor kombiniert Bulls Power und Bears Power Werte, multipliziert das Ergebnis mit dem Kerzenvolumen und färbt das Histogramm entsprechend des resultierenden Momentums. Zwei unabhängige Positionsslots werden sowohl für die Long- als auch für die Short-Seite geführt, was dem System ermöglicht, zu skalieren, wenn die Farbintensität zunimmt. Der StockSharp-Port recreiert den mehrstufigen Filter, die Farblogik und das Trade-Management, während er High-Level-API-Aufrufe für Orders und Risikokontrolle verwendet.

Der Algorithmus abonniert einen konfigurierbaren Zeitrahmen, baut den benutzerdefinierten XBullsBearsEyes-Indikator neu auf und reagiert nur auf abgeschlossene Kerzen. Farbübergänge bestimmen sowohl die Einstiege als auch die Ausstiege: Bullische Farben schließen Short-Trades und können einen oder zwei Long-Slots öffnen; Bärische Farben führen die Spiegelaktion durch. Stop-Loss- und Take-Profit-Abstände werden in `StartProtection`-Parameter übersetzt, damit Platform-Risikomanager Schutzorders verwalten können.

## Indikatorlogik
1. Bulls Power und Bears Power Werte werden mit einer EMA der Periode `IndicatorPeriod` neu aufgebaut, indem das Kerzenhoch/-tief gegen den geglätteten Schlusskurs verglichen wird.
2. Ein vierstufiger adaptiver Filter akkumuliert bullischen (`CU`) und bärischen (`CD`) Druck mit dem Koeffizienten `Gamma`. Der Indikatorwert ist `CU / (CU + CD) * 100 - 50`.
3. Der gefilterte Wert wird je nach `VolumeType` entweder mit Tick-Volumen oder realem Volumen multipliziert.
4. Die multiplizierte Serie und das Rohvolumen werden beide durch einen gleitenden Durchschnitt geglättet, der durch `SmoothingMethod`, `SmoothingLength` und `SmoothingPhase` gewählt wird (die Jurik-Phase wird berücksichtigt, wenn die zugrunde liegende Klasse sie verfügbar macht).
5. Farbniveaus werden aus `HighLevel1`, `HighLevel2`, `LowLevel1` und `LowLevel2` abgeleitet. Werte über den oberen Bändern produzieren Farben `0` oder `1`, Werte unter den unteren Bändern produzieren Farben `3` oder `4`. Farbe `2` zeigt einen neutralen Zustand an.
6. Der Farbverlauf wird gespeichert, damit Signale auf der Kerze `SignalBar` ausgewertet werden können (Standard: eine geschlossene Kerze zurück). Die Farbe der aktuellen Signalkerze wird mit der vorherigen Farbe verglichen, um Übergänge zu erkennen.

## Handelsregeln
- Farben `1` und `0` kennzeichnen bullischen Druck. Wenn sich die Farbe in einen dieser Werte ändert und die vorherige Farbe schwächer war, öffnet Slot 1 (`PrimaryVolume`) bzw. Slot 2 (`SecondaryVolume`) eine Long-Position. Beide Ereignisse schließen bestehende Short-Exposure, wenn `AllowShortExit` aktiviert ist.
- Farben `3` und `4` kennzeichnen bärischen Druck. Wenn die Farbe sich in diese Werte bewegt und die vorherige Farbe stärker war, öffnet Slot 1 oder Slot 2 jeweils eine Short-Position. Beide Ereignisse schließen bestehende Long-Exposure, wenn `AllowLongExit` aktiviert ist.
- Jeder Slot merkt sich, ob er bereits eine offene Position hat, und ignoriert wiederholte Signale, bis die entsprechende Richtung geschlossen wurde.
- `SignalBar` definiert, wie viele abgeschlossene Kerzen vor der Farbauswertung übersprungen werden (0 = letzte abgeschlossene Kerze). Der Code benötigt mindestens zwei historische Farben zum Vergleich.
- Stop-Loss und Take-Profit in Punkten (`StopLossPoints`, `TakeProfitPoints`) werden mit `Security.PriceStep` in absolute Preisabstände umgerechnet und zum Starten des Plattformschutzes mit Marktausstiegen verwendet.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `PrimaryVolume` | Volumen für den ersten Slot (ausgelöst durch Farbe 1 / 3). |
| `SecondaryVolume` | Volumen für den zweiten Slot (ausgelöst durch Farbe 0 / 4). |
| `StopLossPoints` / `TakeProfitPoints` | Schutzabstände in Preisschritten. Auf null setzen zum Deaktivieren. |
| `AllowLongEntry` / `AllowShortEntry` | Skalierung in die entsprechende Richtung aktivieren. |
| `AllowLongExit` / `AllowShortExit` | Automatische Ausstiege aktivieren, wenn die entgegengesetzte Farbe erscheint. |
| `CandleType` | Für Kerzen und Indikatorberechnung abonnierter Zeitrahmen (Standard: 8 Stunden). |
| `IndicatorPeriod` | EMA-Periode zum Neuaufbau von Bulls/Bears Power. |
| `Gamma` | Adaptiver Glättungsfaktor für den vierstufigen Filter (0.0 – 0.999). |
| `VolumeType` | Tick-Volumen oder reales Volumen zur Gewichtung auswählen. |
| `HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2` | Niveaumultiplikatoren, die Farbschwellen definieren. |
| `SmoothingMethod` | Gleitender Durchschnittstyp zur Glättung des Indikators und des Volumens (SMA, EMA, SMMA, LWMA, Jurik, JurX, ParMA→EMA, T3, VIDYA→EMA, AMA). |
| `SmoothingLength` | Länge des glättenden gleitenden Durchschnitts. |
| `SmoothingPhase` | Jurik-Phaseparameter (begrenzt auf [-100, 100]). |
| `SignalBar` | Anzahl geschlossener Kerzen, die vor der Auswertung von Farbübergängen übersprungen werden. |

## Verwendungshinweise
- Die Strategie operiert mit einem einzelnen Instrument, das von `GetWorkingSecurities()` zurückgegeben wird, und verwendet Marktorders für Ein- und Ausstiege.
- Slot-Management ist nettiert: Zusätzliche Einstiege erhöhen die Nettoposition, während Ausstiege die gesamte Exposure für die betroffene Seite glätten.
- Wenn die Plattform nur Tick-Volumen bereitstellt, fällt `VolumeType = Real` auf die verfügbare Tick-Anzahl zurück.
- VIDYA- und Parabolische Glättung fallen auf exponentielle gleitende Durchschnitte zurück, weil StockSharp diese Implementierungen direkt bereitstellt.
- Den Instrumentenpreisschritt korrekt konfigurieren, damit `StopLossPoints` und `TakeProfitPoints` in die beabsichtigten absoluten Abstände konvertiert werden.
