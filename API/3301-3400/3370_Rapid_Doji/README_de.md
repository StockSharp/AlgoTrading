# Schnelle Doji-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Rapid Doji-Strategie repliziert die Logik des ursprünglichen Expertenberaters „Rapid Doji EA“. Es scannt fertige Kerzen eines konfigurierbaren Zeitrahmens (standardmäßig täglich) und platziert Stop-Entry-Orders über und unter jeder Doji-Kerze. Schutzstopps werden mithilfe eines Average True Range (ATR)-Multiplikators positioniert, während ein zusätzlicher Trailing-Stop die Risikodistanz in Rohpunkten festhält, nachdem eine Position profitabel wird.

## Handelslogik

1. **Datenabonnement** – die Strategie hört auf fertige Kerzen des ausgewählten Zeitrahmens und verwaltet einen ATR-Indikator mit einem konfigurierbaren Zeitraum.
2. **Doji-Erkennung** – eine Kerze wird als Doji behandelt, wenn die absolute Körpergröße höchstens 3 % des gesamten Kerzenbereichs beträgt. Es werden nur fertige Kerzen ausgewertet.
3. **Auftragserteilung** – wenn ein gültiges Doji gefunden wird:
   - Beim Doji-Hoch wird eine Kauf-Stopp-Order platziert.
   - Beim Doji-Tief wird eine Verkaufsstopp-Order platziert.
   - Jeder Eintrag speichert einen schützenden Stop-Preis, der dem entgegengesetzten extremen Minus/Plus-Multiplikator ATR entspricht.
4. **Risikomanagement** – sobald eine Position eröffnet wird, wird die verbleibende ausstehende Order storniert, der gespeicherte Stop wird als Schutzstopp registriert und die Trailing-Logik übernimmt die Kontrolle.
5. **Trailing Stop** – bei jeder neuen Kerze wird das Stop-Level verschoben, um einen festen Abstand (in durch die Preisstufe des Instruments umgerechneten Punkten) zum letzten Schlusskurs einzuhalten, jedoch nur, wenn die Position bereits profitabel ist.

Die Strategie verwendet niemals Take-Profit-Ziele; Ausstiege erfolgen durch den Schutzstopp oder manuelle Eingriffe.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzendatentyp, der zur Mustererkennung verwendet wird (standardmäßig täglicher Zeitrahmen). |
| `AtrPeriod` | Lookback-Länge des ATR-Indikators. |
| `AtrMultiplier` | Auf den ATR-Wert angewendeter Multiplikator zur Stop-Loss-Berechnung. |
| `TrailingDistancePoints` | Feste Distanz in Rohpunkten, die beim Verfolgen des Stopps verwendet wird. |

Alle Parameter unterstützen die Optimierung innerhalb der StockSharp-Umgebung.

## Implementierungshinweise

- Der Code basiert auf dem High-Level-Kerzenabonnement API (`SubscribeCandles`) in Kombination mit der Indikatorbindung (`Bind`), um eine manuelle Verlaufsverarbeitung zu vermeiden.
- Aufträge werden bis `Security.ShrinkPrice` normalisiert, um die Tick-Größe der Börse zu berücksichtigen.
- Schutzstopps werden explizit verwaltet, um das Verhalten des ursprünglichen MetaTrader-Expertenberaters nachzuahmen.
- Das Projekt verzichtet bewusst auf eine Python-Implementierung gemäß den Aufgabenanforderungen.
