# Sistema de Confirmação de Volume para uma Tendência (Estratégia)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Indicador de Impulso de Tendência (TTI), o Indicador de Confirmação de Preço por Volume (VPCI) e o ADX para confirmar tendências de alta.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: ADX > 30, TTI > sinal, VPCI > 0.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - VPCI < 0.
- **Stops**: Não.
- **Valores padrão**:
  - `ADX Length` = 14
  - `ADX Smoothing` = 14
  - `TTI Fast Average` = 13
  - `TTI Slow Average` = 26
  - `TTI Signal Length` = 9
  - `VPCI Short Avg` = 5
  - `VPCI Long Avg` = 25
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: ADX, TTI, VPCI
  - Stops: Não
  - Complexidade: Médio
  - Período: Médio prazo
