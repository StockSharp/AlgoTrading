# MACD Die PSAR-Strategie wurde korrigiert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Portierung des MetaTrader Expert Advisors **EA_MACD_FixedPSAR**. Es handelt Trendumkehrungen durch die Kombination eines MACD-Crossover-Filters mit einer EMA-Trendprüfung. Das Risikomanagement spiegelt die ursprüngliche Implementierung wider und unterstützt sowohl einen Trailing-Stop mit fester Distanz als auch einen Trailing-Modus im Parabolic SAR-Stil. Alle Abstände werden in Pips konfiguriert und intern basierend auf der Tick-Größe des Instruments in Preiseinheiten umgerechnet.

## Indikatoren
- `MovingAverageConvergenceDivergenceSignal` (12, 26, 9) liefert MACD und Signalleitungen.
- `ExponentialMovingAverage` (Standard 26) bestätigt die kurzfristige Trendrichtung.

## Handelslogik
1. **Eintrittsbedingungen**
   - **Long**: MACD überschreitet seine Signallinie, bleibt aber unter Null, der absolute MACD-Wert überschreitet den *MACD Open Level* und der EMA steigt im Vergleich zur vorherigen Kerze.
   - **Short**: MACD unterschreitet seine Signallinie, bleibt aber über Null, der absolute MACD-Wert überschreitet den *MACD Open Level* und der EMA fällt im Vergleich zur vorherigen Kerze.
2. **Exit Conditions**
   - MACD-Umkehr, die den *MACD-Schlusskurs* in die entgegengesetzte Richtung überschreitet.
   - Konfigurierbare Take-Profit- und Stop-Loss-Level, beide gemessen in Pips.
   - Optionales Trailing-Stop-Verhalten:
     - **Behoben**: Behält einen konstanten Abstand zum letzten Schlusskurs bei.
     - **Behoben PSAR**: emuliert die inkrementelle Parabolic SAR-Anpassung, die von der MQL-Version verwendet wird.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `Volume` | Handelsvolumen, das für Marktaufträge verwendet wird. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. |
| `TrailMode` | Trailing-Stop-Logik (`None`, `Fixed`, `FixedPsar`). |
| `TrailingStopPips` | Distanz für den festen Nachlaufmodus. |
| `PsarStep` | Anfänglicher Beschleunigungsfaktor für den Schleppmodus PSAR. |
| `PsarMaximum` | Maximaler Beschleunigungsfaktor für den Schleppmodus PSAR. |
| `MacdOpenLevelPips` | Mindestgröße MACD (in Pips), die zum Öffnen einer Position erforderlich ist. |
| `MacdCloseLevelPips` | Mindestgröße MACD (in Pips), die zum Schließen einer Position erforderlich ist. |
| `TrendPeriod` | EMA Zeitraum, der für die Trendbestätigung verwendet wird. |
| `CandleType` | Kerzenserientyp für Indikatorberechnungen. |

## Notizen
- Alle Schwellenwerte werden in Pips gespeichert und mithilfe der Tick-Größe des Instruments in Preiseinheiten übersetzt (mit fünf oder drei Dezimalstellen, die die MetaTrader-Anpassung emulieren).
- Die Trailing-Stop-Logik wird nur bei vollständig geformten Kerzen aktualisiert, um vorzeitige Ausstiege zu vermeiden.
- Die Strategie zeichnet Kerzen, beide Indikatoren und Handelsmarken im Standard-Chartbereich, sofern verfügbar.
