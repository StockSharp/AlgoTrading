# EA OBJPROP Diagramm-ID-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **EA OBJPROP-Diagramm-ID-Strategie** stellt das diagrammorientierte Verhalten des ursprünglichen MetaTrader 5-Beispiels wieder her, indem sie Donchian-Kanalumschläge in drei synchronisierten Zeitrahmen anzeigt. Das primäre Diagramm zeigt den Handelszeitrahmen, während zwei Hilfsfelder den H4- und Tageskontext visualisieren. Dieses Setup spiegelt den ursprünglichen Expert Advisor wider, der mehrere Diagramme und Indikatoren in einem einzigen Arbeitsbereich zur visuellen Analyse stapelte.

## Hauptmerkmale

- **Multi-Timeframe-Visualisierung** – abonniert automatisch Primär-, H4- und Tageskerzen für das ausgewählte Wertpapier.
- **Einheitliche Donchian-Kanallänge** – wendet auf jeden Zeitrahmen die gleiche Kanalperiode an, um die Umschläge vergleichbar zu halten.
- **High-Level-Chart-Integration** – basiert auf StockSharp Diagrammbereichen, um Preisreihen, Donchian-Kanäle und ausgeführte Trades darzustellen, wobei das MQL-Layout ohne Objektmanipulation auf niedriger Ebene reproduziert wird.
- **Erweiterbare Grundlage** – speichert die neuesten Kanalgrenzen für jeden Zeitrahmen, sodass die Strategie in Zukunft problemlos um Breakout- oder Bestätigungslogik erweitert werden kann.

## Parameter

| Parameter | Beschreibung | Kategorie | Standard |
|-----------|-------------|----------|---------|
| `ChannelLength` | Länge des Donchian-Kanals, der über alle abonnierten Zeiträume hinweg verwendet wird. | Indikatoren | 22 |
| `PrimaryCandleType` | Hauptzeitrahmen, der für den Handel und als oberes Diagrammfeld verwendet wird. | Allgemein | 30-Minuten-Kerzen |
| `H4CandleType` | Zusätzlicher H4-Zeitrahmen, der in einem sekundären Bereich angezeigt wird. | Allgemein | 4-Stunden-Kerzen |
| `DailyCandleType` | Zusätzlicher täglicher Zeitrahmen, der in einem tertiären Bereich angezeigt wird. | Allgemein | 1-Tages-Kerzen |

Alle Parameter sind über die Parameter-Benutzeroberfläche von StockSharp verfügbar, unterstützen die Optimierung und können ohne Änderung des Codes feinabgestimmt werden.

## Strategielogik

1. Initialisiert drei Donchian-Kanalindikatoren mit demselben Längenparameter.
2. Abonniert die ausgewählte Primär-, H4- und Tageskerzenserie für das aktuelle Wertpapier.
3. Bindet jedes Abonnement mithilfe des übergeordneten API an seinen jeweiligen Kanalindikator und stellt so sicher, dass die Indikatorwerte inkrementell berechnet werden.
4. Erstellt einen Hauptdiagrammbereich und bis zu zwei Hilfsbereiche, in denen Kerzen, Kanäle und die Trades der Strategie gezeichnet werden.
5. Speichert die aktuellsten oberen und unteren Kanalgrenzen für jeden Zeitrahmen, sodass später benutzerdefinierte Entscheidungsregeln hinzugefügt werden können.

Die aktuelle Implementierung dient nur der Visualisierung und sendet keine Bestellungen. Dies spiegelt den ursprünglichen MetaTrader-Code wider, der sich auf die Erstellung eines Dashboards mit Diagrammen ohne automatisierte Handelslogik konzentrierte.

## Nutzungshinweise

- Stellen Sie sicher, dass das ausgewählte Wertpapier über historische Daten für jeden von der Strategie verwendeten Zeitrahmen verfügt, um alle Diagrammbereiche zu füllen.
- Sie können jeden der Zeitrahmenparameter in andere `TimeFrame`-Datentypen (z. B. 15 Minuten oder wöchentliche Kerzen) ändern, wenn andere Kontextbereiche erforderlich sind.
- Zusätzliche Handelslogik kann in den Verarbeitungsmethoden (`ProcessPrimary`, `ProcessH4`, `ProcessDaily`) geschichtet werden, indem auf die gespeicherten Kanalebenen reagiert wird.

## Konvertierungshinweise

- Das MetaTrader-Beispiel erstellte untergeordnete Diagramme über `OBJ_CHART`-Objekte; Die StockSharp-Version ersetzt dies durch Diagrammbereiche, die von der übergeordneten API-Version erstellt wurden, die besser in die Plattform integriert ist.
- Die Indikatorverwaltung erfolgt über `BindEx`-Aufrufe anstelle der manuellen Handle-Erstellung, um sicherzustellen, dass die Werte mit eingehenden Kerzen synchronisiert werden.
- Routinen zum Löschen von Objekten sind nicht erforderlich, da StockSharp Abonnements und Diagrammbindungen automatisch verwirft, wenn die Strategie stoppt.
