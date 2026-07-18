# EMA Cross-Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist die StockSharp-Umstellung des MetaTrader 4-Expertenberaters mit Sitz in `MQL/8606/EMA_CROSS_2.mq4`. Es behält die ursprüngliche Idee bei, die Beziehung zwischen einem langsamen und einem schnellen exponentiellen gleitenden Durchschnitt zu verfolgen und eine einzelne Marktposition zu eröffnen, wenn ein Crossover auftritt. Schutzexits (Take-Profit, Stop-Loss und Trailing-Stop) werden über den übergeordneten `StartProtection`-Helfer abgewickelt, sodass das Verhalten die MetaTrader-Implementierung unter Verwendung der Best Practices von StockSharp widerspiegelt.

## Handelslogik
- Erstellen Sie Kerzen mit den konfigurierbaren `CandleType` (standardmäßig 15-Minuten-Balken) und füttern Sie zwei EMA-Indikatoren: Der langsame EMA verwendet `SlowEmaLength` und der schnelle EMA verwendet `FastEmaLength`.
- Behalten Sie die aktuelle Richtung des langsamen EMA relativ zum schnellen EMA bei. Die erste abgeschlossene Kerze, nachdem beide Indikatoren gebildet wurden, wird nur zur Initialisierung dieser Richtung verwendet, genau wie der `first_time`-Schutz im ursprünglichen Berater.
- Wenn sich der langsame EMA über den schnellen EMA bewegt (die neue Richtung wird zu `1`) und die Strategie flach ist, senden Sie eine Marktkauforder. Wenn der langsame EMA unter den schnellen EMA fällt (die neue Richtung wird zu `2`) und die Strategie flach ist, senden Sie einen Marktverkaufsauftrag. Dies reproduziert die genaue Auf-/Ab-Zuordnung der MQL-Funktion `Crossed(LEma, SEma)`.
- Es kann jeweils nur eine Position aktiv sein. Während ein Trade offen ist (oder die Einstiegsorder noch aussteht), werden zusätzliche Crossovers ignoriert.

## Handels- und Risikomanagement
- `StartProtection` konfiguriert Take-Profit-, Stop-Loss- und Trailing-Stop-Abstände in Preiseinheiten, die aus dem Instrument `PriceStep` berechnet werden. Trailing Stops sind optional: Setzen Sie `TrailingStopPips` auf Null, um sie zu deaktivieren.
- Aufträge werden mit `BuyMarket`/`SellMarket` aufgegeben und vom Markt geschlossen, wenn ein Schutzniveau ausgelöst wird, genau wie die `OrderSend`- und Nachfolgelogik des ursprünglichen Beraters.
- Die Basislosgröße wird durch `OrderVolume` gesteuert. Vor jeder Eingabe wird auf die Lautstärkestufe, das Minimum und das Maximum des Instruments abgeglichen, um eine Ablehnung zu vermeiden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Abstand in Pips (Preisschritten), der für den schützenden Take-Profit verwendet wird. Standard: 20. |
| `StopLossPips` | Abstand in Pips, der für den schützenden Stop-Loss verwendet wird. Standard: 30. |
| `TrailingStopPips` | Nachlaufdistanz in Pips. Auf `0` setzen, um das Nachstellen zu deaktivieren. Standard: 50. |
| `OrderVolume` | Losgröße der Markteintritte vor der Ausrichtung. Standard: 2. |
| `FastEmaLength` | Periode des schnellen EMA, der auf die Schlusskurse angewendet wird. Standard: 5. |
| `SlowEmaLength` | Zeitraum der langsamen EMA, angewendet auf die Schlusskurse. Standard: 60. |
| `CandleType` | Zeitrahmen für den Kerzenbau. Standard: 15 Minuten. |

## Notizen
- Die Strategie wartet, bis beide EMAs vollständig gebildet sind, bevor sie auf einen Crossover reagiert. Dabei wird die `Bars < 100`-Prüfung aus dem MQL-Skript entfernt und gleichzeitig die gleiche Stabilität erreicht.
- Da nur Marktaufträge verwendet werden, gibt es keine einzelnen `OrderModify`-Aufrufe. Das integrierte Schutzmodul positioniert den Trailing Stop automatisch auf die gleiche Weise neu, wie die MetaTrader-Schleife `OrderStopLoss` aktualisiert hat.
- Es wird kein Python-Port bereitgestellt (auf Anfrage); Es ist nur die C#-Implementierung enthalten.
