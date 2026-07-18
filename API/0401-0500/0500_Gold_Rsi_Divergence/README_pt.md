# Estratégia de Divergência RSI em Ouro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Divergência RSI em Ouro faz scalping em ouro identificando divergências de alta e baixa entre o preço e o Índice de Força Relativa (RSI).
Quando o preço marca uma nova mínima mas o RSI imprime uma mínima mais alta, a estratégia busca comprar.
Por outro lado, quando o preço marca uma nova máxima mas o RSI imprime uma máxima mais baixa, a estratégia vende.
Ambas as configurações são confirmadas apenas se dois pivôs ocorrem dentro de um intervalo de barras configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço com mínima mais baixa, RSI com mínima mais alta, RSI < 40.
  - **Vendido**: Preço com máxima mais alta, RSI com máxima mais baixa, RSI > 60.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Utiliza stop loss e take profit.
- **Stops**: Stop loss e take profit fixos em pips.
- **Valores padrão**:
  - `RsiLength` = 60
  - `StopLossPips` = 11
  - `TakeProfitPips` = 33
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
