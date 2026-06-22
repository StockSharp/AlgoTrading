# 2MA Bunny Cross Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **2MA Bunny Cross Expert** opera o cruzamento de duas médias móveis simples. Uma operação comprada é aberta quando a média rápida sobe acima da lenta, enquanto uma operação vendida é aberta quando a média rápida cai abaixo da lenta. Qualquer posição oposta é fechada antes de uma nova ser aberta.

## Detalhes

- **Propósito**: seguimento de tendência via cruzamento de médias móveis
- **Negociação**: comprado e vendido
- **Indicadores**: Média móvel simples rápida e lenta
- **Stops**: nenhum
- **Valores padrão**:
  - `CandleType` = 1 minute
  - `FastLength` = 5
  - `SlowLength` = 20
