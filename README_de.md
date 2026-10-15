![StockSharp-Logo](logo.png)

# StockSharp-Beispiele für algorithmischen Handel

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dies ist das offizielle StockSharp-Repository für Beispiele zum algorithmischen Handel. Es verbindet einen großen, übersichtlich organisierten Katalog von API-Strategien mit visuellen Beispielen für den Strategy Designer, Lernmaterialien und automatischen Prüfungen, die die Kompilierbarkeit der Beispiele sicherstellen.

Das Repository ist für Lernen, Forschung, Prototyping und Regressionstests gedacht. Die Strategien veranschaulichen Handelsideen und die Verwendung der StockSharp-APIs; sie sind keine direkt einsetzbaren Anlageempfehlungen.

## Einstieg

| Ziel | Bereich |
|---|---|
| Strategien nach Handelsidee durchsuchen | [API-Strategiekatalog](API/README_de.md) |
| C#- und Python-Implementierungen untersuchen | [`API`](API/) |
| Visuelle Schemas und Designer-Beispiele ansehen | [`Designer`](Designer/) |
| Eine C#-Strategie kompilieren und testen | [`Backtester`](Backtester/) |
| Die automatisierte Strategie-Testumgebung untersuchen | [`Tests`](Tests/) |

## Inhalt des Repositorys

### API-Strategiekatalog

Das Verzeichnis [`API`](API/) enthält sowohl bekannte Ansätze – gleitende Durchschnitte, Ausbrüche, Momentum, Volatilität und Mean Reversion – als auch Paarhandel, Arbitrage, Market Making, Portfoliomethoden, Orderflow-Modelle, Experimente mit maschinellem Lernen und viele spezialisierte Varianten.

Der Katalog gruppiert Strategien nach ihrer wichtigsten Handelsidee. Im Dateisystem werden nummerierte Bereichsverzeichnisse verwendet, damit GitHub die große Sammlung effizient darstellen kann. Ein typisches Beispiel ist wie folgt aufgebaut:

```text
API/0001-0100/0001_MA_CrossOver/
├── CS/
│   ├── MaCrossoverStrategy.cs
│   └── logo.svg
├── PY/
│   ├── ma_crossover_strategy.py
│   └── logo.svg
├── README.md
├── README_ru.md
├── README_zh.md
├── README_es.md
├── README_de.md
├── README_pt.md
└── README_ja.md
```

Jedes API-Beispiel setzt dieselbe Strategieidee sowohl in C# als auch in Python um. Die Dokumentation in sieben Sprachen erläutert Konzept, Parameter, Signallogik und Risiken. Transparente SVG-Logos kennzeichnen Strategie und Implementierungssprache und bleiben in hellen wie dunklen Designs gut lesbar.

### Beispiele für den Strategy Designer

