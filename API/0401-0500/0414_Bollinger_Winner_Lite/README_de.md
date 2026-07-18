# Bollinger Winner Lite-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Winner Lite ist ein vereinfachtes Umkehrsystem, das reagiert, wenn der
Preis über die Bollinger Bands hinaus gedehnt wird. Es beobachtet große Kerzen,
die außerhalb eines Bandes schließen, und erwartet einen schnellen Rückprall nach
innen.

Der `CandlePercent`-Parameter legt fest, wie groß die Ausbruchskerze im Verhältnis
zu den letzten Bewegungen sein muss. Nur Kerzen, die diesen Schwellenwert überschreiten,
lösen Trades aus und filtern kleine Schwankungen heraus. Standardmäßig handelt die
Strategie nur Long, aber die Aktivierung von `ShowShort` erlaubt gespiegelte
Short-Setups.

Ausstiege erfolgen, wenn der Preis das gegenüberliegende Band berührt oder zur
mittleren Linie zurückkehrt. Es wird kein harter Stop verwendet; das System basiert
auf Mean Reversion.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs unter unterem Band mit Kerzengröße > `CandlePercent`.
  - **Short**: Schlusskurs über oberem Band mit Kerzengröße > `CandlePercent` (erfordert `ShowShort`).
- **Ausstiegskriterien**: Berührung des mittleren Bandes oder des gegenüberliegenden Bandes.
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `ShowShort` = false
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Standardmäßig nur Long
  - Indikatoren: Bollinger Bands
  - Komplexität: Einfach
  - Risikolevel: Mittel
