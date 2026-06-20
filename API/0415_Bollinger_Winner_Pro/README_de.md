# Bollinger Winner Pro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Winner Pro erweitert die Lite-Version durch modulare Filter und
Risikokontrollen. Es sucht weiterhin nach Preisen, die außerhalb der Bollinger Bands
schließen, aber Trades werden nur ausgeführt, wenn optionale Bestätigungen übereinstimmen.

Trader können RSI-, Aroon- und Moving-Average-Filter aktivieren, um Momentum und
Trendrichtung zu bestätigen. Ein integrierter Stop-Loss kann ebenfalls aktiviert
werden, um das Risiko zu begrenzen. Diese Flexibilität ermöglicht es der Strategie,
sich an verschiedene Märkte oder Testanforderungen anzupassen.

Der Ansatz zielt auf Mean Reversion: Sobald der Preis wieder in die Bands eintritt
oder die gegenüberliegende Seite berührt, wird die Position geschlossen oder der
Stop ausgelöst. Da mehrere Filter gestapelt werden können, sind Signale seltener,
aber von höherer Qualität.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**: Kerze schließt außerhalb eines Bandes und alle aktivierten Filter stimmen zu.
- **Ausstiegskriterien**: Rückkehr zum mittleren/gegenüberliegenden Band oder Stop-Loss wenn `UseSL` wahr ist.
- **Stops**: Optionaler Stop-Loss, gesteuert durch `UseSL`.
- **Standardwerte**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filter**:
  - Kategorie: Mean Reversion mit Bestätigungen
  - Richtung: Long/Short
  - Indikatoren: Bollinger Bands, RSI, Aroon, Moving Average
  - Komplexität: Fortgeschritten
  - Risikolevel: Mittel
