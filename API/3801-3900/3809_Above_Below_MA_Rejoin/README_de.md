# Oben Unten MA Rejoin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Above Below MA Rejoin-Strategie ist eine StockSharp-Umsetzung des MetaTrader 4-Expertenberaters „AboveBelowMA“. Das ursprüngliche Skript überwacht den 15-Minuten-Chart von GBP/USD und vergleicht den aktuellen Preis mit einem exponentiellen gleitenden Durchschnitt (EMA) über eine Periode, der anhand des typischen Preises berechnet wird. Wenn der Preis auf der entgegengesetzten Seite eines steigenden oder fallenden Durchschnitts handelt, versucht die Strategie, diese Abweichung abzuschwächen und sich wieder der zugrunde liegenden Richtung von EMA anzuschließen. Dieser Port hält die Signalstruktur intakt und nutzt gleichzeitig StockSharp High-Level-APIs (`SubscribeCandles` + `Bind`).

## Handelslogik
- Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig 15 Minuten) und geben Sie einen exponentiellen gleitenden Durchschnitt ein, der den typischen Preis `(High + Low + Close) / 3` verwendet.
- Verfolgen Sie die neuesten und vorherigen EMA-Werte, um die kurzfristige Steigung zu verstehen. Eine bullische Tendenz erfordert einen Anstieg des EMA, während eine bärische Tendenz einen Rückgang erfordert.
- **Long-Setup:** Wenn die Kerze mindestens eine Preisstufe unter EMA öffnet, unter EMA schließt und der vorherige EMA-Wert niedriger als der aktuelle EMA-Wert ist, schließen Sie alle Short-Positionen und bereiten Sie sich auf den Kauf vor. Wenn keine Position mehr vorhanden ist, erteilen Sie eine Marktkauforder.
- **Short-Setup:** Wenn die Kerze mindestens eine Preisstufe über EMA öffnet, über EMA schließt und der vorherige EMA-Wert höher ist als der aktuelle EMA-Wert, schließen Sie alle Long-Engagements und bereiten Sie den Verkauf vor. Wenn die Position flach ist, erteilen Sie einen Marktverkaufsauftrag.
- Aufträge werden nur für fertige Kerzen erteilt, um vorzeitige Signale bei teilweise geformten Balken zu vermeiden.

## Positionsgrößenbestimmung
- Die Größe der MetaTrader-Version wird mit `AccountFreeMargin / 10000` gehandelt, die auf 5 Lots begrenzt ist. Die Implementierung von StockSharp bietet ein äquivalentes Verhalten: Wenn `UseDynamicVolume` aktiviert ist, dividiert die Strategie den aktuellen Portfoliowert durch `BalanceToVolumeDivider` (Standard: `10000`).
- Die berechnete Größe ist durch `MaxVolume` begrenzt und spiegelt die feste 5-Lot-Obergrenze des Expertenberaters wider. Wenn die dynamische Größenanpassung deaktiviert ist, wird der Parameter `InitialVolume` als festes Volumen verwendet.
- Alle Volumina sind an den Volumenschritt des Instruments und die minimalen/maximalen Volumenbeschränkungen angepasst, um eine Ablehnung durch den Broker oder Simulator zu vermeiden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `EmaLength` | Periode des exponentiellen gleitenden Durchschnitts (standardmäßig 1, passend zu EA). |
| `CandleType` | Zeitrahmen, der zum Erstellen der Kerzen verwendet wird, die den EMA versorgen (Standard 15 Minuten). |
| `InitialVolume` | Das Bestellvolumen wurde korrigiert, wenn die dynamische Größenanpassung deaktiviert ist. |
| `UseDynamicVolume` | Ermöglicht die portfoliobasierte Positionsgrößenbestimmung (`Balance / BalanceToVolumeDivider`). |
| `BalanceToVolumeDivider` | Auf den Portfoliowert angewendeter Teiler, um `AccountFreeMargin / 10000` zu emulieren. |
| `MaxVolume` | Maximales Auftragsvolumen, das die Strategie zulässt. |

## Notizen
- Die Strategie verwendet `ClosePosition()`, bevor ein Trade in die entgegengesetzte Richtung eröffnet wird, und entspricht der MetaTrader-Logik, die gegensätzliche Orders über `CheckOrders` schließt.
- Da Signale an fertigen Kerzen ausgewertet werden, können Einträge etwas später erfolgen als bei der Tick-basierten MetaTrader-Version. Diese Änderung verbessert die Stabilität beim Ausführen von Backtests oder beim Live-Handel mit Kerzendaten.
- Stellen Sie sicher, dass das ausgewählte Wertpapier aussagekräftige `PriceStep`-, `VolumeStep`- und Portfoliobewertungsinformationen bereitstellt, damit der dynamische Volumenblock wie erwartet funktioniert.
