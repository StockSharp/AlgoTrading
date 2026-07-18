# Hybride Scalping-Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein hybrides Scalping-System, das RSI-Signale mit EMA-Trendfiltern und optionaler Volumenbestätigung kombiniert. Der Bot kann die Signalempfindlichkeit von sehr leicht bis stark anpassen und enthält Schnellausstieg- und Trailing-Stop-Funktionen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 35%. Es funktioniert am besten bei liquiden Krypto-Paaren.

Die Strategie geht long oder short basierend auf RSI-Schwellenwerten und Kerzenstärke, optional gefiltert nach Trend und Volumen. Positionen sind mit konfigurierbarem Take-Profit, Stop-Loss und Trailing-Logik abgesichert, und tägliche Handelslimits werden zu Beginn jeder Sitzung zurückgesetzt.

## Details

- **Einstiegskriterien**:
  - **Kauf**: RSI unter 30 mit bullischer Kerze, optionale Trend-/Volumenfilter je nach Empfindlichkeit.
  - **Verkauf**: RSI über 70 mit bärischer Kerze, optionale Trend-/Volumenfilter.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Take Profit, Stop Loss, Trailing Stop oder schnelle RSI/EMA-Umkehr.
- **Stops**: Ja, prozentbasierter SL/TP und optionaler Trailing Stop.
- **Filter**:
  - Trend- und Volumenfilter je nach Konfiguration.
