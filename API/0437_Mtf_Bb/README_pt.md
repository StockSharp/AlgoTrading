# Estratégia de Bollinger Bands Multi-Timeframe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Aplica Bollinger Bands tanto em um período principal quanto em um superior. Opera quando o preço perfura as bandas do período superior e opcionalmente filtra entradas com uma média móvel de longo prazo. O objetivo é dissipar os extremos contra a tendência mais ampla.

A estratégia suporta posições compradas e vendidas. Uma porcentagem de stop-loss pode ser habilitada para gestão de risco. O uso de múltiplos períodos ajuda a evitar operações contra a estrutura dominante do mercado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento abaixo da banda inferior do período superior e acima do filtro MA (se habilitado).
  - **Vendido**: Fechamento acima da banda superior do período superior e abaixo do filtro MA (se habilitado).
- **Critérios de saída**:
  - Comprado: Preço fecha acima da banda superior do período atual.
  - Vendido: Preço fecha abaixo da banda inferior do período atual.
- **Indicadores**:
  - Bollinger Bands em dois períodos (comprimento 20, multiplicador 2)
  - Filtro EMA opcional (período 200)
- **Stops**: Stop-loss opcional via StartProtection (baseado em %).
- **Valores padrão**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `UseMaFilter` = False
  - `MaLength` = 200
  - `SLPercent` = 2
- **Filtros**:
  - Contratendência com contexto MTF
  - Período: principal 5m, MTF 60m por padrão
  - Indicadores: Bollinger Bands, EMA
  - Stops: Opcional
  - Complexidade: Moderado
