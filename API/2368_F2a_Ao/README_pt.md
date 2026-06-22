# Estratégia F2a AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o assessor especialista original do MetaTrader "F2a_AO". Ela filtra o Awesome Oscillator com uma SMA curta e abre operações apenas na direção de uma vela de referência em um período superior.

O oscilador é calculado em seu próprio período. Quando a vela de referência fecha acima de sua abertura, um AO filtrado positivo aciona uma entrada comprada e fecha qualquer posição vendida. Quando a vela de referência fecha abaixo de sua abertura, um AO filtrado negativo aciona uma entrada vendida e fecha qualquer posição comprada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A vela de referência é de alta e o AO filtrado > 0.
  - **Vendido**: A vela de referência é de baixa e o AO filtrado < 0.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - AO filtrado < 0 fecha posições compradas.
  - AO filtrado > 0 fecha posições vendidas.
- **Stops**: Sem stop-loss ou take-profit explícito, módulo de proteção está habilitado.
- **Valores padrão**:
  - `IndicatorTimeFrame` = 12 horas.
  - `TrendTimeFrame` = 1 dia.
  - `FastPeriod` = 13.
  - `SlowPeriod` = 144.
  - `FilterLength` = 3.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Awesome Oscillator, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
