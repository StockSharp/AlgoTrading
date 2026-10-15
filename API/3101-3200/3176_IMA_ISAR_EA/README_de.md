# iMA iSAR EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader 5 Expert Advisor „iMA iSAR EA" unter Verwendung der StockSharp High-Level-API. Sie kombiniert einen dreifachen gewichteten gleitenden Durchschnittsfilter mit zwei Parabolic-SAR-Trails, um Momentum-Ausbrüche zu identifizieren. Eine Long-Position wird eröffnet, wenn der schnellste gewichtete gleitende Durchschnitt über den anderen beiden Durchschnitten bleibt und beide SAR-Trails unterhalb des Kerzen-Schlusskurses liegen. Eine gespiegelte Bedingung erzeugt Short-Einstiege. Schutzstops, Gewinnziele und ein optionaler Trailing-Stop werden in Preispunkten (Pips) verwaltet.

Die Implementierung arbeitet auf einer einzigen Kerzenserie, die über den Parameter `CandleType` konfigurierbar ist. Alle Indikatoren werden auf diesem Zeitrahmen ausgewertet. Der ursprüngliche MetaTrader Expert verwendete mehrere Zeitrahmen für seine Indikatoren; in StockSharp wird dieses Verhalten durch individuelle Gleitdurchschnitts-Shifts approximiert, die jedes Signal um eine Anzahl abgeschlossener Bars verzögern können.

## Handelsregeln
- **Indikatoren**
  - Drei gewichtete gleitende Durchschnitte (`Fast`, `Normal`, `Slow`) berechnet auf dem konfigurierten Kerzenstream. Optionale Bar-Shifts emulieren die verzögerten Puffer aus dem originalen MQ5-Code.
  - Zwei Parabolic-SAR-Indikatoren (`FastSAR`, `NormalSAR`) teilen denselben Kerzenstream, haben aber unabhängige Beschleunigungs- und Maximalschrittparameter.
- **Einstiegsbedingungen**
  - **Long**: `Fast` MA liegt über `Normal` und `Slow`, während beide SAR-Werte unter dem Kerzen-Schlusskurs liegen.
  - **Short**: `Fast` MA liegt unter `Normal` und `Slow`, während beide SAR-Werte über dem Kerzen-Schlusskurs liegen.
  - Wenn ein Umkehrsignal erscheint, schließt die Strategie jedes entgegengesetzte Exposure und wechselt die Richtung in einer einzigen Marktorder.
- **Risikokontrollen**
  - Feste Stop-Loss- und Take-Profit-Niveaus werden in Pips ausgedrückt (Vielfache des Wertpapier-Preisschritts). Sie werden auf abgeschlossenen Kerzen ausgewertet.
  - Optionaler Trailing-Stop: Einmal aktiviert folgt der Stop dem Schlusskurs in konfigurierbarem Abstand und rückt nur vor, nachdem er sich um den angegebenen Trailing-Schritt bewegt hat.
  - Volumina werden an die Einstellungen `VolumeStep`, `MinVolume` und `MaxVolume` des Wertpapiers angepasst, bevor Orders gesendet werden.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|-----|----------|-------------|
| `Volume` | `decimal` | `0.1` | Basis-Ordergröße. Wird automatisch erhöht, um eine entgegengesetzte Position beim Richtungswechsel zu decken. |
| `StopLossPips` | `decimal` | `50` | Schutzstop-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | `decimal` | `50` | Gewinnziel-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `UseTrailing` | `bool` | `true` | Aktiviert das dynamische Trailing-Stop-Management. |
| `TrailingStopPips` | `decimal` | `25` | Abstand zwischen Preis und Trailing-Stop, in Pips. |
| `TrailingStepPips` | `decimal` | `5` | Minimale günstige Bewegung (Pips) bevor der Trailing-Stop vorrückt. |
| `CandleType` | `DataType` | `TimeFrameCandle 15m` | Kerzenserie für alle Berechnungen. |
| `FastMaPeriod` | `int` | `10` | Periode des schnellen gewichteten gleitenden Durchschnitts. |
| `FastMaShift` | `int` | `0` | Anzahl abgeschlossener Bars zur Rückverschiebung des schnellen MA. |
| `NormalMaPeriod` | `int` | `30` | Periode des normalen gewichteten gleitenden Durchschnitts. |
| `NormalMaShift` | `int` | `3` | Anzahl abgeschlossener Bars zur Rückverschiebung des normalen MA. |
| `SlowMaPeriod` | `int` | `60` | Periode des langsamen gewichteten gleitenden Durchschnitts. |
| `SlowMaShift` | `int` | `6` | Anzahl abgeschlossener Bars zur Rückverschiebung des langsamen MA. |
| `FastSarStep` | `decimal` | `0.02` | Beschleunigungsfaktor für den schnellen Parabolic SAR. |
| `FastSarMax` | `decimal` | `0.2` | Maximale Beschleunigung für den schnellen Parabolic SAR. |
| `NormalSarStep` | `decimal` | `0.02` | Beschleunigungsfaktor für den normalen Parabolic SAR. |
| `NormalSarMax` | `decimal` | `0.2` | Maximale Beschleunigung für den normalen Parabolic SAR. |

## Hinweise
- Trailing-Stop-Prüfungen erfolgen beim Kerzen-Schluss. Wenn Intrabar-Präzision erforderlich ist, kombinieren Sie die Strategie mit einer tick-basierten Schutzkomponente.
- Die Pip-Größe entspricht dem Wertpapier-Preisschritt, wenn verfügbar. Andernfalls wird ein Standard-Tick von `0.0001` für FX-Paare angenommen.
- Für Konsistenz mit der MetaTrader-Version arbeiten alle Indikatorsignale auf geschlossenen Kerzen. Ausstehende Transaktionen werden nicht vorbereitet; jedes Signal sendet sofort eine Marktorder.
