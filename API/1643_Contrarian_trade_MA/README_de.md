# Contrarian-Handels-MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein wöchentliches Contrarian-System, das frühere Hochs, Tiefs und einen gleitenden Durchschnitt auswertet, um am Ende jeder Woche Trades zu eröffnen. Die Position wird unabhängig von der Richtung eine Woche lang gehalten.

Die Methode ist für die wichtigsten Währungspaare konzipiert, kann aber auf jeden liquiden Vermögenswert mit Wochendaten angewendet werden.

## Details

- **Einstiegskriterien**:
  - **Kauf**: Vorwochenschluss über dem höchsten Hoch des Analysezeitraums oder der gleitende Durchschnitt über der Wocheneröffnung.
  - **Verkauf**: Vorwochenschluss unter dem niedrigsten Tief des Analysezeitraums oder der gleitende Durchschnitt unter der Wocheneröffnung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Position wird nach einer Haltedauer von einer Woche geschlossen.
- **Stops**: Keine.
- **Zeitrahmen**: Wochenkerzen.
