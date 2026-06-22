# Karacatica-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Karacatica-Strategie ist ein trendfolgender Ansatz, der Preisaktionen mit dem Average Directional Index (ADX) kombiniert. Sie sucht nach Situationen, in denen der aktuelle Schlusskurs höher oder niedriger als der Schlusskurs vor einer bestimmten Anzahl von Kerzen ist, und bestätigt die Bewegung mit der Dominanz der +DI- oder -DI-Linie.

## Indikatoren
- **Average Directional Index (ADX)** – misst die Trendstärke und liefert die +DI- und -DI-Komponenten.
- **Preisvergleich** – prüft, ob der letzte Schluss über oder unter dem Schluss von *Period* Kerzen zurück liegt.

## Parameter
- `Period` – Anzahl der Kerzen für die ADX-Berechnung und den Rückblick für den Preisvergleich. Standard ist 70.
- `TakeProfitPercent` – Take-Profit ausgedrückt als Prozentsatz des Einstiegspreises. Standard ist 2%.
- `StopLossPercent` – Stop-Loss ausgedrückt als Prozentsatz des Einstiegspreises. Standard ist 1%.
- `CandleType` – Zeitrahmen der zu abonnierenden Kerzen. Standard ist 1 Stunde.

## Handelslogik
- **Long-Einstieg**: `Close > Close[Period]` und `+DI > -DI` ohne vorhandenes Long-Signal. Schließt Short-Positionen und eröffnet eine Long-Position.
- **Short-Einstieg**: `Close < Close[Period]` und `-DI > +DI` ohne vorhandenes Short-Signal. Schließt Long-Positionen und eröffnet eine Short-Position.
- **Positionsschutz**: `StartProtection` wendet sowohl Take-Profit- als auch Stop-Loss-Prozentsätze an.

## Verwendungshinweise
- Entwickelt für die High-Level-API von StockSharp; abonniert Kerzen und bindet den ADX-Indikator.
- Die Strategie schließt automatisch entgegengesetzte Positionen, wenn ein neues Signal erscheint.
- Derzeit ist keine Python-Implementierung vorgesehen.

## Haftungsausschluss
Dieses Beispiel dient nur zu Bildungszwecken und garantiert keine Gewinne. Der Handel ist mit erheblichem Risiko verbunden. Testen Sie Strategien immer gründlich, bevor Sie sie auf Live-Märkten einsetzen.
