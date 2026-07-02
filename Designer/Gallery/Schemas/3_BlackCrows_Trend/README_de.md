# Beschreibung der 3-Black-Crows-Trend-Strategie im StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieübersicht

Die „3 Black Crows Trend"-Strategie im [Strategy Designer](https://doc.stocksharp.com/topics/designer.html) verwendet ein spezifisches bearisches Umkehr-Candlestick-Muster, um potenzielle Abwärtsbewegungen am Aktienmarkt vorherzusagen. Dieses automatisierte Handelsschema ist sorgfältig entwickelt, um bedeutende Preismuster zu erkennen und darauf zu reagieren, mit dem Ziel, von bearischen Trends zu profitieren.

![schema](schema.png)

## Strategiedetails

### Mustererkennung: 3 Black Crows

- **Beschreibung**: Dieses Modul identifiziert das „3 Black Crows"-[Muster](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html), das eine potenzielle bearische Umkehr nach einem Aufwärtstrend signalisiert. Das Muster besteht aus drei aufeinanderfolgenden Kerzen mit langen Körpern, die jeweils unterhalb ihrer Eröffnungspreise schließen, wobei die Eröffnung jeder Sitzung innerhalb des Körpers der vorherigen Kerze liegt.
- **Bedingungen**:
  - Kerze 1: Open > Close
  - Kerze 2: Open > Close und Open < Previous Open
  - Kerze 3: Open > Close und Open < Previous Open

### Handelsausführung

- **Ordertyp**: Market-[Order](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)
- **Einstieg**: Initiiert eine Verkaufsorder bei Bestätigung des „3 Black Crows"-Musters.
- **Ausstiegsstrategie**:
  - **Take Profit**: Auf 3% über dem Einstiegspreis gesetzt.
  - **Stop Loss**: Auf 1% unter dem Einstiegspreis gesetzt.
- **Risikomanagement**: Die Strategie hält strikt an den initialen [Stop-Loss- und Take-Profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)-Einstellungen ohne Trailing fest.

### Handelsbedingungen

- **Frequenz**: Betreibt auf einem [täglichen Zeitrahmen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) und verarbeitet neue Kerzenformationen am Ende jedes Handelstages.
- **Market Order**: Gewährleistet schnelle Ausführung durch das [Platzieren von Trades](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) zu aktuellen Marktpreisen.

## Implementierungsdetails

- **Plattform**: Auf der StockSharp-Plattform implementiert, die umfassende Funktionen für die Mustererkennung und automatisierte Handelsausführung bietet.
- **Einstellungen**:
  - **Protokollierungsstufe**: Konfigurierbar für detaillierte operative Einblicke.
  - **Parameteranzeige**: Anpassbare Anzeigeeinstellungen für operative Transparenz.
  - **Nullwertverarbeitung**: Konfigurierbare Behandlung von Nullwerten zur Verbesserung der Robustheit und Zuverlässigkeit.

## Fazit

Die „3 Black Crows Trend"-Strategie ist für Trader konzipiert, die sich auf die Identifizierung und Ausnutzung bearischer Umkehrmuster konzentrieren. Sie kombiniert präzise Mustererkennung mit strengen Handelsausführungsregeln, um die potenzielle Profitabilität in bearischen Marktszenarien zu steigern.
