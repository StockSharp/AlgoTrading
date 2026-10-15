# Flawless Victory-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Flawless Victory ist ein modulares Momentum-System, das Oszillatoren mit Bollinger-Bändern kombiniert. Je nach gewählter Version kann es mit einfachen RSI-Signalen arbeiten, feste Take-Profit- und Stop-Loss-Ziele anwenden oder eine Bestätigung durch den Money Flow Index verlangen. Das Ziel ist es, die Erschöpfung an den Rändern der Volatilitätsbänder auszunutzen und Mean-Reversion-Schwingungen zu reiten.

Version 1 steigt ein, wenn der RSI überverkaufte oder überkaufte Zonen nahe den Bollinger-Extremen verlässt. Version 2 fügt explizite Risikosteuerung über prozentbasierte Ziele hinzu. Version 3 verlangt, dass sowohl RSI als auch MFI übereinstimmen, und filtert schwache Umkehrungen heraus.

Die Strategie funktioniert am besten auf Intraday-Märkten mit klaren Volatilitätsgrenzen.

## Details

- **Einstiegskriterien**:
  - **Long**: siehe Versionsregeln (RSI <30 nahe der unteren Bande; Version 3 auch `MFI < 20`)
  - **Short**: RSI >70 nahe der oberen Bande (Version 3 auch `MFI > 80`)
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - **Version 1**: entgegengesetztes RSI-Signal
  - **Version 2**: Take-Profit- oder Stop-Loss-Prozentsätze
  - **Version 3**: entgegengesetzte RSI/MFI-Kombination
- **Stops**: Optional in Version 2
- **Standardwerte**:
  - `RSI_length` = 14
  - `MFI_length` = 14
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `TakeProfitPct` = 1.5
  - `StopLossPct` = 1.0
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, MFI, Bollinger Bands
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
