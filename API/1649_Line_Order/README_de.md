# Linien-Order-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Linien-Order-Strategie** löst eine Marktorder aus, wenn der Preis eine benutzerdefinierte horizontale Linie kreuzt. Sie ist als vereinfachte Umsetzung des ursprünglichen MQL-Skripts *LineOrder.mq4* konzipiert und stellt manuelle Linienhandel-Funktionalität über die High-Level-API von StockSharp bereit.

Die Strategie bietet Parameter zur Steuerung von Richtung, Einstiegsniveau und Risikomanagement. Nach dem Einstieg in eine Position werden optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus bei jeder abgeschlossenen Kerze überwacht. Die Logik ist vollständig ereignisgesteuert und pflegt keine eigenen Sammlungen.

## Parameter
- **LinePrice** – Preisniveau für die Orderplatzierung.
- **IsBuy** – `true` für Long-Einstiege, `false` für Short-Einstiege.
- **StopLoss** – Stop-Loss-Abstand in Preiseinheiten (0 deaktiviert).
- **TakeProfit** – Take-Profit-Abstand in Preiseinheiten (0 deaktiviert).
- **TrailingStop** – Trailing-Stop-Abstand in Preiseinheiten (0 deaktiviert).
- **Volume** – Ordervolumen.
- **CandleType** – Kerzentyp zur Preisüberwachung.

## Handelsregeln
- **Einstieg**: wenn der Schlusskurs `LinePrice` in der gewählten Richtung kreuzt.
- **Stop-Loss**: schließt die Position, wenn der Verlust den `StopLoss`-Abstand vom Einstieg überschreitet.
- **Take-Profit**: schließt die Position, wenn der Gewinn die `TakeProfit`-Distanz erreicht.
- **Trailing Stop**: nach dem Einstieg passt sich dieser dem günstigsten Preis an und schließt, wenn sich der Preis um `TrailingStop` gegen die Position bewegt.

## Hinweise
- Funktioniert mit jedem von StockSharp unterstützten Wertpapier.
- Für Bildungszwecke konzipiert, um die Übertragung von manuellem Linienhandel aus MQL zu veranschaulichen.
- Die Python-Version ist absichtlich weggelassen.
