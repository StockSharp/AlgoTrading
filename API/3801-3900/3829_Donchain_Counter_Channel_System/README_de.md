# Donchain-Gegenkanalsystem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Das **Donchain Counter-Channel System** reproduziert den MetaTrader 4 Expert Advisor von Michal Rutka aus dem Jahr 2005. Es sucht nach Umdrehungen in einem 20-tägigen Donchian-Kanal, der auf täglichen Kerzen berechnet wird. Wenn das untere Band nach oben dreht, geht die Strategie davon aus, dass es den Verkäufern nicht gelungen ist, den Preis auf neue Tiefststände zu drücken, und kauft in der nächsten Sitzung zum Marktpreis. Wenn sich das obere Band nach unten dreht, interpretiert die Strategie dies als einen Momentumverlust bei Rallyes und verkauft Leerverkäufe zum Marktwert. Schutzstopps sind immer auf das gegenüberliegende Donchian-Band ausgerichtet, sodass Exits die ursprüngliche Stopp-Management-Logik widerspiegeln.

Es ist nur eine Eingabe alle 24 Stunden zulässig, entsprechend der Regel aus dem Artikel, die das System auf höchstens eine Bestellung pro Tag beschränkt. Diese Implementierung verwendet StockSharps High-Level-API mit Indikatorbindungen, sodass die Donchian-Werte zusammen mit jeder abgeschlossenen Kerze eintreffen.

## Handelslogik
1. Abonnieren Sie den konfigurierten `CandleType` (standardmäßig täglich) und werten Sie einen `DonchianChannels`-Indikator mit dem ausgewählten `ChannelPeriod` aus.
2. Immer wenn eine Kerze zu Ende ist:
   - Wenn eine Long-Position offen ist, verschieben Sie das Stop-Level auf das aktuelle untere Band, wenn es steigt, und steigen Sie aus, wenn das Kerzentief dieses Level berührt.
   - Wenn eine Short-Position offen ist, verschieben Sie das Stop-Level auf das aktuelle obere Band, wenn es fällt, und steigen Sie aus, wenn das Kerzenhoch dieses Level berührt.
   - Wenn keine Position vorhanden ist, überspringen Sie Eingaben, wenn der letzte Handel vor weniger als `TradeCooldown` stattfand.
   - Gehen Sie long, wenn das untere Donchian-Band der vorherigen Kerze höher ist als das der Kerze davor, was einen Aufschwung in der Kanaluntergrenze signalisiert. Stellen Sie den Anfangsstopp auf das aktuelle untere Band ein.
   - Gehen Sie short, wenn das obere Donchian-Band der vorherigen Kerze niedriger ist als das der Kerze davor, was einen Abwärtstrend der Kanalobergrenze signalisiert. Stellen Sie den Anfangsstopp auf das aktuelle obere Band ein.
3. Verfolgen Sie den Stop weiterhin entlang der Bänder, bis der Preis durch sie hindurch eine Umkehr durchläuft, wodurch die Position geschlossen wird.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `1` | Ordergröße für Long- und Short-Einträge. |
| `ChannelPeriod` | `20` | Anzahl der Kerzen, die zur Berechnung der oberen und unteren Bänder von Donchian verwendet werden. |
| `TradeCooldown` | `1 day` | Mindestwartezeit, bevor ein erneuter Eintrag zulässig ist. |
| `CandleType` | `Daily` | Kerzenserie, auf der der Donchian-Kanal berechnet wird. |

## Indikatoren und Daten
- **Donchian Kanäle** – stellt die oberen und unteren Kanalgrenzen bereit, die zur Trendwendeerkennung und für Trailing Stops verwendet werden.
- **Tägliche Kerzen (Standard)** – liefern Schließzeiten, die für die 24-Stunden-Abklingzeit und für die Auswertung der Indikatorumdrehungen erforderlich sind.

## Implementierungshinweise
- Die Strategie verwendet `BindEx`, um ein eingegebenes `DonchianChannelsValue` im Candle-Handler zu empfangen, wodurch sichergestellt wird, dass beide Bänder gleichzeitig verfügbar sind.
- Stops werden simuliert, indem Kerzenhochs und -tiefs anhand des gespeicherten Bandwerts überwacht werden, genau wie der ursprüngliche EA seinen Stop-Loss bei jedem neuen Balken aktualisiert.
- Der Cooldown-Timer wird nur bei neuen Einträgen aktualisiert und spiegelt das Quellskript wider, das mehrere Einträge am selben Handelstag verhinderte.
