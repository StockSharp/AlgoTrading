# Darvas-Boxen-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie implementiert einen Ausbruch-Ansatz basierend auf dem klassischen **Darvas Boxes**-Konzept. Sie überwacht die Preisbewegung innerhalb eines dynamischen Preisbereichs (Box), der mithilfe des **Donchian Channels**-Indikators berechnet wird. Wenn der Preis oberhalb der oberen Grenze der Box schließt, wird eine Long-Position eröffnet. Wenn der Preis unterhalb der unteren Grenze schließt, wird eine Short-Position eröffnet. Optionale Stop-Loss- und Take-Profit-Niveaus bieten ein grundlegendes Risikomanagement.

## Funktionsweise

1. Für jede Kerze berechnet der Donchian Channels-Indikator die obere und untere Grenze anhand des angegebenen `BoxPeriod`.
2. Die Strategie verfolgt die vorherigen oberen und unteren Werte, um Ausbrüche zu erkennen.
3. Wenn der aktuelle Schlusskurs die vorherige obere Grenze nach oben kreuzt, führt die Strategie folgendes aus:
   - Schließt jede bestehende Short-Position (sofern erlaubt).
   - Eröffnet eine neue Long-Position (sofern erlaubt).
4. Wenn der aktuelle Schlusskurs die vorherige untere Grenze nach unten kreuzt, führt die Strategie folgendes aus:
   - Schließt jede bestehende Long-Position (sofern erlaubt).
   - Eröffnet eine neue Short-Position (sofern erlaubt).
5. Aktive Positionen werden auf Stop-Loss- und Take-Profit-Bedingungen überwacht.

## Parameter

- **BoxPeriod** (`int`): Anzahl der Kerzen zur Berechnung der Preisbox. Standardwert ist 20.
- **StopLoss** (`decimal`): Abstand vom Einstiegspreis zum Stop-Loss-Niveau. Standardwert ist 1000.
- **TakeProfit** (`decimal`): Abstand vom Einstiegspreis zum Take-Profit-Niveau. Standardwert ist 2000.
- **AllowBuyEntry** (`bool`): Ermöglicht das Eröffnen von Long-Positionen. Standardwert ist `true`.
- **AllowSellEntry** (`bool`): Ermöglicht das Eröffnen von Short-Positionen. Standardwert ist `true`.
- **AllowBuyExit** (`bool`): Ermöglicht das Schließen von Long-Positionen bei umgekehrten Signalen oder Risikoereignissen. Standardwert ist `true`.
- **AllowSellExit** (`bool`): Ermöglicht das Schließen von Short-Positionen bei umgekehrten Signalen oder Risikoereignissen. Standardwert ist `true`.
- **CandleType** (`DataType`): Kerzentyp für Berechnungen. Standardwert sind 4-Stunden-Kerzen.

## Verwendung

1. Hängen Sie die Strategie an ein Wertpapier an und legen Sie die gewünschten Parameterwerte fest.
2. Starten Sie die Strategie. Sie abonniert die konfigurierte Kerzenserie und verarbeitet eingehende Daten.
3. Trades werden mit Marktorders ausgeführt, wenn Ausbruchsbedingungen erfüllt sind.
4. Optionale Stop-Loss- und Take-Profit-Niveaus verwalten offene Positionen.

## Hinweise

- Die Strategie verwendet die High-Level-API mit `BindEx`, um Indikatorwerte und Kerzendaten zu verbinden.
- Interne Sammlungen werden vermieden; Indikatorwerte werden über den Binding-Callback abgerufen.
- Nur abgeschlossene Kerzen werden verarbeitet, um zuverlässige Signale sicherzustellen.
- Kommentare im Code sind auf Englisch, wie erforderlich.
