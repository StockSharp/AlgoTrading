# News-Trading EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zeitbasierte Straddle-Strategie, die für den Handel rund um wirtschaftliche Nachrichtenveröffentlichungen entwickelt wurde. Zu einem geplanten Zeitpunkt platziert die Strategie symmetrische Buy-Stop- und Sell-Stop-Orders in einem festen Abstand vom aktuellen Preis. Orders werden in jedem Kerzenzeitraum während des Aktivierungsfensters aktualisiert, um dem Marktpreis zu folgen. Wenn eine Position eröffnet wird, wird die entgegengesetzte ausstehende Order storniert und optionale Take-Profit- und Stop-Loss-Niveaus steuern die Ausstiege.

## Details

- **Einstiegskriterien**:
  - Während des Straddle-Fensters Buy-Stop bei close + Distance * step und Sell-Stop bei close - Distance * step platzieren.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenläufiger Stop, Take-Profit/Stop-Loss oder Order-Ablauf
- **Stops**: Fester Stop-Loss und Take-Profit
- **Standardwerte**:
  - `StartDateTime` = DateTime.Now
  - `StartStraddle` = 0
  - `StopStraddle` = 15
  - `Volume` = 0.01m
  - `Distance` = 55m
  - `TakeProfit` = 30m
  - `StopLoss` = 30m
  - `Expiration` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: News
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Ereignis
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
