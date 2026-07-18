# Estratégia de Execução Instantânea
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra imediatamente em uma única posição na primeira vela concluída e a gerencia com regras simples de lucro e risco. A direção da posição é selecionável por parâmetros. Uma vez que uma operação é aberta, o algoritmo rastreia lucro e perda e pode seguir o preço para proteger os ganhos.

A lógica reproduz o comportamento do script MQL original que permitia a execução instantânea de ordens de mercado com valores opcionais de take profit, stop loss e trailing stop.

## Detalhes

- **Critérios de entrada**: abre uma posição de mercado na primeira vela finalizada após o início. A direção é definida pelo parâmetro `Direction`.
- **Comprado/Vendido**: Ambos os lados suportados.
- **Critérios de saída**:
  - Take profit atingido.
  - Stop loss atingido.
  - Trailing stop ativado e o preço atinge o nível de trailing.
- **Stops**: Take profit, stop loss e trailing stop estão disponíveis.
- **Valores padrão**:
  - `TakeProfit` = 70 unidades de preço.
  - `StopLoss` = 0 (desativado).
  - `TrailingStart` = 5 unidades de preço.
  - `TrailingSize` = 5 unidades de preço.
- **Filtros**:
  - Categoria: Utilitário
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
