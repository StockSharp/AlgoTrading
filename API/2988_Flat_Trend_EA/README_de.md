# Flat Trend EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Flat Trend EA Strategy ist ein StockSharp-Port des MQL5-Expert-Advisors "Flat Trend EA". Der Algorithmus kombiniert den Parabolic SAR-Indikator mit dem Average Directional Index (ADX), um vier Marktzustände zu erkennen: Aufwärtstrend, Abwärtstrend, Ende des Kaufs und Ende des Verkaufs. Die Strategie reagiert nur auf abgeschlossene Kerzen aus dem konfigurierten Zeitrahmen und spiegelt die ursprüngliche Logik wider, entgegengesetzte Positionen zu schließen, bevor eine neue eröffnet wird.

## Handelslogik
- **Kaufsignal**: der Parabolic SAR-Punkt druckt unter dem Schlusskurs und die ADX +DI-Linie liegt über der -DI-Linie. Jede Short-Exposition wird sofort geschlossen, und ein neues Long wird eröffnet, wenn keine Position aktiv ist.
- **Verkaufssignal**: der Parabolic SAR-Punkt druckt über dem Schlusskurs und +DI ist kleiner oder gleich -DI. Jede Long-Exposition wird geschlossen, bevor ein Short-Trade eröffnet wird.
- **Trendende-Filter**: wenn der SAR über dem Preis liegt, während +DI größer als -DI ist, markiert die Strategie das Ende eines Short-Trends; wenn SAR unter dem Preis liegt, während +DI kleiner oder gleich -DI ist, markiert es das Ende eines Long-Trends. Beide Ereignisse erzwingen das Schließen bestehender Positionen ohne Eröffnung eines neuen Trades.
- **Handelsfenster**: optionale Sitzungsfilter beschränken Einstiege auf das Intervall `[StartHour, EndHour)`. Signale außerhalb der Sitzung können Trades noch schließen, aber neue Einstiege werden übersprungen.

## Risikomanagement
- **Stop-Loss und Take-Profit**-Abstände werden in Pips gemessen (automatisch für drei- und fünfstellige Instrumente skaliert). Preise werden auf den Wertpapierschritt normalisiert.
- Der **Trailing Stop** aktiviert sich, nachdem die Position mehr als `TrailingStopPips + TrailingStepPips` gewinnt. Long-Positionen trailern unterhalb des letzten Schlusskurses, Shorts darüber. Wenn Trailing deaktiviert ist, bleibt das Stop-Niveau fest.
- **Schutzausstiege**: bei jeder abgeschlossenen Kerze prüft die Strategie Tief-/Hochpreise gegen Stop-Loss-, Take-Profit- und Trailing-Niveaus. Jede Verletzung schließt die Position und setzt die Risikoverfolgung zurück.

## Parameter
- `StopLossPips` – Abstand zum Schutz-Stop in Pips.
- `TakeProfitPips` – Abstand zum Ziel in Pips.
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (auf 0 setzen um Trailing zu deaktivieren).
- `TrailingStepPips` – zusätzlicher Fortschritt erforderlich bevor der Trailing Stop bewegt wird; muss positiv sein wenn Trailing aktiviert ist.
- `UseTradingHours` – aktiviert den Handelsfenster-Filter.
- `StartHour` / `EndHour` – inklusive Startstunde und exklusive Endstunde für Einstiege (Börsenzeit).
- `AdxPeriod` – Glättungsperiode für ADX, die die +DI- und -DI-Empfindlichkeit steuert.
- `SarStart`, `SarIncrement`, `SarMaximum` – Parabolic SAR-Beschleunigungseinstellungen entsprechend dem ursprünglichen Indikator (0.02 / 0.02 / 0.2 standardmäßig).
- `CandleType` – Zeitrahmen für Kerzen-Abonnements und Indikatorberechnungen.
- `Volume` – von `Strategy` geerbt, repräsentiert die Ordergröße beim Einstieg in neue Positionen.

## Indikatoren
- **Average Directional Index (ADX)** liefert die +DI- und -DI-Komponenten zur Bestimmung der aktuellen Trendrichtung.
- **Parabolic SAR** definiert ob die Marktstruktur bullisch oder bärisch ist und liefert das Punkt-Niveau für die Trailing-Logik.

## Weitere Hinweise
- Die Pip-Größe wird aus den Wertpapiereinstellungen berechnet: Für drei- und fünfstellige Instrumente wird der Preisschritt mit zehn multipliziert, um die MQL-Definition eines Pips zu entsprechen.
- Die Strategie schließt immer bestehende Positionen, wenn entgegengesetzte oder Endsignale erscheinen, bevor neue Einstiege ausgewertet werden, und reproduziert den ursprünglichen EA-Workflow.
- Nur die C#-Implementierung wird bereitgestellt; keine Python-Version oder -Ordner wird wie gewünscht erstellt.
