# Levels-mit-Revolve-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Trades, wenn der Marktpreis ein benutzerdefiniertes Level kreuzt. Eine Kauforder wird platziert, wenn der Preis durch das Level nach oben steigt, und eine Verkaufsorder, wenn der Preis darunter fällt. Das System kann optional eine bestehende Position umkehren, wenn das entgegengesetzte Signal erscheint. Es werden auch optionale Stop-Loss- und Take-Profit-Abstände in Preiseinheiten unterstützt.

Die Strategie abonniert Kerzen und reagiert nur, wenn eine Kerze vollständig ausgebildet ist. Alle Berechnungen werden auf Basis des Schlusskurses jeder fertigen Kerze durchgeführt. Wenn der Umkehrmodus aktiviert ist, wird die aktuelle Position geschlossen und beim nächsten Signal eine neue Position in die entgegengesetzte Richtung eröffnet.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs kreuzt über `LevelPrice`.
  - Short: Schlusskurs kreuzt unter `LevelPrice`.
- **Long/Short**: Beide Richtungen.
- **Umkehr**: Optional, gesteuert durch `EnableReversal`.
- **Stops**: Optionaler Stop-Loss und Take-Profit in Preiseinheiten.
- **Standardwerte**:
  - `LevelPrice` = 100.
  - `StopLoss` = 0 (deaktiviert).
  - `TakeProfit` = 0 (deaktiviert).
  - `EnableReversal` = false.
  - `CandleType` = 1-Minuten-Zeitrahmen.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Optional
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
