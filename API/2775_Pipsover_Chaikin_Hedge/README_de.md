# Pipsover Chaikin Hedge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den MetaTrader-Expertenberater "Pipsover 2" in StockSharp. Sie sucht nach überverkauften oder
überkauften Bedingungen mit dem Chaikin-Oszillator, während der Preis einen gleitenden Durchschnitt durchstößt, und nutzt den
vorherigen Kerzenkörper zur Bestätigung der Umkehrung. Der StockSharp-Port behält die diskretionäre Absicherungslogik des
ursprünglichen Codes bei: Wenn ein entgegengesetztes Signal erscheint, während bereits eine Position besteht, kehrt die Strategie
sofort die Netto-Exposition um, um dem neuen Bias zu folgen.

## Indikatoren und Daten
- **Chaikin-Oszillator**: aufgebaut aus einer Akkumulations/Distributions-Linie, geglättet durch zwei gleitende Durchschnitte. Beide
  Durchschnitte sind konfigurierbar und entsprechen der MetaTrader-Implementierung (einfach, exponentiell, geglättet oder gewichtet).
- **Preis-gleitender-Durchschnitt**: konfigurierbare Länge, Verschiebung und Typ. Er dient als Mean-Reversion-Anker, den vorherige
  Kerzenhochs oder -tiefs durchstechen müssen.
- **Zeitrahmen**: die Strategie abonniert einen einzigen Kerzenstrom, der über den `CandleType`-Parameter gewählt wird.

## Handelslogik
1. Nur mit fertigen Kerzen arbeiten. Der vorherige Kerzenkörper (Schluss vs. Eröffnung) liefert den Richtungsbias.
2. Den Chaikin-Oszillator-Wert der vorherigen Kerze ablesen. Große negative Werte signalisieren überverkauft, große positive Werte
   markieren überkaufte Zonen.
3. Verlangen, dass die vorherige Kerze den aktuellen gleitenden Durchschnittswert durchsticht (`Low < MA` für bullische Setups
   und `High > MA` für bärische).
4. Einsteigen wenn keine Position offen ist:
   - **Long**: vorherige Kerze bullisch, Tief unterhalb MA, Chaikin unterhalb `-OpenLevel`.
   - **Short**: vorherige Kerze bärisch, Hoch oberhalb MA, Chaikin oberhalb `OpenLevel`.
5. Wenn eine Position existiert und ein entgegengesetztes Setup erscheint, kehrt der Algorithmus die Netto-Position um
   (`SellMarket` / `BuyMarket` mit Extra-Volumen), um das Absicherungsverhalten der MT5-Version zu spiegeln.
6. Stops und Ziele werden innerhalb der Strategie mit Kerzenhochs/-tiefs emuliert, da StockSharp mit Nettopositionen statt
   individuellen gesicherten Tickets arbeitet.

## Risikomanagement
- **Stop-Loss und Take-Profit**: Abstände in Pips, umgerechnet durch den Instrument-Preisschritt. Beide können mit null deaktiviert werden.
- **Breakeven**: Sobald der Preis um `BreakevenPips` zugunsten wechselt, wird der Stop auf den Einstiegspreis verschoben.
- **Trailing**: Nachdem die Bewegung `BreakevenPips + TrailingStopPips` überschreitet, folgt der Stop dem Preis im Trailing-Abstand.
- **Positions-Zustand zurücksetzen**: Wann immer ein Ausstieg erfolgt, werden alle internen Preisniveaus geleert, um sich auf den nächsten Trade vorzubereiten.

## Parameter
| Name | Beschreibung |
| ---- | ------------ |
| `OpenLevel` | Chaikin-Magnitude die zum Öffnen einer neuen Position erforderlich ist (Standard 100). |
| `CloseLevel` | Chaikin-Magnitude die zum Umkehren einer bestehenden Position erforderlich ist (Standard 125). |
| `StopLossPips` | Stop-Loss-Abstand in Pips (Standard 65). |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (Standard 100). |
| `TrailingStopPips` | Trailing-Abstand in Pips (Standard 30). |
| `BreakevenPips` | Gewinn in Pips bevor der Stop auf Break-Even verschoben wird (Standard 15). |
| `MaPeriod` | Gleitende Durchschnittslänge für den Preisfilter (Standard 20). |
| `MaShift` | Bars zum Verschieben des gleitenden Durchschnitts (Standard 0). |
| `MaType` | Typ des gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted). |
| `ChaikinFastPeriod` | Schnelle Glättungslänge im Chaikin-Oszillator (Standard 3). |
| `ChaikinSlowPeriod` | Langsame Glättungslänge im Chaikin-Oszillator (Standard 10). |
| `ChaikinMaType` | Für Chaikin-Glättung verwendeter Typ des gleitenden Durchschnitts. |
| `CandleType` | Für Berechnungen verwendete Kerzenreihe. |

## Hinweise
- Die Basis-`Volume`-Eigenschaft in StockSharp konfigurieren, um die Trade-Größe zu steuern.
- Pips werden mit dem `PriceStep` des Instruments berechnet. Wenn der Schritt 3- oder 5-Dezimal-Notierungen entspricht (z.B. 0.00001),
  multipliziert die Strategie ihn mit 10, um dem MetaTrader-Pip-Abstand zu entsprechen.
- Da StockSharp Nettopositionen verwendet, werden Hedge-Orders des ursprünglichen MQL-Expertenberaters als sofortige Umkehrungen
  der bestehenden Position dargestellt.
