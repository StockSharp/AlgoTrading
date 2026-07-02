# Nina EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Nina EA-Strategie ist ein One-Position-Trendfolger, der vom MetaTrader 4-Experten „NinaEA“ übernommen wurde. Der ursprüngliche Roboter verwendet einen benutzerdefinierten Indikator namens **NINA** und handelt immer dann, wenn die Differenz zwischen den bullischen und bärischen Puffern des Indikators über oder unter Null fällt. In der StockSharp-Version wird der benutzerdefinierte Indikator durch den integrierten **SuperTrend**-Indikator ersetzt, der auch separate bullische und bärische Puffer veröffentlicht. Eine Umkehrung der SuperTrend-Richtung dient als Nulldurchgangs-Proxy: Wenn der Trend bullisch wird, kauft die Strategie, und wenn er bärisch wird, verkauft sie.

Die Strategie behält immer höchstens eine offene Position. Ein entgegengesetztes Signal schließt sofort die bestehende Position und etabliert einen neuen Handel in die neue Richtung. Ein optionaler Stop-Loss, ausgedrückt in Preispunkten, kann aktiviert werden, um die ursprüngliche „StopLoss“-Eingabe nachzuahmen.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie und berechnen Sie SuperTrend mit der angegebenen ATR-Periode und dem Multiplikator.
2. Warten Sie, bis sowohl die Strategie als auch der Indikator festgelegt sind, bevor Sie auf Signale reagieren.
3. Bei jeder fertigen Kerze:
   - Wenn ein schützender Stop-Preis erreicht wird, verlassen Sie die offene Position zum Marktwert.
   - Wenn der SuperTrend von bärisch zu bullisch wechselt, schließen Sie alle Short-Positionen und kaufen Sie mit dem konfigurierten Volumen.
   - Wenn der SuperTrend von bullisch zu bärisch wechselt, schließen Sie alle Long-Engagements und verkaufen Sie mit dem konfigurierten Volumen.
   - Speichern Sie die aktuelle SuperTrend-Richtung, um die nächste Umkehr zu erkennen.

Die Logik repliziert das Verhalten des MetaTrader-Experten, wobei `nina = Buffer0 - Buffer1` und ein Vorzeichenwechsel sowohl Exits als auch neue Einträge bewirken.

## Positions- und Risikomanagement
- Es kann jeweils nur eine Position aktiv sein; Alle Trades kehren die Richtung um, anstatt mehrere Orders zu stapeln.
- Aus dem Füllpreis wird ein optionaler Stop-Loss in Preispunkten berechnet. Bei einem Long-Trade wird der Stop unterhalb des Einstiegs platziert, bei einem Short-Trade wird er oberhalb des Einstiegs platziert. Wenn Sie den Parameter auf Null setzen, wird der Stopp deaktiviert.
- `StartProtection()` wird aufgerufen, damit bei Bedarf integrierte StockSharp-Schutzmaßnahmen konfiguriert werden können.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Volume` | `0.1` | Bestellvolumen, das für jeden neuen Eintrag verwendet wird. |
| `AtrPeriod` | `10` | ATR Zeitraum, der an die SuperTrend-Berechnung übergeben wurde (bildet das Original `PeriodWATR` zu). |
| `AtrMultiplier` | `1` | ATR-Multiplikator für SuperTrend (bildet den ursprünglichen `Kwatr` ab). |
| `StopLossPoints` | `0` | Optionale Stop-Loss-Distanz in Preispunkten. Bei Zero bleibt der Stop deaktiviert, identisch mit dem MetaTrader-Code, der Market-Orders ohne Stop-Preis gesendet hat. |
| `CandleType` | `TimeFrame(1 minute)` | Kerzenserie, die den Indikator und die Handelslogik speist. |

## Konvertierungshinweise
- Der MetaTrader-Experte verließ sich auf den benutzerdefinierten `NINA`-Indikator. Seine beiden Puffer wurden als bullische/bärische SuperTrend-Linien interpretiert, da nur ihre Differenz und ihr Vorzeichen für den Handel von Bedeutung waren. SuperTrend stellt dieselben Informationen über sein `IsUpTrend`-Flag bereit, was es zu einem geeigneten High-Level-Ersatz macht, der keine manuelle Pufferbehandlung erfordert.
- Die Order-Closing-Logik spiegelt die `OrdersTotal()`-Schleife aus dem ursprünglichen Skript wider: Eine Trendumkehr schmeichelt zunächst der aktuellen Position und eröffnet dann einen Trade in die neue Richtung.
- Die nicht verwendeten MetaTrader-Eingaben (`highlow`, `cbars`, `from`, `maP`, `SMAspread`, `Slippage`) werden weggelassen, da sie keinen Einfluss auf die Handelsregeln in der Originaldatei haben.

## Nutzungstipps
1. Hängen Sie die Strategie an ein Wertpapier an und konfigurieren Sie den Kerzenzeitrahmen, der Ihrem MetaTrader-Test entspricht.
2. Passen Sie den ATR-Zeitraum und den Multiplikator an, um das Verhalten des ursprünglichen Indikators zu reproduzieren.
3. Erhöhen Sie `StopLossPoints`, wenn Sie ein hartes Risikolimit wünschen; andernfalls belassen Sie den Wert für rein signalbasierte Ausgänge auf Null.
