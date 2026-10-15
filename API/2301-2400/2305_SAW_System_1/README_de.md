# SAW System 1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Ausbruch-Strategie platziert Stop-Orders zu Beginn jedes Handelstages. Sie misst die durchschnittliche Tagesspanne über eine konfigurierbare Anzahl von Tagen und verwendet diesen Wert, um Stop-Loss- und Take-Profit-Niveaus abzuleiten. Orders werden auf beiden Seiten des aktuellen Preises positioniert, und es wird erwartet, dass nur eine Seite ausgelöst wird.

Zur angegebenen `OpenHour` berechnet die Strategie Kauf- und Verkauf-Stop-Preise auf halber Stop-Loss-Distanz vom aktuellen Marktpreis. Stop-Loss- und Take-Profit-Niveaus werden als Prozentsätze der durchschnittlichen Spanne definiert. Wenn eine Stop-Order gefüllt wird, kann die entgegengesetzte Order entweder storniert oder für eine Positionsumkehr beibehalten werden. Eine optionale Martingal-Funktion multipliziert das Volumen der verbleibenden Order nach einer Ausführung.

Ausstehende Einstiegsorders, die bis `CloseHour` nicht ausgeführt werden, werden entfernt, um Übernacht-Exposition zu vermeiden. Nach einem Einstieg platziert die Strategie sofort protective Stop-Loss- und Take-Profit-Orders relativ zum Ausführungspreis.

## Details

- **Einstiegskriterien:**
  - Durchschnittliche Tagesspanne mit ATR über `VolatilityDays` Tage berechnen.
  - Stop-Loss- und Take-Profit-Abstände als `StopLossRate` und `TakeProfitRate` Prozent dieser Spanne berechnen.
  - Zur `OpenHour` Kauf- und Verkauf-Stop-Orders mit `offset = stopLoss/2` vom Marktpreis platzieren.
- **Ausstiegskriterien:**
  - Protective Stop-Loss- und Take-Profit-Orders schließen Positionen.
  - Ausstehende Einstiegsorders werden zur `CloseHour` storniert.
- **Umkehrmodus:**
  - Wenn `Reverse` wahr ist, bleibt die entgegengesetzte Stop-Order, um die Position umzukehren.
  - Wenn `UseMartingale` ebenfalls wahr ist, wird die verbleibende Order mit dem mit `MartingaleMultiplier` multiplizierten Volumen neu registriert.
- **Long/Short:** Beide Richtungen.
- **Stops:** Fester Stop-Loss und Take-Profit basierend auf der Tagesspanne.
- **Standardwerte:**
  - `VolatilityDays` = 5
  - `OpenHour` = 7
  - `CloseHour` = 10
  - `StopLossRate` = 15%
  - `TakeProfitRate` = 30%
  - `Reverse` = false
  - `UseMartingale` = false
  - `MartingaleMultiplier` = 2.0

Dieser Ansatz versucht, Ausbrüche nach ruhigen Übernacht-Sessions zu erfassen und begrenzt das Risiko durch volatilitätsangepasste Ziele.
