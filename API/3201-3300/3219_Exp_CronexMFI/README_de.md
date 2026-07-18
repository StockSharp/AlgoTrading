# Exp Cronex MFI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den Experten-Advisor **Exp_CronexMFI**. Er glättet den Money Flow Index (MFI) zweimal und handelt **gegen** den Crossover der resultierenden Linien. Der Port behält die ursprüngliche konträre Philosophie bei und macht jede Einstellung als StockSharp-Strategieparameter zugänglich.

## Funktionsweise
1. Abonnieren der ausgewählten Kerzenserie (der Standard ist ein 4-Stunden-Zeitrahmen).
2. Berechnen des Money Flow Index mit der konfigurierten Periode.
3. Anwenden der gewählten Glättungsmethode zweimal: Der erste Durchlauf erzeugt die schnelle Cronex-Linie, der zweite Durchlauf glättet die schnelle Linie erneut, um die langsame Linie aufzubauen.
4. Speichern historischer Paare von schnellen und langsamen Werten mit einer einstellbaren Verzögerung (`SignalShift`).
5. Wenn die schnelle Linie die langsame Linie **nach unten** kreuzt, werden Shorts geschlossen (wenn erlaubt) und eine Long-Position geöffnet/erweitert. Wenn die schnelle Linie **nach oben** kreuzt, werden Longs geschlossen und eine Short-Position geöffnet/erweitert.
6. Orders werden mit dem Strategie-`Volume` gesendet und können für Long- und Short-Seiten unabhängig deaktiviert werden.

Die Strategie wertet nur abgeschlossene Kerzen aus, um das Timing der MetaTrader-Implementierung zu entsprechen.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `MfiPeriod` | `int` | `25` | Länge des Money Flow Index. |
| `FastPeriod` | `int` | `14` | Periode der ersten Glättungsstufe (schnelle Cronex-Linie). |
| `SlowPeriod` | `int` | `25` | Periode der zweiten Glättungsstufe (langsame Cronex-Linie). |
| `SignalShift` | `int` | `1` | Anzahl abgeschlossener Kerzen zur Verzögerung der Signalverarbeitung, reproduziert das `SignalBar`-Verhalten aus MQL. |
| `Smoothing` | `SmoothingMethod` | `Simple` | Gleitender-Durchschnitt-Algorithmus für beide Glättungsstufen. |
| `EnableLongEntries` | `bool` | `true` | Aktiviert Market-Orders, die Long-Positionen öffnen oder hinzufügen. |
| `EnableShortEntries` | `bool` | `true` | Aktiviert Market-Orders, die Short-Positionen öffnen oder hinzufügen. |
| `EnableLongExits` | `bool` | `true` | Erlaubt Signalen, bestehende Long-Exposition zu schließen. |
| `EnableShortExits` | `bool` | `true` | Erlaubt Signalen, bestehende Short-Exposition zu schließen. |
| `CandleType` | `DataType` | `TimeFrame(4h)` | Kerzenserie für Indikatorberechnungen. |
| `Volume` | `decimal` | `1` | Ordergröße beim Öffnen neuer Positionen. |

## Glättungsoptionen
Der originale MQL-Indikator bietet mehrere proprietäre Glättungsmodi. Der StockSharp-Port ordnet sie integrierten gleitenden Durchschnitten zu:

| MQL-Konzept | `SmoothingMethod`-Wert | Hinweise |
| --- | --- | --- |
| SMA | `Simple` | Einfacher gleitender Durchschnitt. |
| EMA | `Exponential` | Exponentieller gleitender Durchschnitt. |
| SMMA | `Smoothed` | Geglätteter gleitender Durchschnitt (Wilder). |
| LWMA | `Weighted` | Linear gewichteter gleitender Durchschnitt. |
| JJMA / JurX / ParMA / T3 / VIDYA / AMA | `DoubleExponential`, `TripleExponential`, `Hull`, `ZeroLagExponential`, `ArnaudLegoux`, `KaufmanAdaptive` | Nächste Approximation für adaptives Glätten wählen. |

## Unterschiede zur MQL-Version
- Tick-/Real-Volumen-Auswahl aus MQL ist nicht verfügbar; StockSharp-Kerzen liefern aggregierte Volumendaten.
- Handelsverwaltung basiert ausschließlich auf Market-Orders. Der ursprüngliche Geldmanagement-Helfer, der die Ausführung bis zur nächsten Bar verzögerte, wird durch `SignalShift` emuliert.
- Stop-Loss- und Take-Profit-Platzierung muss extern konfiguriert werden (z. B. über Risikoregeln oder Schutzmodule).

## Verwendungshinweise
- Eine Kerzenserie wählen, die zur Liquidität des Instruments passt; das Standard-4-Stunden-Intervall spiegelt den Quell-EA wider.
- `SignalShift` anpassen, wenn ein Crossover mit zusätzlichen Kerzen bestätigt werden soll.
- Die Strategie mit Risikoverwaltungsregeln (z. B. `StartProtection`) kombinieren, um Verluste zu begrenzen.
