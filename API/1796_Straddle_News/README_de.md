# Straddle News-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie für hochvolatile Nachrichtenveröffentlichungen. Sie platziert symmetrische Stop-Orders auf beiden Seiten des aktuellen Preises, um Ausbrüche zu erfassen. Sobald eine Order ausgeführt wird, wird die entgegengesetzte ausstehende Order storniert und ein Trailing-Stop schützt die offene Position.

## Details

- **Einstiegskriterien**: warten bis der Spread unter `SpreadOperation` fällt, dann Buy-Stop bei Ask + `PipsAway` Punkten und Sell-Stop bei Bid - `PipsAway` Punkten platzieren
- **Long/Short**: Beide
- **Ausstiegskriterien**: schützender Stop-Loss oder Take-Profit, oder Trailing-Stop wenn der Preis um `TrailingStop` Punkte zurückgeht
- **Stops**: Initialer Stop-Loss und Take-Profit über `StartProtection`; benutzerdefinierter Trailing-Stop im Code
- **Standardwerte**:
  - `StopLoss` = 100
  - `TakeProfit` = 300
  - `TrailingStop` = 50
  - `PipsAway` = 50
  - `BalanceUsed` = 0.01
  - `SpreadOperation` = 25
  - `Leverage` = 400
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Level1 / Tick
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch

## Funktionsweise

1. Level1-Kurse abonnieren, um aktuelle Bid- und Ask-Preise zu erhalten.
2. Wenn der Spread klein genug ist, Volumen aus Portfoliowert, Hebel und `BalanceUsed` berechnen.
3. Ausstehende Buy- und Sell-Stop-Orders mit durch `PipsAway` definierten Abständen platzieren.
4. Wenn eine Position eröffnet wird, die entgegengesetzte ausstehende Order stornieren.
5. Stop-Loss- und Take-Profit-Orders basierend auf `StopLoss` und `TakeProfit` anhängen.
6. Höchsten/niedrigsten Preis seit Einstieg verfolgen und aussteigen, wenn der Preis mehr als `TrailingStop` Punkte zurückgeht.
