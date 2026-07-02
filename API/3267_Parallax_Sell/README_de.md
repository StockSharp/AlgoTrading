# Parallax Sell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Parallax Sell ist eine reine Short-Martingale-Strategie, konvertiert vom MetaTrader-Expertenberater `parallax_sell`. Der ursprüngliche Roboter handelte JPY-Crosses (CAD/JPY und CHF/JPY) und verlässt sich auf eine Konfluenz von Williams %R-, MACD- und Stochastik-Oszillator-Filtern, um Shorts in überkaufte Rallyes zu initiieren. Positionsausstiege hängen von Momentum-Verblassungszeichen ab, die von Williams %R oder einem langsamen Stochastik geliefert werden, während ein Martingale-ähnliches Positionsgrößen-Schema die Exposition nach Verlustsequenzen erhöht.

## Einstiegslogik
- Auf dem konfigurierbaren Zeitrahmen arbeiten (Standard: 1-Stunden-Kerzen).
- Auf einen frischen Kerzenschluss warten.
- Erfordern, dass Williams %R (Einstiegsrückblick 350) über dem überkauften Schwellenwert (Standard -10) liegt.
- Erfordern, dass die MACD-Hauptlinie (12/120/9-Einstellungen) über einem bullischen Schwellenwert bleibt (Standard 0.178), um aufwärts gerichtetes Momentum zu bestätigen, bevor es gefadet wird.
- Einen Abwärtskreuz des schnellen Stochastik %K (Länge 10, Verlangsamung 3) unter das Einstiegsauslöseniveau (Standard 90) erkennen. Nur dieses Kreuzereignis kann einen neuen Short erzeugen.
- Jedes qualifizierte Signal sendet eine zusätzliche Marktverkaufsorder. Mehrere Short-Orders können sich stapeln und der Martingale-Volumenlogik folgen.

## Ausstiegslogik
- Den schwebenden Gewinn aller offenen Shorts in Pips mit der Instrument-Pip-Größe verfolgen.
- Wenn nur ein Short offen ist und der durchschnittliche Gewinn das Einzelhandels-Ziel übersteigt (Standard 10 Pips) **und** Williams %R unter den Ausstiegsschwellenwert fällt (Standard -80), die Position schließen.
- Wenn mehr als ein Short offen ist und der durchschnittliche Korbgewinn das Korbziel übersteigt (Standard 15 Pips) **und** der langsame Stochastik %K (Länge 90, Verlangsamung 1) unter den überverkauften Auslöser fällt (Standard 12), den gesamten Korb schließen.
- Ein zusätzlicher Sicherheits-Take-Profit schließt den Korb, wenn der durchschnittliche Gewinn die konfigurierte Take-Profit-Distanz erreicht (Standard 100 Pips).

## Positionsgrößenbestimmung
- Mit dem Basisvolumen beginnen (Standard 0.01 Lots).
- Nach einem profitablen Zyklus (realisierter PnL-Anstieg), das nächste Ordervolumen auf das Basisvolumen zurücksetzen.
- Nach einem Verlustzyklus (realisierter PnL-Rückgang), das nächste Ordervolumen mit dem Martingale-Multiplikator multiplizieren (Standard 1.6). Volumen werden automatisch am Instrument-Volumenschritt ausgerichtet.

## Risikomanagement
- Die Strategie registriert eine Schutz-Take-Profit-Order mit der konfigurierten Pip-Distanz. Kein fester Stop-Loss wird verwendet; Ausstiege werden durch Indikatorfilter gesteuert.
- Start-Schutz wird einmal aktiviert, wie von den StockSharp-Konvertierungsrichtlinien gefordert.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen für Berechnungen. | 1H-Kerzen |
| `EntryWilliamsLength` | Williams %R-Rückblick für Einstiege. | 350 |
| `ExitWilliamsLength` | Williams %R-Rückblick für Ausstiege. | 350 |
| `EntryStochasticLength` / `Signal` / `Slowing` | Schnelle Stochastik-Einstellungen für den Einstiegskreuz. | 10 / 1 / 3 |
| `ExitStochasticLength` / `Signal` / `Slowing` | Langsame Stochastik-Einstellungen für Ausstiegsbestätigung. | 90 / 7 / 1 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD-Parameter. | 12 / 120 / 9 |
| `EntryWilliamsThreshold` | Mindestwert von Williams %R vor dem Shorting. | -10 |
| `ExitWilliamsThreshold` | Williams %R-Niveau, das den Ausstieg für einen einzelnen Trade bestätigt. | -80 |
| `EntryStochasticTrigger` | Niveau, das der schnelle Stochastik nach unten kreuzen muss, um Einstiege auszulösen. | 90 |
| `ExitStochasticTrigger` | Niveau, unter das der langsame Stochastik fallen muss, um Körbe zu schließen. | 12 |
| `MacdThreshold` | Mindestwert der MACD-Hauptlinie. | 0.178 |
| `SingleTradeTargetPips` | Gewinnziel (Pips), wenn nur ein Short aktiv ist. | 10 |
| `MultiTradeTargetPips` | Gewinnziel (Pips), wenn mehrere Shorts aktiv sind. | 15 |
| `TakeProfitPips` | Harte Take-Profit-Distanz (Pips). | 100 |
| `InitialVolume` | Basis-Ordergröße. | 0.01 |
| `MartingaleMultiplier` | Multiplikator nach einem Verlust bei aktiviertem Martingale. | 1.6 |
| `UseMartingale` | Martingale-Eskalation aktivieren oder deaktivieren. | true |

## Hinweise
- Die Strategie handelt nur Short-Positionen und geht von Forex-ähnlichen Pip-Konventionen bei der Gewinnmessung aus.
- Durchschnittliche Gewinnberechnungen behandeln jeden Einstieg gleich, was dem MetaTrader-Block entspricht, der Pips pro Trade mittelte.
- Schwellenwerte anpassen oder Martingale deaktivieren (`UseMartingale = false`), um das Risiko bei hochvolatilen Paaren zu reduzieren.
