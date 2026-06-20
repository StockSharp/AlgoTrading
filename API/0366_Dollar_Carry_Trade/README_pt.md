# Carry Trade do Dólar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de **Carry Trade do Dólar** classifica os pares de moedas em USD pelo diferencial de taxas de juros e fica comprado em USD contra moedas de baixo carry e vendido contra moedas de alto carry. Rebalanceia mensalmente no primeiro dia de negociação.

## Detalhes
- **Critérios de entrada**: Classificar por carry; comprado em baixo carry, vendido em alto carry.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Rebalanceamento mensal.
- **Stops**: Sem stop explícito.
- **Valores padrão**:
  - `K = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Rates
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
