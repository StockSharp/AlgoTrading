# 30-Minuten-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ansatz vergleicht den Eröffnungspreis der aktuellen 30-Minuten-Kerze mit dem Schlusskurs der vorherigen Kerze.
Wenn eine neue Kerze über dem vorherigen Schlusskurs eröffnet, wird eine Long-Position eröffnet.
Wenn bereits eine Long-Position gehalten wird und die nächste Kerze unter dem vorherigen Schlusskurs eröffnet, kehrt die Strategie zu einer Short-Position um.
Alle offenen Positionen werden eine Minute vor dem Ende der aktuellen Kerze geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Eröffnungskurs der aktuellen Kerze > Schlusskurs der vorherigen Kerze.
  - **Short**: Eröffnungskurs der aktuellen Kerze < Schlusskurs der vorherigen Kerze, während eine Long-Position gehalten wird.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Eine Minute vor Kerzenschluss jede Position schließen.
- **Stops**: Keine.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame().
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Price action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
