# Estratégia de Níveis Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Abre operações quando o preço cruza linhas de tendência definidas pelo usuário. Cada linha pode acionar posições compradas, vendidas ou ambas as direções. O stop loss e o take profit são definidos em passos de preço.

## Detalhes

- **Critérios de entrada**: Preço cruzando uma linha de tendência configurada
- **Comprado/Vendido**: Determinado pela direção da linha (Buy/Sell/Both)
- **Critérios de saída**: Níveis de stop loss ou take profit
- **Stops**: Sim
- **Valores padrão**:
  - `StopLoss` = 300 steps
  - `TakeProfit` = 900 steps
  - `Volume` = 1
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Níveis
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Uso

1. Criar e configurar linhas de tendência via `AddLine`.
2. Iniciar a estratégia para monitorar as velas recebidas.
3. Quando o preço cruza uma linha ativa na direção especificada, a estratégia envia uma ordem a mercado.
4. A posição é fechada quando o stop loss ou take profit é atingido.
