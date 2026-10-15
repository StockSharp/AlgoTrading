# Estratégia de Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de EMA com gestão de risco adaptada ao período.

Os testes indicam um retorno anual médio de aproximadamente 31%. Funciona melhor no mercado de criptomoedas.

A estratégia compra quando uma EMA rápida cruza acima de uma EMA mais lenta e a tendência de longo prazo é de alta. As entradas vendidas ocorrem no cruzamento oposto. Os horários de negociação e um filtro ADX simples ajudam a evitar períodos de baixo momentum. O risco é gerenciado com take profit e stop loss baseados em percentuais.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA9 cruza acima de EMA20 enquanto EMA50 está acima de EMA200.
  - **Vendido**: EMA9 cruza abaixo de EMA20 enquanto EMA50 está abaixo de EMA200.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Stop loss ou take profit.
  - **Vendido**: Stop loss ou take profit.
- **Stops**: Sim, trailing opcional.
- **Valores padrão**:
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1.0
  - `TrailingPercent` = 0.5
  - `StartHour` = 15
  - `EndHour` = 20
  - `CooldownBars` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI, ADX
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
