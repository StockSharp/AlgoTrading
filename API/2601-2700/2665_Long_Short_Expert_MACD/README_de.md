# Long/Short Expert MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Long/Short Expert MACD Strategie** ist eine StockSharp-Konvertierung des MetaTrader-Experts "LongShortExpertMACD". Sie kombiniert die Standard Moving Average Convergence Divergence (MACD) Kreuzungslogik mit festem Risikomanagement. Die Strategie reagiert auf Kreuzungen zwischen der MACD-Linie und ihrer Signallinie, kann im Long-only-, Short-only- oder bidirektionalen Modus betrieben werden und wendet automatisch Take-Profit- und Stop-Loss-Niveaus an, die in Kursschritten ausgedrückt werden.

Die Implementierung verwendet die High-Level StockSharp-API mit Kerzenabonnements und Indikatorbindungen. Orders werden als Marktorders registriert, was die Strategie einfach an Echtzeit- und historische Datenquellen anschließbar macht.

## Indikatoren und Marktdaten
- **Kerzen** – ein einzelner Zeitrahmen, bereitgestellt durch den Parameter `CandleType` (standardmäßig 1-Minuten-Zeitrahmen). Die Strategie abonniert diese Kerzenserie über `SubscribeCandles`.
- **MovingAverageConvergenceDivergenceSignal** – StockSharp's MACD-Indikator mit konfigurierbaren Längen für schnellen EMA, langsamen EMA und Signal-EMA. Der Histogrammwert ergibt sich implizit aus der Differenz zwischen MACD- und Signalausgaben.

## Handelslogik
1. **Signalvorbereitung**
   - Bei jeder fertigen Kerze werden die MACD- und Signalwerte über die Indikatorbindung abgerufen.
   - Der historische Status `_prevIsMacdAboveSignal` verfolgt, ob der MACD bei der vorherigen Kerze über der Signallinie lag.

2. **Einstiegskriterien**
   - **Bullisches Kreuz**: wenn der MACD die Signallinie nach oben kreuzt, eröffnet die Strategie eine Long-Position, wenn die konfigurierte Handelsrichtung Long-Einstiege erlaubt.
     - Wenn bereits eine Short-Position aktiv ist und der Umkehrmodus aktiviert ist (`AllowedPosition = Both`), enthält die Ordergröße das aktuelle Short-Volumen, um die Position in einer einzelnen Marktorder zu schließen und zu Long zu wechseln.
     - Im Long-only-Modus wird eine vorhandene Short-Position sofort geschlossen, aber kein neuer Long-Trade eröffnet, bis das folgende Signal kommt.
   - **Bärisches Kreuz**: die symmetrische Aktion für Short-Einstiege.

3. **Ausstiegskriterien**
   - **Risikomanagement**: sowohl Stop-Loss- als auch Take-Profit-Niveaus werden vom aktuellen durchschnittlichen Eintrittspreis aus neu berechnet, wann immer eine Position erkannt wird. Die Abstände werden in Kursschritten festgelegt (d.h. `Security.PriceStep * Parameter`), was das Verhalten über Instrumente hinweg konsistent hält.
     - Long-Positionen werden geschlossen, wenn das Tief der Kerze das Stop-Loss-Niveau erreicht oder das Hoch das Take-Profit-Niveau erreicht.
     - Short-Positionen werden geschlossen, wenn das Hoch der Kerze das Stop-Loss-Niveau erreicht oder das Tief das Take-Profit-Niveau berührt.
   - **Entgegengesetztes Kreuz**: wenn die Handelsrichtung die entgegengesetzte Seite erlaubt, wird die Position abgeflacht (und optional umgekehrt), wann immer die Indikatorbeziehung wechselt.

4. **Betriebliche Sicherungen**
   - Die Handelslogik wird nur ausgeführt, wenn die Strategie gebildet, online und der Handel erlaubt ist (`IsFormedAndOnlineAndAllowTrading`).
   - Schutzniveaus werden zurückgesetzt, wenn keine Position gehalten wird, um veraltete Schwellenwerte zu vermeiden.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `AllowedPosition` | `Both` | Beschränkt die Strategie auf Long-only, Short-only oder bidirektionalen Handel. |
| `FastLength` | `12` | Periode des schnellen EMA innerhalb der MACD-Berechnung. |
| `SlowLength` | `24` | Periode des langsamen EMA innerhalb der MACD-Berechnung. |
| `SignalLength` | `9` | Periode des Signal-EMA zur Kreuzungserkennung. |
| `TakeProfitPoints` | `50` | Abstand zum Take-Profit-Niveau in Kursschritten (`PriceStep * Punkte`). Auf `0` setzen zum Deaktivieren. |
| `StopLossPoints` | `20` | Abstand zum Stop-Loss-Niveau in Kursschritten. Auf `0` setzen zum Deaktivieren. |
| `CandleType` | `TimeFrame(1 minute)` | Kerzenserie zur Signalgenerierung. |
| `Volume` | `1` | Anzahl Lots/Kontrakte bei jeder Marktorder. |

Alle numerischen Parameter bieten Optimierungsbereiche, um das Walk-Forward-Testing in StockSharp Designer oder Runner zu vereinfachen.

## Positionsmanagement
- **Umkehrlogik**: wenn bidirektionaler Handel erlaubt ist, verwendet die Strategie kombinierte Ordergrößen, um Positionen in einer einzigen Marktorder umzukehren, und spiegelt das Verhalten des Original-MetaTrader-Experts wider.
- **Long-only / Short-only Modi**: bestehende Positionen auf der nicht erlaubten Seite werden sofort geschlossen, aber kein neues Engagement wird eingegangen, bis ein Signal in der erlaubten Richtung auftritt.
- **Stop/Take Neuberechnung**: die Strategie berechnet Schutzniveaus bei jeder Kerze mit dem aktuellen `PositionAvgPrice` neu, um korrekte Abstände auch nach Teilfüllungen oder gestaffelten Einstiegen zu gewährleisten.

## Verwendungshinweise
- Stellen Sie sicher, dass das Instrument einen gültigen `PriceStep` liefert; wenn der Wert fehlt, fällt die Strategie auf `1.0` Kurseinheiten zurück, was für aktienähnliche Instrumente geeignet ist, aber für Forex-Symbole möglicherweise angepasst werden muss.
- Die Strategie basiert auf abgeschlossenen Kerzen. Latenzempfindliche Szenarien sollten ausreichend granulare Kerzen liefern, um Verzögerungen zu vermeiden.
- Da Orders Marktorders ohne Slippage-Kontrolle sind, sollte das Risikomanagement mögliche Füllunterschiede berücksichtigen, besonders bei illiquiden Assets.
- Visualisierung wird automatisch erstellt, wenn die Host-Anwendung Chart-Bereiche unterstützt; MACD, Kerzen und eigene Trades werden zur schnellen Überwachung gezeichnet.

## Konvertierungshinweise
- Die StockSharp-Implementierung behält die konfigurierbaren MACD-Parameter, Take-Profit- und Stop-Loss-Abstände sowie den Positions-Verfügbarkeits-Schalter aus dem MQL5-Expert bei.
- Trailing-Stop- und Money-Management-Module aus MetaTrader werden absichtlich ausgelassen, da ihr Verhalten den "Keine"-Varianten des Original-Experts entspricht.
