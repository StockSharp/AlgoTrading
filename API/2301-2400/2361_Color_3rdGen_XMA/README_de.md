# Color 3rdGen XMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt basierend auf der Richtung eines gleitenden Durchschnitts der dritten Generation. Der Indikator ist eine Kombination aus zwei exponentiellen gleitenden Durchschnitten und wird blau, wenn er steigt, und rosa, wenn er fällt. Ein Kaufsignal wird aufgezeichnet, wenn der Durchschnitt nach oben dreht, und ein Verkaufssignal, wenn er nach unten dreht.

Aufträge werden nur zu einer benutzerdefinierten Zeit nach dem Erscheinen eines Signals platziert. Positionen können auch geschlossen werden, wenn das entgegengesetzte Signal erkannt wird oder eine vordefinierte Haltezeit abläuft. Optionale Stop-Loss- und Take-Profit-Level werden in Punkten gemessen.

## Parameter

- **Length** – Glättungsperiode des Durchschnitts der dritten Generation.
- **StartHour** – Stunde, zu der neue Positionen eröffnet werden können.
- **StartMinute** – Minute innerhalb der Stunde, wenn Eröffnungen erlaubt sind.
- **HoldMinutes** – maximale Zeit zum Halten einer offenen Position.
- **Volume** – Auftragsvolumen für Einstiege.
- **StopLoss** – Stop-Loss-Abstand in Punkten. `0` deaktiviert den Stop.
- **TakeProfit** – Take-Profit-Abstand in Punkten. `0` deaktiviert das Ziel.
- **UseLongEntries** – Long-Einstiege aktivieren.
- **UseShortEntries** – Short-Einstiege aktivieren.
- **CloseLongBySignal** – Long-Positionen schließen, wenn ein Verkaufssignal erscheint.
- **CloseShortBySignal** – Short-Positionen schließen, wenn ein Kaufsignal erscheint.
- **CandleType** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Logik

1. Kerzen des ausgewählten Zeitrahmens abonnieren.
2. Den gleitenden Durchschnitt der dritten Generation für jede Kerze berechnen.
3. Erkennen, wenn der Durchschnitt zwischen aufeinanderfolgenden Kerzen steigt oder fällt.
4. Ein Kauf- oder Verkaufssignal basierend auf der Richtungsänderung speichern.
5. Zur angegebenen Öffnungszeit in Richtung des gespeicherten Signals einsteigen.
6. Positionen bei entgegengesetzten Signalen, wenn die Haltezeit abläuft oder wenn Stop-Loss/Take-Profit-Level erreicht werden, schließen.
