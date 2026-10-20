# BTCUSD Momentum After Abnormal Days

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Die Rendite einer abgeschlossenen Tageskerze lautet `r=(Close-Open)/Open`. Mittelwert und Populationsabweichung verwenden die vorherigen `LookbackPeriod` abgeschlossenen Tagesrenditen ohne die aktuelle; das Vorfenster muss vollständig sein. Kauf bei `r>mean+k*sd`, Verkauf bei `r<mean-k*sd`, mit `k=AbnormalThreshold`. Gleichheit erzeugt keinen Einstieg.

Einstieg nur ohne Position. Menge: `floor(CapitalPerTrade/Close/VolumeStep)*VolumeStep`; Mengen unter dem Minimum entfallen, oberhalb des Maximums werden abgewiesen. Kapital bezieht sich auf den Signalschluss, Marktpreise können abweichen. Die gesamte ausgeführte Position wird zum nächsten Tagesschluss oder nach einer Datenlücke zum ersten verfügbaren späteren Tagesschluss geschlossen. Ausstieg hat auf dieser Kerze Vorrang. Offene Orders verhindern Duplikate; kein Preisstop.

## Parameter

| Parameter | Standardwert |
|---|---|
| `CandleType` | `1d` |
| `LookbackPeriod` | `5` |
| `AbnormalThreshold` | `1.6` |
| `CapitalPerTrade` | `1000` |
