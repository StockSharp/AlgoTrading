# Strategie-Basis-Vorlage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ordner bietet ein minimales Gerüst zum Aufbau eigener Trading-Ideen. Die
Strategie berechnet nur einen einzigen exponentiellen gleitenden Durchschnitt und
stellt eine breite Palette gängiger Parameter bereit: Long- oder Short-Trades aktivieren,
optionales Take Profit und Stop Loss sowie Optimierungsbereiche. Entwickler können ihre
eigene Einstiegs- und Ausstiegslogik in die Platzhalter einfügen, um neue Systeme
schnell zu prototypisieren.

Die Vorlage zeigt auch, wie das integrierte Schutzmodul mit prozentualen Zielen gestartet
werden kann, was das Experimentieren mit verschiedenen Risikoeinstellungen erleichtert.
Da keine echten Signale enthalten sind, soll dieses Skript nicht direkt gehandelt werden,
sondern als Ausgangspunkt für weitere Forschung dienen.

## Details

- **Einstiegskriterien**: Nicht implementiert – durch eigene Regeln ersetzen.
- **Long/Short**: Über Parameter konfigurierbar.
- **Ausstiegskriterien**: Nicht implementiert – durch eigene Regeln ersetzen.
- **Stops**: Optionaler prozentualer Take Profit und Stop Loss über das Schutzmodul.
- **Standardwerte**:
  - EMA-Länge = 10.
  - Take Profit = 1.2%, Stop Loss = 1.8% (standardmäßig deaktiviert).
- **Filter**:
  - Kategorie: Vorlage
  - Richtung: Konfigurierbar
  - Indikatoren: EMA
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Benutzerdefiniert
