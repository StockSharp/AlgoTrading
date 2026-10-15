# Drei-MA-Cross-Channel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Three MA Cross Channel Strategy** wandelt den MetaTrader Expert Advisor `3MaCross_EA` in den StockSharp High-Level API um. Es überwacht drei konfigurierbare gleitende Durchschnitte und eröffnet Geschäfte, wenn der schnellere Durchschnitt den langsameren kreuzt. Ein Donchian-Preiskanal wird optional zur Verwaltung von Exits verwendet und ähnelt stark dem ursprünglichen EA, der auf den „Preiskanal“-Indikator verwies.

## Handelslogik
- **Long-Einstieg**: Wird generiert, wenn sowohl der schnelle als auch der mittlere gleitende Durchschnitt über dem langsamen gleitenden Durchschnitt schließen und einer der beiden schnelleren Durchschnitte den langsamen Durchschnitt auf dem aktuellen Balken kreuzt.
- **Short-Einstieg**: Wird ausgelöst, wenn sowohl der schnelle als auch der mittlere gleitende Durchschnitt unter dem langsamen gleitenden Durchschnitt schließen und einer der beiden schnelleren Durchschnitte den langsamen Durchschnitt unterschreitet.
- **Positionsausgang**:
  - Gegenüberliegendes Crossover-Signal.
  - Optionaler Donchian-Kanalstopp: Long-Positionen werden geschlossen, wenn der Preis unter das untere Band fällt; Short-Positionen werden geschlossen, wenn der Preis über das obere Band steigt.
  - Optionale feste Take-Profit- oder Stop-Loss-Abstände, gemessen in absoluten Preiseinheiten.

Die Strategie wartet immer auf abgeschlossene Kerzen und entspricht dem `TradeAtCloseBar`-Verhalten des ursprünglichen Skripts. Es wird jeweils nur eine Richtungsposition beibehalten; Wenn ein Signal für eine bestehende Position erscheint, wird der aktuelle Handel geschlossen, bevor ein neuer eröffnet wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|------|---------|-------------|
| `FastLength` | `int` | `2` | Rückblick auf den schnellen gleitenden Durchschnitt. |
| `MediumLength` | `int` | `4` | Rückblick auf den mittleren gleitenden Durchschnitt. |
| `SlowLength` | `int` | `30` | Rückblick auf den langsam gleitenden Durchschnitt. |
| `ChannelLength` | `int` | `15` | Donchian Kanalfenster, das für kanalbasierte Exits verwendet wird. |
| `FastType` | `MovingAverageTypeEnum` | `EMA` | Algorithmus für gleitenden Durchschnitt, angewendet auf den schnellen Durchschnitt (SMA, EMA, SMMA, WMA). |
| `MediumType` | `MovingAverageTypeEnum` | `EMA` | Algorithmus für gleitenden Durchschnitt, angewendet auf den mittleren Durchschnitt. |
| `SlowType` | `MovingAverageTypeEnum` | `EMA` | Algorithmus für den gleitenden Durchschnitt, der auf den langsamen Durchschnitt angewendet wird. |
| `TakeProfit` | `decimal` | `0` | Gewinnziel in absoluten Preiseinheiten. Zum Deaktivieren auf `0` setzen. |
| `StopLoss` | `decimal` | `0` | Verlustlimit in absoluten Preiseinheiten. Zum Deaktivieren auf `0` setzen. |
| `UseChannelStop` | `bool` | `true` | Aktiviert Donchian Kanalausgänge. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Für Berechnungen verwendeter Kerzentyp. |

## Notizen
- Alle gleitenden Durchschnitte verwenden Schlusskurse und können individuell konfiguriert werden, um den Optionen `FasterMode`, `MediumMode` und `SlowerMode` des ursprünglichen EA zu entsprechen.
- `TakeProfit` und `StopLoss` verwenden absolute Preisabstände (z. B. entspricht `0.0010` 10 Pips auf einem 5-stelligen Forex-Symbol). Sie werden bei Kerzenschlüssen ausgewertet und reproduzieren die Balken-Schließungsverwaltung von EA.
- Wenn `UseChannelStop` aktiviert ist, reproduziert die Strategie das automatische Stop-Loss-Verhalten, das auf dem benutzerdefinierten Indikator `Price Channel` beruhte.
- Die Strategie zeichnet die drei gleitenden Durchschnitte, den Kanal Donchian und Handelsmarkierungen zur visuellen Bestätigung auf dem Diagramm auf.
