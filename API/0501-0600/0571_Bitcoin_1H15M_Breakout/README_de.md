# Bitcoin 1H-15M Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verfolgt das Hoch und das Tief der vorherigen 1-Stunden-Kerze und eröffnet Trades, wenn eine 15-Minuten-Kerze außerhalb dieses Bereichs schließt. Das Risiko wird mit einem festen Stop-Loss-Puffer und einem Take-Profit auf Basis eines konfigurierbaren Chance-Risiko-Verhältnisses gesteuert.

## Details

- **Einstiegskriterien**:
  - 15-Minuten-Schluss über dem vorherigen 1-Stunden-Hoch → Long-Einstieg.
  - 15-Minuten-Schluss unter dem vorherigen 1-Stunden-Tief → Short-Einstieg.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop-Loss bei festem Pufferabstand.
  - Take-Profit bei Puffer × Chance-Risiko-Verhältnis.
- **Stops**: Stop-Loss und Take-Profit über Schutzmodul.
- **Standardwerte**:
  - Niedrigerer Zeitrahmen = 15 Minuten.
  - Höherer Zeitrahmen = 1 Stunde.
  - Stop-Loss-Puffer = 50.
  - Chance-Risiko-Verhältnis = 2.0.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: SL & TP
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
