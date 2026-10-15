# MACD Long-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert RSI-Extremwerte mit MACD-Kreuzungen, um Rücksetzer innerhalb eines Trends zu erfassen. Nachdem RSI einen extremen Wert erreicht, wartet das System auf eine bestätigende MACD-Kreuzung vor dem Einstieg. Dieser Ansatz filtert rauschige Momentum-Verschiebungen und konzentriert sich auf hochwahrscheinliche Umkehrungen.

Die Strategie handelt in beide Richtungen und kann schnell wechseln, wenn entgegengesetzte Signale erscheinen. MACD liefert Momentum-Bestätigung, während RSI überkaufte und überverkaufte Zonen hervorhebt. Schutz-Stops können über die Risikokontrollen des Motors hinzugefügt werden.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI fällt unter die Überverkauft-Zone, dann kreuzt die MACD-Linie über das Signal.
  - **Short**: RSI steigt über die Überkauft-Zone, dann kreuzt die MACD-Linie unter das Signal.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung oder Stop ausgelöst.
- **Indikatoren**:
  - RSI (Länge 14, Überverkauft 30, Überkauft 70)
  - MACD (schnell 12, langsam 26, Signal 9)
- **Stops**: Implementierung über StartProtection oder externes Money-Management.
- **Standardwerte**:
  - `RsiLength` = 14
  - `Oversold` = 30
  - `Overbought` = 70
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filter**:
  - Momentum-Umkehr
  - Funktioniert auf verschiedenen Zeitrahmen
  - Indikatoren: RSI, MACD
  - Stops: Optional
  - Komplexität: Grundlegend
