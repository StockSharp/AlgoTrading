# Estratégia FON60DK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre posições compradas quando a linha Tillson T3 sobe acima da banda superior do Optimized Trend Tracker (OTT) e Williams %R confirma momentum altista. A posição é fechada quando Tillson T3 cai abaixo da banda OTT oposta e Williams %R entra em território de sobrevenda.

## Detalhes

- **Critérios de entrada**: `T3 > OTT_up` && `Williams %R > -20`
- **Critérios de saída**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **Tipo**: Seguidor de tendência
- **Indicadores**: Tillson T3, OTT, Williams %R
- **Período**: 1 minuto (padrão)
- **Stops**: Nenhum
