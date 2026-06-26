# Hedger Drawdown-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

StockSharp-Port des MetaTrader 5 Expert Advisors **hedger.mq5** (MQL #23511). Das ursprüngliche System eröffnet eine schützende Absicherung in der entgegengesetzten Richtung, wenn eine bestehende Position um eine bestimmte Anzahl von Pips in den Drawdown gerät. Sobald der Preis um einen kleineren Betrag zurückgeht, wird die Absicherung auch mit Verlust geschlossen, wodurch der ursprüngliche Trade sich erholen kann. Diese Konvertierung reproduziert das Verhalten mit der High-Level-API von StockSharp und passt die Mechanik an das Netto-Positionsmodell der Plattform an.

## Trading-Logik

1. Die Strategie überwacht den Abschluss jeder Kerze des konfigurierten Zeitrahmens.
2. Für jede Long-Position, die keine Absicherung ist, prüft sie, ob die Distanz zwischen dem Einstiegspreis und dem aktuellen Schlusskurs größer oder gleich **DrawdownOpenPips** ist. Wenn keine Short-Absicherung aktiv ist, öffnet sie eine mit demselben Volumen.
3. Für jede Short-Position, die keine Absicherung ist, wendet sie die symmetrische Regel an und öffnet eine Long-Absicherung, nachdem der Verlust den Öffnungsschwellenwert erreicht.
4. Aktive Absicherungspositionen werden geschlossen, wenn ihr schwebender Verlust **DrawdownClosePips** erreicht, was der MetaTrader-Logik der Freigabe des Schutzes nach einer teilweisen Erholung entspricht.
5. Wenn das Konto keine offenen Positionen hat und **StartWithLong** aktiviert ist, öffnet der Algorithmus eine initiale Long-Position, um den Zyklus zu starten.

Da StockSharp Nettopositionen verfolgt, führt die Strategie interne Bücher über Long- und Short-Einträge (einschließlich derer, die Absicherungen sind). Jede Marktorder aktualisiert die Bücher, sodass Absicherungen unabhängig geöffnet und geschlossen werden können, selbst wenn der Broker Positionen zusammenfasst.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `DrawdownOpenPips` | Drawdown in Pips, der das Öffnen der entgegengesetzten Absicherung auslöst. |
| `DrawdownClosePips` | Drawdown in Pips, der das Schließen der Absicherung erzwingt. |
| `InitialVolume` | Volumen des initialen Trades beim Start des Zyklus. |
| `StartWithLong` | Wenn aktiviert, öffnet eine initiale Long-Position bei flachem Konto. |
| `EnableVerboseLogging` | Schreibt Absicherungsaktionen in das Strategie-Log zur Fehlersuche. |
| `CandleType` | Kerzenserie zur Überwachung der Drawdowns. |

## Unterschiede zur MetaTrader-Version

- Der Expert Advisor verwendete Ticket-Kommentare (`hedge_buy` / `hedge_sell`) zur Unterscheidung von Absicherungspositionen. Die Konvertierung speichert diesen Zustand im Speicher, da StockSharp Netting verwendet.
- Margin-Prüfungen und Slippage-Einstellungen werden weggelassen; die Orderplatzierung verwendet die High-Level-Helfer `BuyMarket` / `SellMarket`.
- Die Strategie stellt Optimierungsbereiche für die Pip-Schwellenwerte und das Volumen bereit, damit sie mit StockSharp-Optimierern abgestimmt werden können.

## Verwendungshinweise

1. Hängen Sie die Strategie an das gewünschte Symbol und Portfolio an.
2. Passen Sie die Pip-Schwellenwerte an die Volatilität des Instruments an.
3. Aktivieren Sie das ausführliche Logging beim Validieren der Konvertierung – das Log erfasst jede Absicherungserstellung und -entfernung mit Pip-Statistiken.
4. Setzen Sie auf Zeitrahmen ein, die bedeutungsvolle Kerzenabschlüsse liefern (z.B. M15 bis H1), um Overtrading zu vermeiden.
