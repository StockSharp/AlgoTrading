# Martingale Bone Crusher Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Martingale Bone Crusher Strategie** repliziert das Verhalten des ursprünglichen MetaTrader Expert Advisors. Die Strategie handelt in Richtung eines Schnell/Langsam-Vergleichs gleitender Durchschnitte und wendet ein Martingale-Geldmanagement-Modell an, das die Ordergröße nach einem Verlusthandel erhöht. Eine große Auswahl an Risikomanagement-Tools ist verfügbar, darunter feste Geldziele, Prozentsatzziele, ein konfigurierbarer Breakeven-Zug, klassische Stop-Loss/Take-Profit-Niveaus gemessen in Preisschritten und ein gewinnschützender Trailing Stop gemessen in Geld.

## Handelslogik

- **Signalerzeugung** – zwei einfache gleitende Durchschnitte werden auf der primären Kerzenreihe berechnet. Wenn der schnelle Durchschnitt unter dem langsamen liegt, sucht die Strategie nach Long-Einstiegen. Wenn er darüber liegt, sucht sie nach Short-Einstiegen. Neue Trades werden nicht platziert, während eine aktive Position besteht.
- **Martingale-Sequenzierung** – nach jedem abgeschlossenen Trade wird die nächste Positionsgröße aktualisiert. Wenn der letzte Trade mit einem Verlust schloss, wird das nächste Volumen entweder multipliziert oder erhöht (je nach Einstellungen). Gewinnende Trades setzen die Positionsgröße auf den Anfangswert zurück.
- **Modusauswahl** – zwei Martingale-Varianten werden bereitgestellt:
  - `Martingale1`: Der nächste Trade folgt immer der aktuellen Richtung des gleitenden Durchschnitts, auch nach einem Verlust.
  - `Martingale2`: Nach einem Verlust wird der nächste Trade relativ zur verlierenden Richtung umgekehrt. Dies spiegelt das Verhalten der zweiten Option des ursprünglichen Expert Advisors wider.
- **Risikokontrollen** – während eine Position offen ist, wertet die Strategie kontinuierlich aus:
  - klassische Stop-Loss- und Take-Profit-Niveaus ausgedrückt in Preisschritten;
  - einen optionalen Trailing Stop, der dem Extrempreis mit einem festen Schrittabstand folgt;
  - einen Breakeven-Zug, der das Ausstiegsniveau verschiebt, nachdem sich die Position um eine konfigurierbare Distanz zu Gunsten bewegt;
  - globale geld- und prozentbasierte Gewinnziele, die die Position schließen, wenn der aggregierte schwebende PnL die Schwellenwerte überschreitet;
  - einen zusätzlichen Trailing Stop in Geld, der angesammelten Gewinn sichert, sobald der schwebende Gewinn das Aktivierungsniveau erreicht.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `UseTakeProfitMoney` | Aktiviert ein festes Geld-Take-Profit-Ziel. |
| `TakeProfitMoney` | Geldbetrag, der den Trade schließt, wenn `UseTakeProfitMoney` aktiv ist. |
| `UseTakeProfitPercent` | Aktiviert ein Gewinnziel ausgedrückt als Prozentsatz des anfänglichen Portfoliowerts. |
| `TakeProfitPercent` | Prozentsatz, der verwendet wird, wenn `UseTakeProfitPercent` aktiviert ist. |
| `EnableTrailing` | Aktiviert den geldbasierten Trailing Stop. |
| `TrailingTakeProfitMoney` | Schwebender Gewinn, der erforderlich ist, um den Geld-Trailing-Stop zu aktivieren. |
| `TrailingStopMoney` | Erlaubter Drawdown vom Spitzen-Schwebgewinn, nachdem der Trailing Stop aktiv ist. |
| `MartingaleModes` | Wählt zwischen `Martingale1` und `Martingale2` Verhalten. |
| `UseMoveToBreakeven` | Aktiviert die Breakeven-Stop-Anpassung. |
| `MoveToBreakevenTrigger` | Preisschritte, um die sich der Trade zu Gunsten bewegen muss, bevor der Breakeven-Schutz aktiviert wird. |
| `BreakevenOffset` | Abstand, der zum Einstiegspreis hinzugefügt wird, wenn der Breakeven-Stop platziert wird. |
| `Multiply` | Multiplikator, der auf das nächste Volumen nach einem Verlust angewendet wird, wenn `DoubleLotSize` `true` ist. |
| `InitialVolume` | Basis-Ordervolumen für den ersten Trade und nach Gewinnen. |
| `DoubleLotSize` | Wechselt zwischen multiplikativem (`true`) und additivem (`false`) Martingale-Sizing. |
| `LotSizeIncrement` | Volumenerhöhung nach einem Verlust, wenn `DoubleLotSize` `false` ist. |
| `TrailingStopSteps` | Trailing-Stop-Abstand in Preisschritten. |
| `StopLossSteps` | Klassischer Stop-Loss-Abstand in Preisschritten. |
| `TakeProfitSteps` | Klassischer Take-Profit-Abstand in Preisschritten. |
| `FastPeriod` | Periode des schnellen einfachen gleitenden Durchschnitts. |
| `SlowPeriod` | Periode des langsamen einfachen gleitenden Durchschnitts. |
| `CandleType` | Kerzenreihe, die für alle Indikatorberechnungen verwendet wird. |

## Hinweise

- Das Positionsvolumen wird am Volumen-Step des Instruments sowie den minimalen und maximalen Limits ausgerichtet.
- Die Berechnungen des schwebenden Gewinns hängen vom `PriceStep` und `StepPrice` des Instruments ab. Wenn sie null sind, werden die geldbasierten Schutzmaßnahmen automatisch übersprungen.
- Nur die C#-Implementierung wird bereitgestellt. Der Python-Port wird gemäß den Aufgabenanforderungen absichtlich weggelassen.
