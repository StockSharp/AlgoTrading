# Linear Regression Channel (Fibo)-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist die StockSharp-Konvertierung des MetaTrader-Expertenberaters **"linear regression channel"**. Sie handelt in Richtung des übergeordneten linearen Trends, bestätigt durch gewichtete gleitende Durchschnitte, Momentum-Werte und einen monatlichen MACD-Filter. Die Geldmanagement-Regeln replizieren das Originalverhalten mit schwebenden Gewinnzielen, Trailing der kumulierten Gewinne, Break-even-Schutz und einem Equity-Stop.

## Handelslogik
1. **Primärer Zeitrahmen** – konfigurierbarer Kerzentyp (Standard 15 Minuten). Alle Signalberechnungen laufen auf diesem Zeitrahmen.
2. **Trendfilter** – eine schnelle und eine langsame linear gewichtete Moving Average (LWMA), berechnet auf dem typischen Preis. Long-Signale erfordern, dass die schnelle LWMA über der langsamen liegt; Short-Signale erfordern das Gegenteil.
3. **Momentum-Bestätigung** – der Momentum-Indikator wird auf einem höheren Zeitrahmen ausgewertet, der das ursprüngliche MetaTrader-Mapping widerspiegelt (M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1). Die letzten drei Momentum-Werte werden in den absoluten Abstand vom Niveau 100 umgerechnet. Ein Long-Setup erfordert, dass mindestens einer der drei Abstände den bullischen Schwellenwert überschreitet, während ein Short-Setup erfordert, dass mindestens einer den bärischen Schwellenwert überschreitet.
4. **Monatlicher MACD-Bias** – monatliche Kerzen treiben einen MACD(12,26,9)-Filter an. Long-Trades sind nur erlaubt, wenn die MACD-Hauptlinie über ihrer Signallinie liegt; Short-Trades erfordern die entgegengesetzte Beziehung.
5. **Eintrittsbedingung** – wenn alle Filter übereinstimmen und der Handel erlaubt ist, eröffnet die Strategie eine Market-Order in der entsprechenden Richtung. Die aktuelle Position wird geschlossen und umgekehrt, wenn ein entgegengesetztes Signal erzeugt wird.

## Risiko- und Trade-Management
- **Fester Stop-Loss / Take-Profit** – Abstände werden in Instrumentenpunkten definiert und auf jeden Einstieg angewendet. Wenn das Kerzenhoch/-tief diese Niveaus durchbricht, wird die Position geschlossen.
- **Trailing Stop** – optional; aktiviert sich, sobald die Position einen konfigurierbaren Punktbetrag gewinnt, und verfolgt den besten Preis mit dem angegebenen Offset.
- **Break-even** – optional; nachdem der Preis um die Auslösedistanz vorangeschritten ist, wird das Stop-Niveau auf den Einstiegspreis plus/minus einem Offset verschoben, um Gewinne zu sichern.
- **Schwebender Gewinn-Take-Profit** – optionales Geldlimit. Wenn der unrealisierte Nettogewinn (in Kontowährung) den Schwellenwert überschreitet, werden alle Positionen geschlossen.
- **Prozentbasierter Take-Profit** – optionales Ziel basierend auf dem anfänglichen Eigenkapital beim Start der Strategie.
- **Geld-Trailing** – sobald der schwebende Gewinn den Auslöser erreicht, registriert die Strategie den Spitzengewinn. Wenn der Gewinn um den angegebenen Stop-Betrag zurückgeht, wird die Position geschlossen.
- **Equity-Stop** – optionaler Drawdown-Schutz. Wenn die Position verliert und der schwebende Verlust einen Prozentsatz des beobachteten Eigenkapital-Peaks überschreitet, liquidiert die Strategie die Position.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `Candle Type` | Primärer Zeitrahmen für die Signalerzeugung. |
| `Fast LWMA` / `Slow LWMA` | Perioden für die schnellen und langsamen linear gewichteten gleitenden Durchschnitte. |
| `Momentum Length` | Momentum-Rückblicklänge auf dem höheren Zeitrahmen. |
| `Momentum Buy Threshold` / `Momentum Sell Threshold` | Minimaler absoluter Abstand von 100 für bullische/bärische Momentum-Bestätigung. |
| `Take Profit (points)` / `Stop Loss (points)` | Schutzabstände in Instrumentenpunkten. |
| `Use Trailing`, `Trailing Activation`, `Trailing Offset` | Trailing-Stop-Konfiguration. |
| `Use Break-even`, `Break-even Trigger`, `Break-even Offset` | Break-even-Logikparameter. |
| `Max Trades` | Maximale Anzahl sequenzieller Einstiege während des Laufs. |
| `Order Volume` | Basisvolumen für Market-Orders. |
| `Use Money TP`, `Money Take Profit` | Schwebender monetärer Take-Profit. |
| `Use Percent TP`, `Percent Take Profit` | Take-Profit als Prozentsatz des anfänglichen Eigenkapitals. |
| `Enable Money Trailing`, `Money Trailing Trigger`, `Money Trailing Stop` | Trailing des schwebenden Gewinns. |
| `Use Equity Stop`, `Equity Risk %` | Eigenkapitalbasierter Stop-Loss-Schutz. |

## Hinweise
- Die Strategie hält nur eine Nettoposition (Long oder Short) und kehrt um, wenn ein entgegengesetztes Signal eintrifft.
- Momentum- und MACD-Abonnements fügen dem Datenfeed automatisch die notwendigen höheren Zeitrahmen über `GetWorkingSecurities()` hinzu.
- Alle Kommentare im Code sind gemäß Repository-Richtlinien auf Englisch.
