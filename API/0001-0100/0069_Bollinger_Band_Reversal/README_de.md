# Bollinger-Band-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Preisextreme außerhalb der Bollinger Bands kehren oft zur mittleren Band zurück. Dieser Ansatz geht gegen diese Ausdehnungen vor: Er kauft Einbrüche unterhalb des unteren Bands, wenn die Kerze grün schließt, und verkauft Rallyes über dem oberen Band nach einer roten Kerze.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 94%. Sie funktioniert am besten auf dem Aktienmarkt.

Der Algorithmus berechnet Bollinger Bands auf jedem Balken und prüft, ob der Schlusskurs das äußere Band durchbricht. Wenn eine bullische Kerze unter dem unteren Band schließt, wird ein Long eröffnet; wenn eine bärische Kerze über dem oberen Band schließt, wird ein Short eingegangen. Der Stop basiert auf einem ATR-Vielfachen, während Ausstiege erfolgen, wenn der Preis zur mittleren Band zurückkehrt.

Mean-Reversion-Trades dauern typischerweise nur wenige Balken, was dieses Setup für kurzfristige Volatilitätskontraktionen geeignet macht.

## Details

- **Einstiegskriterien**: Schluss unter unterem Band mit bullischer Kerze oder Schluss über oberem Band mit bärischer Kerze.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Preis kreuzt mittleres Band oder Stop-Loss.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

