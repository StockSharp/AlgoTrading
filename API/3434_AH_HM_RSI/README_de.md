# AH HM RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Experten **Expert_AH_HM_RSI**. Es sucht nach Hammer- oder Hängemann-Kerzenmustern und erfordert vor dem Handel ein bestätigendes Signal vom Relative Strength Index (RSI). Der Ansatz spiegelt den ursprünglichen Expert Advisor wider, einschließlich seiner Risikomanagementphilosophie, Positionen umzukehren, wenn ein neues Signal erscheint.

## Handelslogik
1. **Trendfilter** – Ein kurzer einfacher gleitender Durchschnitt (Standardlänge 2) wird verwendet, um zu bestimmen, ob sich der Markt in einem Mikro-Abwärtstrend oder einem Aufwärtstrend befindet.
2. **Kerzenmuster** – Die Strategie analysiert die zuletzt abgeschlossene Kerze:
   - Ein **Hammer** wird erkannt, wenn der Körper im oberen Drittel der Spanne liegt, die Lücken der Kerze geringer sind als der vorherige Balken und der Mittelpunkt der Kerze unter dem Trend des gleitenden Durchschnitts liegt.
   - Ein **hängender Mann** wird erkannt, wenn der Körper im oberen Drittel sitzt, die Lücken der Kerze höher als der vorherige Balken sind und der Mittelpunkt der Kerze über dem Trend des gleitenden Durchschnitts liegt.
3. **RSI Filter** –
   - Für Long-Trades muss der RSI unter dem konfigurierbaren Hammer-Schwellenwert liegen (Standard 40).
   - Short-Trades erfordern, dass der RSI über dem Hanging-Man-Schwellenwert liegt (Standard 60).
4. **Handelsausführung** – Bei einem gültigen Signal beginnt die Strategie mit `Volume + |Position|`, sodass offene Positionen sofort umgekehrt werden, wenn das entgegengesetzte Setup eintritt.
5. **Ausstiegsregeln** – Positionen werden abgeflacht, wenn der RSI die konfigurierbare untere (Standard 30) oder obere (Standard 70) Grenze in die entgegengesetzte Richtung überschreitet, wodurch die Ausstiegsstimmen im Originalcode repliziert werden.

## Indikatoren
- **RelativeStrengthIndex** (Standardlänge 33).
- **SimpleMovingAverage** (Standardlänge 2) wird auf Kerzenschlüsse angewendet.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Auftragsgröße, die für Einträge verwendet wird. | `1` |
| `RsiPeriod` | RSI Lookback-Zeitraum. | `33` |
| `MaPeriod` | Zeitraum des gleitenden Durchschnitts für den Trendfilter. | `2` |
| `HammerRsiThreshold` | Maximaler RSI-Wert, der einen hammerlangen Eintrag ermöglicht. | `40` |
| `HangingManRsiThreshold` | Minimaler RSI-Wert, der einen Short-Einstieg mit hängendem Mann ermöglicht. | `60` |
| `LowerExitLevel` | Die RSI-Grenze wird verwendet, um Shorts nach einem Aufwärtskreuz zu schließen. | `30` |
| `UpperExitLevel` | Die RSI-Grenze wird zum Schließen von Long-Positionen nach einem Abwärtskreuz verwendet. | `70` |
| `CandleType` | Von der Strategie verarbeiteter Zeitrahmen. | `1 hour` Kerzen |

Alle Parameter können über die Parameter-Benutzeroberfläche StockSharp optimiert werden.

## Nutzungshinweise
- Die Logik funktioniert ausschließlich bei fertigen Kerzen. Stellen Sie sicher, dass der ausgewählte Zeitrahmen und der ausgewählte Datenfeed vollständige Balken erzeugen.
- Da die Umkehrlogik immer `Volume + |Position|` handelt, ändern Positionen sofort ihre Richtung, wenn das entgegengesetzte Signal vorliegt und mit dem Expert Advisor übereinstimmt.
- Starten Sie das integrierte Risikomanagement einmal beim Start (`StartProtection()` wird in `OnStarted` aufgerufen).

## Dateien
- `CS/AhHmRsiStrategy.cs` – Strategieumsetzung.
- `README.md` – Englische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
