# Patterns EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Patterns EA Strategie ist ein Preisaktionssystem, das die drei zuletzt abgeschlossenen Kerzen auf eine breite Palette von Ein-, Zwei- und Drei-Balken-Formationen scannt. Die Logik ist ein StockSharp-Port des originalen MQL5 Expert Advisors "Patterns_EA" und bewahrt seinen konfigurierbaren Katalog von 30 Candlestick-Setups. Jedes Muster kann unabhängig aktiviert oder deaktiviert werden und kann entweder Long- oder Short-Ausführung zugewiesen werden, wodurch die Strategie die diskretionären Regeln des Quellskripts nachahmen kann.

## Mustergruppen
Der Detektor wertet die aktuelle Kerze und bis zu zwei vorherige Kerzen aus, abhängig von der Mustergruppe:

- **Gruppe 1 – Ein-Balken-Muster:** Neutral Bar, Force Bar Up, Force Bar Down, Hammer, Shooting Star.
- **Gruppe 2 – Zwei-Balken-Muster:** Inside, Outside, Double Bar High Lower Close, Double Bar Low Higher Close, Mirror Bar, Bearish Harami, Bearish Harami Cross, Bullish Harami, Bullish Harami Cross, Dark Cloud Cover, Doji Star, Engulfing Bearish Line, Engulfing Bullish Line, Two Neutral Bars.
- **Gruppe 3 – Drei-Balken-Muster:** Double Inside, Pin Up, Pin Down, Pivot Point Reversal Up, Pivot Point Reversal Down, Close Price Reversal Up, Close Price Reversal Down, Evening Star, Morning Star, Evening Doji Star, Morning Doji Star.

Ein Toleranzparameter (Equality Pips) steuert, wie eng zwei Preise übereinstimmen müssen, um Gleichheitsprüfungen zu erfüllen, und reproduziert die "maximale Pip-Distanz"-Einstellung des Original-EA.

## Parameter
- **Candle Type** – Zeitrahmen, der für die Mustererkennung verwendet wird.
- **Opened Mode** – Positionsmanagement-Logik (Any, Swing, Buy One, Buy Many, Sell One, Sell Many), repliziert aus der MQL-Version.
- **Equality Pips** – Pip-Abstand, der Preisgleichheit definiert; angepasst durch den Preisschritt des Instruments.
- **Enable One-Bar Patterns / Enable Two-Bar Patterns / Enable Three-Bar Patterns** – Schalter für jede Mustergruppe.
- **Enable {Pattern}** – Individuelle Schalter für alle 30 Formationen.
- **{Pattern} Order** – Handelsrichtung (Kauf oder Verkauf), die dem entsprechenden Muster zugewiesen ist.

Alle Parameter werden durch `StrategyParam`-Objekte bereitgestellt, was Optimierung oder UI-Bindung bei der Verwendung innerhalb von StockSharp-Anwendungen ermöglicht.

## Handelslogik
1. Die Strategie abonniert die konfigurierte Kerzenserie und wartet auf abgeschlossene Kerzen.
2. Wenn eine neue Kerze schließt, werden die letzten drei Kerzen gecacht und der Detektor wertet die aktivierten Mustergruppen aus.
3. Jedes Muster repliziert die bedingten Regeln aus der MQL5-Quelle, einschließlich toleranter Vergleiche und Schatten/Körper-Beziehungen.
4. Wenn ein Muster bestätigt wird, prüft `TriggerPattern`, ob die Gruppe und das individuelle Muster aktiviert sind, überprüft die ausgewählte Richtung und wendet den konfigurierten Positionsmodus an.
5. Die Strategie sendet eine Marktorder in die zugewiesene Richtung. Im Swing-Modus wird zunächst jede offene Position in die entgegengesetzte Richtung geschlossen.

## Positionsmodi
- **Any:** Erlaubt Signale in beide Richtungen ohne zusätzliche Einschränkungen.
- **Swing:** Behält eine einzelne Nettoposition bei; entgegengesetzte Signale glätten die bestehende Position, bevor die neue eingegangen wird.
- **Buy One / Sell One:** Beschränken den Handel auf jeweils eine Long- oder Short-Position.
- **Buy Many / Sell Many:** Erlauben mehrere Einstiege in die angegebene Richtung, während Signale in die entgegengesetzte Richtung ignoriert werden.

## Hinweise
- Der Algorithmus verwendet `Security.PriceStep`, um die Gleichheitstoleranz in absoluten Preisabstand umzurechnen. Wenn das Instrument keinen Preisschritt definiert, wird ein Standardwert von 0.0001 angewendet.
- Keine zusätzlichen Indikatoren sind erforderlich; die gesamte Logik basiert ausschließlich auf der Kerzengeometrie, was der Absicht des originalen Expert Advisors entspricht.
