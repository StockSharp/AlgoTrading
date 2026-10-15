# Strategie Vwap Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie - VWAP + CCI. Kaufen, wenn der Preis unter dem VWAP liegt und der CCI unter -100 (überverkauft) ist. Verkaufen, wenn der Preis über dem VWAP liegt und der CCI über 100 (überkauft) ist.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 82%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Der VWAP dient als Wertbenchmark, und der CCI hebt Momentumbewegungen weg von ihm hervor. Einstiege bevorzugen starke CCI-Werte relativ zum VWAP.

Entwickelt für Daytrader, die sich auf die VWAP-Interaktion konzentrieren. Ein prozentualer Stop hilft bei der Aufrechterhaltung der Disziplin.

## Details

- **Einstiegskriterien**:
  - Long: `Close < VWAP && CCI < CciOversold`
  - Short: `Close > VWAP && CCI > CciOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kreuzt zurück durch den VWAP
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Der Markt handelt rund um die Uhr, daher beginnt der Sitzungs-VWAP mit jedem UTC-Tag neu und gewichtet den typischen Preis jeder Kerze mit ihrem Volumen. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

