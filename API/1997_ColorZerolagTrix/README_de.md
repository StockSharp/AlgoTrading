# Color Zerolag TRIX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie aggregiert fünf TRIX-Indikatoren mit unterschiedlichen Perioden und Gewichtungen, um eine schnelle und eine geglättete langsame Linie zu erzeugen. Trades werden ausgelöst, wenn die schnelle Linie die langsame Linie kreuzt.

- **Long-Einstieg:** vorherige schnelle Linie > vorherige langsame Linie und aktuelle schnelle < aktuelle langsame.
- **Short-Einstieg:** vorherige schnelle Linie < vorherige langsame Linie und aktuelle schnelle > aktuelle langsame.
- **Positionsmanagement:** optionale Flags ermöglichen das separate Aktivieren oder Deaktivieren von Long-/Short-Einstiegen und -Ausstiegen.
- **Parameter:** Glättungsfaktor und fünf TRIX-Periodenpaare mit entsprechenden Gewichtungen.
- **Indikatoren:** TRIX (fünf Instanzen) mit gewichteter Summe und Glättung.
- **Standard-Zeitrahmen:** 4-Stunden-Kerzen.

## Filter
- Kategorie: Trendfolge
- Richtung: Beide
- Indikatoren: Mehrere
- Stops: Nein
- Komplexität: Moderat
- Zeitrahmen: Langfristig
- Saisonalität: Nein
- Neuronale Netze: Nein
- Divergenz: Nein
- Risikolevel: Mittel
