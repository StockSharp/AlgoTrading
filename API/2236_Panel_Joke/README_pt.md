# Estratégia Panel Joke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o sistema original *panel-joke* do MetaTrader para StockSharp. Ela compara a vela atual com a anterior em sete métricas de preço (abertura, máxima, mínima, média de máxima e mínima, fechamento, média de máxima/mínima/fechamento e média ponderada de máxima/mínima/fechamento). Cada métrica que aumentou conta para uma configuração de compra potencial; cada queda conta para uma configuração de venda.

Quando o parâmetro `Enable Autopilot` é `true`, a estratégia abre ou inverte posições automaticamente com base em qual lado tem mais pontos. Nenhum indicador adicional ou regra de stop é utilizado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Buy counter > Sell counter.
  - **Vendido**: Sell counter > Buy counter.
- **Critérios de saída**: Inverter quando o sinal oposto aparecer.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Enable Autopilot` = `true`.
  - `Candle Type` = Período de 5 minutos.
- **Filtros**:
  - Categoria: Price action
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

