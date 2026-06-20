# MACD + DMI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert den Moving Average Convergence Divergence mit dem Directional Movement Index, um nur dann zu handeln, wenn die Trendstärke bestätigt ist. Das System wartet auf einen MACD-Crossover und überprüft, ob die dominante Richtungslinie die entgegengesetzte Linie übersteigt, während der ADX über einem Schlüsselniveau liegt.

Die Strategie ist für Long- und Short-Positionen konzipiert. Durch die Kombination von Momentum- und Trendfiltern soll sie Fehlsignale in Seitwärtsmärkten vermeiden. Auf Volatilität basierende Schutz-Stops halten das Risiko begrenzt.

## Details

- **Einstiegskriterien**:
  - **Long**: MACD-Linie kreuzt über die Signallinie, +DI > -DI, und ADX über dem Schlüsselniveau.
  - **Short**: MACD-Linie kreuzt unter die Signallinie, -DI > +DI, und ADX über dem Schlüsselniveau.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal oder Volatilitäts-Stop ausgelöst.
- **Indikatoren**:
  - MACD (schnell 12, langsam 26, Signal 9)
  - Directional Movement Index (Länge 14, ADX-Glättung 14)
- **Stops**: Verwendet integrierten Stop-Loss und Take-Profit über StartProtection.
- **Standardwerte**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filter**:
  - Trendfolge
  - Funktioniert auf mehreren Zeitrahmen
  - Indikatoren: MACD, DMI
  - Stops: Ja
  - Komplexität: Moderat
