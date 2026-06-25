# Strategie für den Ausbruch der vorherigen Kerze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Strategie für den Ausbruch der vorherigen Kerze** beobachtet das Hoch und Tief der zuletzt abgeschlossenen Kerze eines benutzerdefinierten Zeitrahmens (Standard: 4 Stunden). Wenn die aktuelle Kerze über diese Referenzniveaus hinaus um einen konfigurierbaren Einrückungsabstand vordringt, eröffnet die Strategie Ausbruchsgeschäfte. Ein optionaler gleitender Durchschnitts-Trendfilter hält Trades in Einklang mit der vorherrschenden Richtung, während geschichtete Ausstiegslogik (fester Stop-Loss, Take-Profit und pip-basierter Trailing Stop) das Risiko nach dem Einstieg verwaltet.

## Hauptmerkmale

- Verwendet eine höhere Zeitrahmen-Kerze als Ausbruchsanker. Alle Signale stammen vom Hoch oder Tief der letzten abgeschlossenen Referenzkerze.
- Unterstützt vier gleitende Durchschnittstypen (SMA, EMA, Smoothed, WMA) mit unabhängigen Versätzen für die schnellen und langsamen Linien. Wenn beide Perioden ungleich null sind, erfordert der Filter, dass der schnelle MA über/unter dem langsamen MA liegt, bevor Long/Short-Trades akzeptiert werden.
- Konvertiert pip-basierte Abstände (Einrückung, Stop-Loss, Take-Profit, Trailing Stop und Schritt) in Preiseinheiten unter Verwendung der Instrumenteinstellungen. Für Instrumente mit 3 oder 5 Dezimalstellen entspricht ein Pip 10 Preisschritten, was der ursprünglichen MQL-Logik entspricht.
- Ermöglicht die Positionsgröße entweder durch festes Volumen oder durch Riskierung eines Prozentsatzes des Kontokapitals relativ zum Stop-Loss-Abstand.
- Begrenzt die maximale Anzahl von Einstiegen pro Richtung und schließt optional alle offenen Positionen, sobald der schwebende Gewinn einen bestimmten Geldbetrag erreicht.
- Die Trailing-Stop-Logik emuliert den MQL5-Expertenberater: Nachdem der Preis über den Trailing-Abstand plus Schritt hinaus vorrückt, rückt das Stop-Niveau in diskreten Schritten vor.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `CandleType` | Zeitrahmen für die Referenzkerze der vorherigen Periode (Standard: 4 Stunden). |
| `IndentPips` | Pip-Abstand über dem Hoch oder unter dem Tief vor dem Auslösen von Einstiegen. |
| `FastPeriod` / `SlowPeriod` | Längen der gleitenden Durchschnitte. Auf 0 setzen, um den Trendfilter zu deaktivieren. |
| `FastShift` / `SlowShift` | Horizontaler Versatz (in Balken) für jeden gleitenden Durchschnitt vor dem Vergleich. |
| `MaType` | Berechnungsmethode des gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted). |
| `StopLossPips` | Pip-Abstand für den anfänglichen Schutz-Stop. Auf 0 setzen, um zu deaktivieren. |
| `TakeProfitPips` | Pip-Abstand für Take-Profit-Orders. Auf 0 setzen, um zu deaktivieren. |
| `TrailingStopPips` | Trailing-Stop-Abstand. Erfordert `TrailingStepPips` > 0. |
| `TrailingStepPips` | Mindest-Pip-Verbesserung, bevor der Trailing Stop aktualisiert wird. |
| `OrderVolume` | Festes Handelsvolumen. Bei 0 werden Positionen nach Risikoprozentsatz bemessen. |
| `RiskPercent` | Prozentsatz des Portfolio-Kapitals, der pro Trade riskiert wird, wenn `OrderVolume` 0 ist. Erfordert einen Stop-Loss ungleich null. |
| `MaxPositions` | Maximale Anzahl erlaubter Einstiege pro Richtung. |
| `ProfitClose` | Schließt alle offenen Positionen, wenn der schwebende Gewinn diesen Wert (Basiswährung) erreicht. |

## Handelslogik

1. Die zuletzt abgeschlossene Kerze des `CandleType` verfolgen und deren Hoch/Tief speichern.
2. Bei jeder Aktualisierung der aktuellen Kerze:
   - Den gleitenden Durchschnittsfilter anwenden, wenn aktiviert. Ohne ausreichende MA-Historie wartet die Strategie.
   - Ausbruchsniveaus berechnen: vorheriges Hoch + Einrückung und vorheriges Tief − Einrückung.
   - Wenn das aktuelle Kerzenhoch das obere Niveau überquert, eine Long-Position eröffnen (vorbehaltlich Filtern, maximaler Positionsanzahl und per-Kerzen-Einstiegssperre).
   - Wenn das aktuelle Kerzentief das untere Niveau überquert, eine Short-Position mit denselben Prüfungen eröffnen.
3. Nach dem Einstieg fügt die Strategie Stop-Loss- und Take-Profit-Niveaus hinzu (wenn konfiguriert) und hält sie im Speicher. Wenn der Preis eine der Grenzen berührt, wird die Position über eine Marktorder geschlossen.
4. Die Trailing-Stop-Aktivierung spiegelt den MQL-Expertenberater wider: Der Preis muss den Trailing-Abstand plus den Trailing-Schritt überschreiten, bevor der Stop bewegt wird. Nachfolgende Updates erfordern eine weitere vollständige `TrailingStepPips`-Verbesserung.
5. Der schwebende Gewinn wird bei jedem Tick aus dem durchschnittlichen Einstandspreis neu berechnet. Wenn er `ProfitClose` erreicht, wird die gesamte offene Exposition sofort liquidiert.
6. Für risikobewertungsbasierte Größenbestimmung konvertiert die Strategie den Pip-Stop-Abstand in Währung unter Verwendung des `PriceStep` und `StepPrice` des Instruments. Das resultierende Volumen respektiert `MaxPositions` für die Skalierung.

## Hinweise

- `TrailingStopPips` auf 0 setzen, um das Trailing zu deaktivieren. Wenn Sie das Trailing aktivieren, stellen Sie sicher, dass `TrailingStepPips` ebenfalls positiv ist; andernfalls werden keine Trailing-Updates durchgeführt.
- Die Strategie speichert Einstiegs-Zeitstempel pro Kerze, um mehrere Einstiege auf derselben Referenzkerze zu vermeiden, was dem ursprünglichen EA-Verhalten entspricht.
- Für Instrumente ohne `PriceStep`/`StepPrice`-Metadaten kann die risikobasierte Größenbestimmung nicht berechnet werden und Trades werden übersprungen, es sei denn, `OrderVolume` ist angegeben.
- Alle Kommentare im Code sind auf Englisch geschrieben, um den Projektrichtlinien zu entsprechen.

## Dateien

- `CS/PreviousCandleBreakdownStrategy.cs` – C#-Implementierung der Strategie.

Die Python-Übersetzung wird für diese Strategie nicht bereitgestellt.
