# 3x Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **3x Supertrend** utiliza três bandas baseadas em ATR com diferentes períodos e multiplicadores.
Uma posição comprada é aberta quando o preço sobe acima de todas as três bandas e a banda rápida muda para
tendência de alta. A operação é fechada quando o preço cai abaixo de todas as bandas, sinalizando a perda do momentum de alta.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**: Preço acima de todas as bandas e banda rápida virando para cima.
- **Critérios de saída**: Preço abaixo de todas as bandas.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `AtrPeriod1` = 11
  - `Factor1` = 1
  - `AtrPeriod2` = 12
  - `Factor2` = 2
  - `AtrPeriod3` = 13
  - `Factor3` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Supertrend baseado em ATR
  - Complexidade: Moderado
  - Nível de risco: Médio
