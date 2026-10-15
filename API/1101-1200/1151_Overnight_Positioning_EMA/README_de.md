# Übernacht-Positionierungs-Strategie mit EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eröffnet eine Long-Position kurz vor dem Schließen des gewählten Marktes und schließt sie nach der Markteröffnung. Ein optionaler EMA-Filter bestätigt Einstiege. Die Strategie unterstützt US-, asiatische und europäische Sessions und schließt alle offenen Positionen vor dem Wochenende.

## Details

- **Einstieg**: Minuten vor Marktschluss, wenn der Preis über dem EMA liegt (falls aktiviert).
- **Ausstieg**: Nach der Markteröffnung für die angegebenen Minuten oder fünf Minuten vor dem Freitagsschluss.
- **Markt**: USA, Asien oder Europa.
- **Indikator**: EMA.
- **Richtung**: Nur Long.
- **Stops**: Keine.
