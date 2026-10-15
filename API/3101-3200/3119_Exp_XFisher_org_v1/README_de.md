# Exp XFisher org v1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert den MetaTrader 5-Experten **Exp_XFisher_org_v1**. Sie handelt Umkehrungen, die an der Fisher-Transformation des Preises erkannt werden, der zusätzlich mit einem konfigurierbaren gleitenden Durchschnitt geglättet wird. Der StockSharp-Port behält die Gegentrend-Natur des ursprünglichen Roboters bei: wenn die Fisher-Kurve nach einem Aufwärtsschwung abwärts dreht, wird eine Long-Position eröffnet, und wenn die Kurve nach einem Abwärtsschwung aufwärts dreht, wird eine Short-Position eröffnet. Bestehende Positionen werden geschlossen, sobald der Indikator in die entgegengesetzte Richtung umkehrt.

Der Hilfsindikator `XFisherOrgIndicator` in `CS/ExpXFisherOrgV1Strategy.cs` folgt der MT5-Logik:

1. Das höchste Hoch und niedrigste Tief über `Length` abgeschlossene Kerzen nehmen.
2. Die ausgewählte Preisquelle (siehe *Applied Price* unten) anhand dieser Extreme in den Bereich 0–1 umrechnen.
3. Den rekursiven Filter `value = (wpr - 0.5) + 0.67 * value[prev]` gefolgt von der Fisher-Transformation anwenden
   `fish = 0.5 * ln((1 + value) / (1 - value)) + 0.5 * fish[prev]`.
4. Das Ergebnis mit einem der unterstützten gleitenden Durchschnitte glätten. Der geglättete Fisher-Wert bildet die Hauptlinie; die Signallinie ist einfach der Wert der vorherigen Bar, genau wie in der MQL-Version, wo Puffer #1 eine Einbar-Verschiebung speichert.

Die Konvertierung behält die ursprünglichen Standardwerte (`Length = 7`, Jurik-Glättung der Länge 5, Phase 15, H4-Kerzen) bei und stellt dieselben Enable/Disable-Schalter für das Öffnen und Schließen von Long/Short-Trades bereit.

## Handelsregeln
- **Long-Einstieg** – wenn der Fisher-Wert von `SignalBar + 1` Bars vor stieg (`Fisher[SignalBar+1] > Fisher[SignalBar+2]`)
  aber der Wert bei `SignalBar` unter seine verzögerte Kopie kreuzt oder diese berührt (`Fisher[SignalBar] <= Fisher[SignalBar+1]`).
- **Short-Einstieg** – wenn der Fisher-Wert von `SignalBar + 1` Bars vor fiel, aber der Wert bei `SignalBar` über seine verzögerte Kopie kreuzt.
- **Positions-Exit** – die entgegengesetzte Umkehr schließt eine bestehende Position, bevor ein neuer Trade in Betracht gezogen wird. Ein Long-Exit wird durch dieselbe Bedingung ausgelöst, die einen Short öffnet, und umgekehrt.
- **Volumen** – wird durch `OrderVolume` gesteuert. Wenn ein Flip von Short zu Long (oder Long zu Short) erforderlich ist, sendet die Strategie eine einzelne Marktorder mit ausreichendem Volumen, um die alte Position zu schließen und die neue in derselben Transaktion zu öffnen.

Alle Berechnungen verwenden **ausschließlich abgeschlossene Kerzen**. Wenn `SignalBar` null ist, wird die aktuelle geschlossene Kerze zur Signalauswertung verwendet; positive Werte verschieben das Signal zeitlich zurück, genau wie der MT5-`SignalBar`-Input.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `OrderVolume` | Volumen jeder Marktorder. | `1` |
| `BuyOpenAllowed` / `SellOpenAllowed` | Öffnen von Long/Short-Trades erlauben. | `true` |
| `BuyCloseAllowed` / `SellCloseAllowed` | Schließen bestehender Long/Short-Trades erlauben. | `true` |
| `SignalBar` | Verschiebung (in geschlossenen Kerzen) zum Lesen der Fisher-Puffer. | `1` |
| `Length` | Lookback für höchste/niedrigste Preisextrema. | `7` |
| `SmoothingLength` | Periode des Glättungsdurchschnitts. | `5` |
| `Phase` | Jurik-Phase (von anderen Methoden ignoriert). | `15` |
| `SmoothingMethod` | Gleitender Durchschnitt, der auf den Fisher-Output angewendet wird. | `Jjma` |
| `PriceType` | Applied Price, die an den Indikator weitergeleitet wird (Schluss, Eröffnung, Median, etc.). | `Close` |
| `CandleType` | Kerzenserie für die Berechnung (Standard: 4-Stunden-Kerzen). | `H4` |

## Glättungsmethoden-Mapping
Der ursprüngliche Indikator stellt eine große Auswahl an Glättungskernen bereit. Der StockSharp-Port ordnet sie zuverlässigen eingebauten Implementierungen zu:

- `Jjma`, `Jurx`, `T3` → `JurikMovingAverage` (Phasenparameter wird angewendet, wenn die Eigenschaft verfügbar ist).
- `Sma`, `Ema`, `Smma`, `Lwma` → jeweilige StockSharp-Durchschnitte.
- `Parabolic` → durch `ExponentialMovingAverage` approximiert (nächstes Verhalten unter StockSharp).
- `Vidya`, `Ama` → `KaufmanAdaptiveMovingAverage` (adaptives VIDYA-Verhalten wird mit Kaufman AMA modelliert).

Dieses Mapping spiegelt den in anderen Kositsin-Konvertierungen im Repository verwendeten Ansatz und hält die Reaktion der geglätteten Fisher-Linie vergleichbar mit der MT5-Implementierung.

## Unterschiede zum MT5-Experten
- **Geldmanagement** – StockSharp-Strategien arbeiten mit expliziten Volumen. Die `MM`/`MarginMode`-Inputs von MT5 werden durch einen einzelnen `OrderVolume`-Parameter ersetzt, damit der Trader die Lotgröße direkt definieren kann.
- **Ausführungsmodell** – Trades werden einmal pro abgeschlossener Kerze über die High-Level-Subscription-API generiert, anstatt bei jedem Tick. Dies vermeidet doppelte Orders und macht den ursprünglichen `IsNewBar`-Helper überflüssig.
- **Applied Price-Optionen** – alle Preismodi aus `SmoothAlgorithms.mqh` sind unterstützt, einschließlich TrendFollow- und Demark-Varianten.
- **Charting** – die Strategie zeichnet Kerzen, die geglättete Fisher-Transformation und die ausgeführten Trades im Standardchartbereich.

## Dateien
- `CS/ExpXFisherOrgV1Strategy.cs` – Strategieklasse, Indikatorimplementierung und Wertecontainer.
