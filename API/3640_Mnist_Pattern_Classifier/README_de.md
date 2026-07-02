# Mnist-Musterklassifizierungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Herkunft

Die Strategie ist ein StockSharp-Port des MetaTrader 5-Experten **TestMnistOnnx.mq5** (MQL ID 47225). Das ursprüngliche Skript stellt eine interaktive Funktion bereit
Canvas, auf dem der Benutzer Ziffern zeichnet, die durch ein gebündeltes MNIST ONNX-Modell klassifiziert werden. Die StockSharp-Version behält den Geist von
Mustererkennung, sondern ersetzt die handgezeichnete Leinwand durch eine rollende Matrix aus fertigen Kerzen.

## Konzept

1. Ein rollierendes Fenster von `LookbackPeriod` abgeschlossenen Kerzen (Standard 28) wird als 28×28-Raster behandelt, ähnlich einem MNIST-Bild.
2. Mehrere statistische Merkmale – Bereichskomprimierung, Trendstärke, Momentum, RSI-Abweichung und ATR-Normalisierung – werden kombiniert
in einen synthetischen „Konfidenz“-Score um, der die vom MQL-Experten erstellte neuronale Netzwerkwahrscheinlichkeit nachahmt.
3. Die resultierenden Features werden einer von zehn Musterklassen (`0`–`9`) zugeordnet. Jede Klasse repräsentiert ein Marktregime
(Flat, Trend, Ausbruch, Pullback, Umkehr usw.).
4. Wenn die erkannte Klasse mit dem vom Benutzer ausgewählten `TargetClass` übereinstimmt und die synthetische Konfidenz über `ConfidenceThreshold` liegt,
Die Strategie eröffnet oder kehrt eine Position in die angegebene Richtung um. Positionen werden abgeflacht, wenn sich die Klasse ändert oder
Das Vertrauen fällt unter die Schwelle.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `LookbackPeriod` | 28 | Anzahl der fertigen Kerzen, die in das MNIST-ähnliche Raster umgewandelt werden. |
| `TargetClass` | 1 | Klassenindex (0–9), der Handelsaktionen auslösen soll. |
| `ConfidenceThreshold` | 0,6 | Minimale synthetische Wahrscheinlichkeit, die die Auftragserteilung ermöglicht. |
| `Volume` | 1 | Auftragsvolumen für neue Positionen. |
| `CandleType` | Zeitrahmen von 5 Minuten | Für Kerzenaktualisierungen abonnierter Datentyp. |

## Musterklassen

| Klasse | Bedeutung |
|-------|---------|
| 0 | Flache Konsolidierung oder Konsolidierung mit geringer Volatilität. |
| 1 | Anhaltender Aufwärtstrend. |
| 2 | Anhaltender Abwärtstrend. |
| 3 | Ausbruch nach oben mit starkem Follow-Through. |
| 4 | Ausbruch nach unten mit starkem Follow-Through. |
| 5 | Große Volatilitätsspanne ohne klare Tendenz. |
| 6 | Bullischer Pullback innerhalb eines Aufwärtstrends. |
| 7 | Abwärtstrend innerhalb eines Abwärtstrends. |
| 8 | Bullische Umkehr nach einem längeren Rückgang. |
| 9 | Abwärtstrend nach einem längeren Anstieg. |

## Handelsregeln

- Handelt nur mit fertigen Kerzen, um mit dem ursprünglichen Experten synchron zu bleiben, der auf die fertigen Zeichnungen reagiert hat.
- Verwendet Marktaufträge (`BuyMarket`, `SellMarket`) und flacht sie ab, bevor sie sich umkehrt, um das Einzelpositionsverhalten des zu imitieren
Originalskript.
- Die Konfidenzskalierung ist auf `[0, 1]` beschränkt. Durch Erhöhen von `ConfidenceThreshold` werden schwächere Signale herausgefiltert.
- Die Strategie verwaltet keine Schutzstopps; Das Risikomanagement wird voraussichtlich extern in StockSharp konfiguriert.

## Nutzungstipps

- Wählen Sie einen Kerzentyp, der den Marktrhythmus widerspiegelt, den Sie analysieren möchten. Kürzere Zeitrahmen reagieren schneller, sind aber lauter.
- Optimieren Sie `TargetClass` und `ConfidenceThreshold` gemeinsam – einige Klassen sind von Natur aus seltener und erfordern möglicherweise niedrigere Schwellenwerte.
- Der synthetische Musterklassifikator ist deterministisch; Es besteht keine Abhängigkeit von externen ONNX-Laufzeitbibliotheken.
- Kombinieren Sie es mit den in StockSharp verfügbaren integrierten Risikoschutztools (z. B. `StartProtection`), um die Gefährdung zu kontrollieren.

## Unterschiede zum Original

- Interaktives Zeichnen und ONNX-Inferenz werden durch eine vollautomatische Kerzenanalyse ersetzt.
- Das „Konfidenz“ ist eine deterministische Mischung von Indikatoren und keine Wahrscheinlichkeit eines neuronalen Netzwerks.
- Es wird eine Handelslogik hinzugefügt, um die Mustererkennung in umsetzbare Aufträge umzuwandeln.
- Die MNIST-Ressourcendatei ist in der StockSharp-Umgebung nicht erforderlich.
