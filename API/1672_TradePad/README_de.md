# TradePad Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die TradePad Strategie ist ein manuelles Handelspanel, das vom ursprünglichen MQL TradePad Expert portiert wurde. Die Strategie richtet ein Panel zur interaktiven Verwaltung von Trades ein. Sie verarbeitet Tick-Daten, Trade-Benachrichtigungen, Timer-Events und Chart-Nachrichten ohne automatisierte Einstiegs- oder Ausstiegsregeln.

Dieses Beispiel demonstriert, wie man eine diskretionäre Handelsoberfläche auf Basis von StockSharp aufbaut.

## Details

- **Einstiegskriterien**: Manuelle Orderplatzierung über das Panel.
- **Long/Short**: Beide, abhängig von der Benutzeraktion.
- **Ausstiegskriterien**: Manuelles Schließen der Position.
- **Stops**: Keine; der Benutzer kann eigene Logik implementieren.
- **Filter**: Keine automatischen Filter.
