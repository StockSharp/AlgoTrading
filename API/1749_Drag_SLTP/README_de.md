# Drag SL/TP Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert automatisch Stop-Loss- und Take-Profit-Orders in einem festen Abstand vom ausgeführten Handelspreis. Sie ist nützlich, wenn manuelle Positionen unmittelbar nach dem Einstieg abgesichert werden sollen.

## Parameter

- **Auto Set SL** (`bool`): automatische Stop-Loss-Platzierung aktivieren.
- **SL Points** (`decimal`): Stop-Loss-Abstand in Preisschritten.
- **Auto Set TP** (`bool`): automatische Take-Profit-Platzierung aktivieren.
- **TP Points** (`decimal`): Take-Profit-Abstand in Preisschritten.

## Verhalten

Beim Start der Strategie wird `StartProtection` mit den gewählten Abständen aufgerufen. Jede Position, die während der Laufzeit der Strategie eröffnet wird, erhält sofort die entsprechenden Schutzorders. Die Abstände werden in Preisschritten gemessen (`Security.PriceStep`).

Die Strategie selbst erzeugt keine Handelssignale; sie verwaltet lediglich Schutzorders für Positionen, die manuell oder durch andere Strategien eröffnet wurden.

## Hinweise

- Entwickelt für die Nutzung mit der High-Level-API.
- Nur der abgeschlossene Kerzenstatus sollte in erweiterten Versionen Handelsaktionen auslösen.
- Die grafische Drag-Funktion aus dem ursprünglichen MQL-Skript ist nicht implementiert.
