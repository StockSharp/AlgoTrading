# Elliott-Wellen-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet den Elliott-Wellen-Oszillator (EWO) auf Kerzenschlüsse an. Der EWO wird als Differenz zwischen einem schnellen und einem langsamen Simple Moving Average berechnet (Standard 5 und 35 Perioden). Die Handelslogik sucht nach Wendepunkten im Oszillator, um potenzielle Trendwenden zu erfassen.

Eine Long-Position wird eröffnet, wenn der Oszillator einen lokalen Tiefpunkt bildet und zu steigen beginnt. Eine Short-Position wird eröffnet, wenn der Oszillator einen lokalen Hochpunkt bildet und zu fallen beginnt. Bestehende Positionen werden entsprechend umgekehrt. Optionale prozentuale Take‑Profit- und Stop‑Loss-Schutzmaßnahmen werden über `StartProtection` unterstützt.

## Details

- **Indikator**: Elliott-Wellen-Oszillator = SMA(schnell) − SMA(langsam).
- **Einstiegskriterien**:
  - **Long**: Oszillatorwert fiel und dreht dann aufwärts.
  - **Short**: Oszillatorwert stieg und dreht dann abwärts.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Position kehrt sich bei entgegengesetztem Signal um oder tritt über Stop oder Take‑Profit aus.
- **Stops**: Prozentualer Stop‑Loss und Take‑Profit.
- **Filter**: Keine.
