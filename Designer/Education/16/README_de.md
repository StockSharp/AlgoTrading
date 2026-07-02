# Konverter-Block: Funktionalität "Maximales Volumen"
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Schema zeigt die Funktionalität des "Konverter"-Blocks mit Fokus auf die Einstellung "Maximales Volumen", die in eine Strategie integriert ist, die Kerzendaten aus Tick-Daten aufbaut.

## Überblick

Das Schema erklärt, wie der "Konverter"-Block genutzt werden kann, um Handelsstrategien zu verbessern, indem wichtige Momente auf der Grundlage von Volumendaten identifiziert werden. Die hier detaillierte Beispielstrategie kauft und verkauft basierend auf Kerzenmustern, die aus Tick-Daten gebildet werden.

## Schlüsselkomponenten

- **"Konverter"-Block mit "Maximales Volumen"**: Erklärt, wie dieser Block verwendet werden kann, um maximale Volumeninformationen aus Tick-Daten zu extrahieren und Entscheidungsprozesse zu unterstützen.
- **Kerzenstrategie**: Beschreibt eine Strategie, die auf Kerzenformationen basiert, bei der Entscheidungen auf den Eröffnungs- und Schlusskursen der Kerzen beruhen.

## Detaillierte Aufschlüsselung

### Strategielogik
- **Kaufbedingung**: Die Strategie initiiert eine Kauforder, wenn der Schlusskurs einer Kerze größer als ihr Eröffnungskurs ist, was auf eine bullische Stimmung hinweist.
- **Verkaufsbedingung**: Verkauf bei der sechsten Kerze unabhängig von der Preisbewegung, um kurzfristige Gewinne zu realisieren oder Verluste zu begrenzen — eine zeitbasierte Ausstiegsstrategie.

### Aktualisierungen in Version 5
- **Modifikation des Flag-Blocks**: Der "Flag"-Block und seine Auslösebedingungen wurden überarbeitet, um eine präzisere und konfigurierbarere Signalgebung zu ermöglichen.
- **Ersatz des Formelblocks**: Alle Blöcke aus dem Formelblock wurden in einen einzigen "Formel"-Block konsolidiert, was das Design vereinfacht und die Leistung verbessert.

## Praktische Anwendung

- **Volumenanalyse**: Durch den Einsatz des "Maximales Volumen"-Konverters können Händler die höchsten Volumenniveaus innerhalb eines bestimmten Zeitraums identifizieren, die oft auf erhebliches Marktinteresse oder potenzielle Wendepunkte hinweisen.
- **Kerzenbasierter Handel**: Die Strategie zeigt, wie Kerzenanalysen in Kombination mit Volumendaten für fundierte Handelsentscheidungen genutzt werden können, wobei sowohl trendfolgenden als auch konträren Ansätzen Rechnung getragen wird.

## Fazit

Dieses Schema veranschaulicht nicht nur den effektiven Einsatz des "Konverter"-Blocks in einem praktischen Handelsszenario, sondern beleuchtet auch die Verbesserungen der neuesten Softwareversion, um Nutzern bei der Anpassung an aktualisierte Funktionen zu helfen und ihre Handelsstrategien zu optimieren.
