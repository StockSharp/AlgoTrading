# Estratégia de Níveis com Revolve
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre negociações quando o preço de mercado cruza um nível definido pelo usuário. Uma ordem de compra é colocada quando o preço sobe através do nível e uma ordem de venda quando o preço cai abaixo dele. O sistema pode opcionalmente reverter uma posição existente se o sinal contrário aparecer. Também suporta distâncias opcionais de stop-loss e take-profit medidas em unidades de preço.

A estratégia subscreve velas e reage apenas quando uma vela está completamente formada. Todos os cálculos são realizados sobre o preço de fechamento de cada vela terminada. Quando o modo de reversão está ativado, a posição atual é fechada e uma nova posição na direção oposta é aberta no próximo sinal.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço de fechamento cruza acima de `LevelPrice`.
  - Vendido: o preço de fechamento cruza abaixo de `LevelPrice`.
- **Comprado/Vendido**: Ambos os sentidos.
- **Reversão**: Opcional, controlada por `EnableReversal`.
- **Stops**: Stop-loss e take-profit opcionais em unidades de preço.
- **Valores padrão**:
  - `LevelPrice` = 100.
  - `StopLoss` = 0 (desativado).
  - `TakeProfit` = 0 (desativado).
  - `EnableReversal` = false.
  - `CandleType` = período de 1 minuto.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Opcional
  - Complexidade: Simples
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
