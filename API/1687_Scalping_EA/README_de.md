# Scalping-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein einfaches Scalping-System, das ständig zwei Pending Orders aufrecht erhält: ein Buy Stop oberhalb des Marktes und ein Sell Stop darunter. Wenn der Marktpreis einer Order zu nahe kommt oder sich zu weit entfernt, wird die Order ersetzt, um einen festen Abstand vom aktuellen Preis zu wahren. Ausgeführte Orders verwenden feste Take-Profit- und Stop-Loss-Abstände.

Die Strategie stützt sich nicht auf Indikatoren und reagiert ausschließlich auf Tick-Preisänderungen.

## Details

- **Einstiegskriterien**:
  - Buy Stop 100 Punkte über dem Preis und Sell Stop 100 Punkte darunter platzieren.
  - Orders werden ersetzt, wenn der Abstand zum Preis zu klein oder zu groß wird.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Jede Order trägt festen Take Profit und Stop Loss.
- **Stops**: Ja, fester Abstand.
- **Filter**: Keine.
