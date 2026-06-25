# MACD EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader 5 Expert Advisors `MACD EA (barabashkakvn's edition).mq5` aus dem Ordner `MQL/20010`. Sie recreiert dieselbe MACD-Crossover-Logik, partielle Gewinnmitnahme und Money-Management-Funktionen unter Verwendung der High-Level StockSharp API.

## Handelslogik

* **Signalquelle** – Ein klassischer MACD-Indikator wird mit konfigurierbaren schnellen, langsamen und Signal-Perioden berechnet. Die Strategie untersucht die Differenz zwischen der MACD-Linie und der Signallinie zwei und vier abgeschlossene Kerzen zurück. Ein bullischer Crossover (Differenz wechselt von negativ zu positiv) öffnet einen Long-Trade, während die entgegengesetzte Bedingung einen Short-Trade öffnet.
* **Positionsmanagement** – Jede Order ist durch konfigurierbare Stop-Loss- und Take-Profit-Abstände in Pips geschützt. Die Abstände werden mithilfe des Preisschritts des Instruments in Preise umgerechnet und bei 3 oder 5 Dezimalstellen mit zehn multipliziert, was die Punkt-Anpassung des originalen EA nachahmt.
* **Teilgewinn** – Wenn aktiviert, wird die Hälfte der offenen Position geschlossen, sobald sich der Preis um `PartialProfitPips` in Handelsrichtung bewegt hat. Der verbleibende Teil läuft weiter.
* **Breakeven** – Nachdem sich der Preis um `BreakevenPips` zu Gunsten bewegt hat, aktiviert die Strategie einen Breakeven-Schutz. Wenn der Preis auf das ursprüngliche Einstiegsniveau zurückkehrt, wird die Position zum Einstiegspreis geschlossen, genau wie der EA den Stop auf Breakeven setzt.
* **Entgegengesetztes MACD-Signal** – Ein entgegengesetzter MACD-Crossover schließt sofort jedes verbleibende Engagement und stellt sicher, dass die Strategie niemals eine Position gegen den Indikatortrend hält.

## Money Management

Wenn `UseMoneyManagement` aktiviert ist, erhöht sich die Positionsgröße nach aufeinanderfolgenden Verlustgeschäften. Der nächste Trade verwendet einen Multiplikator basierend auf der Anzahl aufeinanderfolgender Verluste (x2 nach einem Verlust, x3 nach zwei Verlusten, bis x7 bei sechs oder mehr Verlusten). Der Multiplikator wird mit dem Parameter `RiskMultiplier` kombiniert, um das Martingal-artige Sizing des ursprünglichen Codes zu reproduzieren. Gewinnende Trades setzen den Verlustzähler auf null zurück.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | MACD-Berechnungslängen.
| `StopLossPips` | Abstand zum Schutz-Stop in Pips (0 deaktiviert ihn).
| `TakeProfitPips` | Abstand zum Gewinnziel in Pips (0 deaktiviert ihn).
| `PartialProfitPips` | Pips, die benötigt werden, um die Hälfte der Position zu schließen (0 deaktiviert den Teilausstieg).
| `BreakevenPips` | Pips, die erforderlich sind, bevor der Breakeven-Modus aktiviert wird (0 deaktiviert Breakeven).
| `UseMoneyManagement` | Aktiviert dynamisches Positionssizing basierend auf der Verlustserie.
| `RiskMultiplier` | Zusätzlicher Multiplikator, der angewendet wird, wenn Money Management aktiv ist.
| `BaseVolume` | Basis-Handelsvolumen vor jeder Skalierung.
| `CandleType` | Kerzenserie, die für Indikatorberechnungen verwendet wird.

## Hinweise

* Die Strategie verwendet `SubscribeCandles` und Indikatorbindung, um dem empfohlenen High-Level-API-Muster zu folgen.
* Eine Python-Version ist noch nicht verfügbar. Nur die C#-Implementierung im Ordner `CS` wird bereitgestellt.
* Tests wurden nicht hinzugefügt oder geändert, wie angefordert.
