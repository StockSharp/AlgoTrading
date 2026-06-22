# Estratégia Labouchere EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um cruzamento de Oscilador Estocástico com uma sequência de gestão monetária Labouchere. O indicador Estocástico gera sinais quando %K cruza %D. O sistema Labouchere ajusta o volume de negociação após cada posição fechada: perdas adicionam um novo elemento igual à soma do primeiro e do último número da sequência, enquanto lucros removem esses elementos.

As operações são realizadas apenas em velas concluídas. A sequência pode reiniciar opcionalmente quando todos os números são removidos. Um filtro de tempo permite operar dentro de uma janela intradiária específica, e sinais opostos podem fechar posições existentes. Níveis fixos de stop-loss e take-profit (em passos de preço) são suportados.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %K cruza acima de %D.
  - **Vendido**: %K cruza abaixo de %D.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Saída opcional por sinal oposto.
  - Stop-loss e take-profit fixos (se configurados).
- **Stops**: Sim.
- **Gestão monetária**: Sequência Labouchere.
- **Valores padrão**:
  - `LotSequence` = "0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01"
  - `NewRecycle` = true
  - `StopLoss` = 40
  - `TakeProfit` = 50
  - `IsReversed` = false
  - `UseOppositeExit` = false
  - `UseWorkTime` = false
  - `StartTime` = 00:00
  - `StopTime` = 24:00
  - `KPeriod` = 10
  - `DPeriod` = 190
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