Das Verzeichnis [`Designer`](Designer/) enthält visuelle Schemas, wiederverwendbare Strategietypen und Lernbeispiele für den [StockSharp Strategy Designer](https://doc.stocksharp.com/en/topics/designer.html). Diese Beispiele sind hilfreich, wenn eine Strategie lieber grafisch aufgebaut und untersucht werden soll, statt direkt mit Quellcode zu beginnen.

### Build- und Testwerkzeuge

Das Repository enthält zwei kleine .NET-Projekte:

- [`Backtester`](Backtester/) kompiliert eine ausgewählte C#-Strategie dynamisch und führt sie mit den mitgelieferten historischen Beispieldaten aus.
- [`Tests`](Tests/) kompiliert die API-Beispiele und prüft sie in der historischen Emulationsumgebung von StockSharp.

Das Testprojekt verwendet einen Quellcodegenerator. Deshalb benötigen gewöhnliche Strategien keine handgeschriebenen Testmethoden. Jeder generierte Test führt eine Strategie mit Beispielmarktdaten aus, prüft die Erzeugung von Orders und Trades sowie das Klonen und die Serialisierung der Einstellungen. Für Strategien mit mehreren Instrumenten oder besonderer Einrichtung existieren explizite Überschreibungen im Testprojekt.

Vor dem .NET-Build führt [`Tools/validate_api_structure.py`](Tools/validate_api_structure.py) schnelle Strukturprüfungen aus: korrekte nummerierte Verzeichnisse, C#-/Python-Parität, erforderliche Übersetzungen, vorhandene Quelldateien und keine veralteten Aussagen über eine nicht verfügbare Sprachversion.

## Voraussetzungen

Für einen vollständigen lokalen Build werden benötigt:

- das .NET 10 SDK;
- Python 3 für den Strukturvalidator;
- ein benachbarter Checkout des StockSharp-Plattform-Repositorys.

Die Projektreferenzen erwarten folgende Verzeichnisstruktur:

```text
<workspace>/
├── AlgoTrading/
└── StockSharp (GitHub)/
```

Klone dieses Repository als `AlgoTrading` und das [StockSharp-Plattform-Repository](https://github.com/StockSharp/StockSharp) als `StockSharp (GitHub)` unter demselben übergeordneten Verzeichnis.

## Validieren, kompilieren und testen

Führe zuerst die schnellen Repository-Prüfungen aus:

```bash
python Tools/validate_api_structure.py
```

Kompiliere und teste anschließend die Lösung mit derselben Konfiguration wie in CI:

```bash
dotnet build AlgoTrading.slnx --configuration Release
dotnet test AlgoTrading.slnx --no-build --configuration Release
```

Um nur einen generierten Strategietest auszuführen, filtere nach dem Strategieverzeichnis in PascalCase. Beispiel:

```bash
dotnet test Tests/Tests.csproj --no-build --configuration Release \
  --filter "FullyQualifiedName~MaCrossover"
```

Ein einzelnes C#-Beispiel lässt sich direkt wie folgt kompilieren und testen:

```bash
dotnet run --project Backtester/Backtester.csproj -- \
  API/0001-0100/0001_MA_CrossOver/CS/MaCrossoverStrategy.cs
```

## Verwendung der Beispiele

Wähle eine Strategie im [Katalog](API/README_de.md), lies ihre Annahmen und Parameter und vergleiche die Implementierungen in C# und Python. Betrachte jedes Beispiel als Ausgangspunkt: Wähle geeignete Marktdaten, Gebühren, Slippage, Latenz, Positionsgrößen und Risikolimits, bevor du die Idee bewertest.

Für visuelle Entwicklung installiere den [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/), öffne seine [Strategy Gallery](https://doc.stocksharp.com/en/topics/designer/strategy_gallery.html) und verwende die Schemas in [`Designer`](Designer/) als Lernmaterial oder Prototypen.

Eine geänderte Strategie sollte stets mit Daten außerhalb der Stichprobe und in einer Simulation validiert werden, bevor ein Live-Einsatz erwogen wird. Ein Backtest zeigt das Verhalten für einen bestimmten Datensatz; er belegt keine zukünftige Rentabilität.

## Mitwirken

Beiträge zur Verbesserung von Korrektheit, Verständlichkeit, Abdeckung oder Lernwert sind willkommen. Beim Hinzufügen oder Ändern einer API-Strategie gelten folgende Regeln:

1. Die Strategie bleibt in ihrem nummerierten Bereichsverzeichnis.
2. C#- und Python-Implementierung werden gemeinsam gepflegt.
3. Die sieben lokalisierten README-Dateien entsprechen den tatsächlichen Parametern und dem tatsächlichen Verhalten.
4. Wenn sich die visuelle Identität der Strategie ändert, werden die transparenten SVG-Logos für beide Implementierungssprachen aktualisiert.
5. Vor einem Pull Request werden Strukturvalidator, Release-Build und relevante Tests ausgeführt.

Gewöhnliche Strategien werden automatisch vom Testgenerator erkannt. Eine manuelle Überschreibung ist nur nötig, wenn das Beispiel besondere Wertpapiere, Portfolios, Marktdaten oder eine andere Einrichtung benötigt, die die Standardumgebung nicht bereitstellen kann.

## Ressourcen

- [StockSharp-Website](https://stocksharp.com/)
- [Dokumentation](https://doc.stocksharp.com/en/)
- [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/)
- [Community-Chat](https://stocksharp.com/en/chat/)
- [Issue-Tracker](https://github.com/StockSharp/AlgoTrading/issues)

## Lizenz und Risikohinweis

Die für dieses Repository geltenden Bedingungen stehen in [LICENSE](LICENSE) und [NOTICE](NOTICE).

Algorithmischer Handel ist mit erheblichen Risiken verbunden. Die Beispiele werden ausschließlich für Lern- und technische Zwecke ohne Leistungsgarantie bereitgestellt. Vor dem Einsatz echten Kapitals bist du selbst dafür verantwortlich, den Code zu prüfen, Annahmen zu validieren und angemessene operative und finanzielle Risikokontrollen anzuwenden.
