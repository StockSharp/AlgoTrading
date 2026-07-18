# Стратегия FON60DK
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия открывает длинные позиции, когда линия Tillson T3 поднимается выше верхней полосы Optimized Trend Tracker (OTT), а Williams %R подтверждает бычий импульс. Позиция закрывается, когда Tillson T3 опускается ниже противоположной полосы OTT и Williams %R переходит в зону перепроданности.

## Детали

- **Условия входа**: `T3 > OTT_up` && `Williams %R > -20`
- **Условия выхода**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **Тип**: Следование тренду
- **Индикаторы**: Tillson T3, OTT, Williams %R
- **Таймфрейм**: 1 минута (по умолчанию)
- **Стопы**: Нет
