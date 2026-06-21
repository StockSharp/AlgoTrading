# Estratégia Terminator V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador de Convergência/Divergência de Médias Móveis (MACD) para operar em ambas as direções. Uma posição comprada é aberta quando a linha MACD cruza acima da sua linha de sinal. Uma posição vendida é aberta quando a linha MACD cruza abaixo da sua linha de sinal. As posições são protegidas por níveis fixos de stop-loss e take-profit, enquanto um trailing stop opcional pode garantir lucros durante tendências fortes.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `MACD` cruza acima da linha de sinal.
  - **Vendido**: `MACD` cruza abaixo da linha de sinal.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Nível de stop-loss ou take-profit é atingido.
  - Trailing stop é acionado.
- **Stops**: Sim, inclui stop-loss, take-profit e trailing stop opcional.
- **Valores padrão**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 26
  - `SignalPeriod` = 1
  - `TakeProfit` = 500 pontos
  - `StopLoss` = 2500 pontos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
