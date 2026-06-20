# Improvisando-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Improvisando kombiniert einen einfachen EMA-Trendfilter mit RSI-Schwingungen. Das Ziel ist es, der durch den EMA angezeigten vorherrschenden Richtung zu folgen und nur einzusteigen, wenn der RSI die neutrale 50-Linie kreuzt. Das ursprüngliche Design experimentierte auch mit MACD-artigem Momentum, aber diese vereinfachte Version konzentriert sich auf Klarheit und einfache Abstimmung.

Der Benutzer kann Long- und/oder Short-Trades separat aktivieren.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close > EMA` und `RSI > 50`
  - **Short**: `Close < EMA` und `RSI < 50`
- **Long/Short**: Konfigurierbar
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `EmaLength` = 10
  - `RsiLength` = 14
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Konfigurierbar
  - Indikatoren: EMA, RSI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
