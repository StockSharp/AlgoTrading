# Parabolic-SAR-Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader-Expert-Advisors "PSAR Trader EA". Sie beobachtet, wie der Preis mit dem Parabolic-SAR-Indikator interagiert, und reagiert nur, wenn das Punktfeld von einer Seite des Kerzenkörpers auf die andere wechselt. Die Konvertierung bewahrt die ursprüngliche Geldmanagement-Logik: Die Strategie kann entweder mit fester Lotgröße handeln oder das Ordervolumen dynamisch anhand des Kontostands anpassen, feste Stop-Loss- und Take-Profit-Niveaus anwenden und einen Trailing Stop aktivieren, sobald ein Trade ausreichend Gewinn aufgebaut hat.

## Strategielogik
- Einen Parabolic-SAR-Indikator mit benutzerdefinierter Beschleunigung und Maximalwerten auf der gewählten Kerzenserie aufbauen (standardmäßig 30-Minuten-Kerzen).
- Einen **bullischen Flip** erkennen, wenn der SAR-Punkt von oberhalb des Kerzenkörpers nach darunter wechselt. Ist keine Position offen, eine Markt-Kauforder senden. Besteht eine Short-Position, diese zuerst schließen und auf das nächste Signal für den Long-Wiedereinstieg warten.
- Einen **bärischen Flip** erkennen, wenn der SAR-Punkt von unterhalb des Kerzenkörpers nach darüber wechselt. Wenn flach, eine Short-Position eröffnen. Ist eine Long-Position aktiv, diese schließen und den Einstieg bis zum folgenden Signal aufschieben.
- Offene Trades auf jeder abgeschlossenen Kerze überwachen und Ausstiege ausführen, wenn ein Schutzniveau (Stop-Loss, Take-Profit oder Trailing Stop) vom Hoch/Tief der aktuellen Kerze erreicht wird.

## Risikomanagement
- **Stop loss:** in Punkten (Preisschritten) angegeben. Bei Long-Trades liegt der Stop unter dem Einstiegspreis, bei Shorts darüber.
- **Take profit:** ebenfalls in Punkten. Das Ziel spiegelt den Stop in Gegenrichtung und schließt die gesamte Position bei Erreichen.
- **Trailing stop:** startet, nachdem der Preis sich um eine konfigurierbare Anzahl Punkte zugunsten des Trades bewegt. Der Trailing Stop zieht nur in Gewinnrichtung enger und repliziert das "tighten stops only"-Verhalten des ursprünglichen EA.

## Volumenverwaltung
- **Festes Lot:** Wenn Auto-Lot deaktiviert ist, sendet die Strategie Orders mit der konfigurierten festen Lotgröße.
- **Kontostandsbasiertes Lot:** Wenn Auto-Lot aktiviert ist, wird das Volumen als `(Account Balance / 1000) * LotsPerThousand` berechnet und an Volumenschritt und Mindestvolumen der Security angepasst.

## Parameter und Standards
- `SarStep`: Beschleunigungsfaktor des Parabolic SAR. Standard: `0.02`.
- `SarMaximum`: Maximale Beschleunigung des Parabolic SAR. Standard: `0.2`.
- `CandleType`: Zeitrahmen für die Analyse. Standard: 30-Minuten-Kerzen.
- `UseAutoLot`: Dynamisches Lot-Sizing aktivieren. Standard: `false`.
- `FixedLot`: Volumen bei deaktiviertem Auto-Lot-Sizing. Standard: `0.1`.
- `LotsPerThousand`: Multiplikator für Auto-Lot-Berechnungen. Standard: `0.05`.
- `StopLossPoints`: Distanz zum Stop in Punkten. Standard: `500`.
- `TakeProfitPoints`: Distanz zum Take Profit in Punkten. Standard: `1000`.
- `TrailingStartPoints`: Gewinnschwelle zur Aktivierung des Trailings. Standard: `500`.
- `TrailingDistancePoints`: Trailing-Offset nach Aktivierung. Standard: `100`.

## Hinweise
- Die Strategie handelt Long- und Short-Richtungen, hält aber höchstens eine Position gleichzeitig offen.
- Schutzorders werden auf Kerzendaten simuliert; Intracandle-Spitzen, die kleiner als der gewählte Zeitrahmen sind, können im Live-Handel die Ausführungsqualität beeinflussen.
