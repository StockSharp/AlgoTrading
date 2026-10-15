# Trailing-Verwaltungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine einzelne Long-Position und verwaltet sie anschließend mit mehreren Risikokontrollen:

- Prozentbasierter **Take Profit** und **Stop Loss**.
- **Trailing** des Gewinns, der nach einem konfigurierbaren Gewinn aktiviert wird.
- **Teilschließung** bei benutzerdefinierten Gewinnniveaus.

Der Algorithmus demonstriert, wie man eine bestehende Position in StockSharp ausschließlich mit Kerzendaten verwaltet.

## Details

- **Einstiegskriterien**: Marktkauf bei der ersten abgeschlossenen Kerze.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Take-Profit-Prozentsatz.
  - Stop-Loss-Prozentsatz.
  - Trailing-Gewinn-Auslöser.
  - Teilschließungsanteile.
- **Stops**: Ja, über Prozentsätze.
- **Filter**: Keine.
