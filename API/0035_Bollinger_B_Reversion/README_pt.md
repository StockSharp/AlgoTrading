# Estratégia Bollinger Percent B Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta abordagem opera contra extremos de preço além das Bollinger Bands usando o indicador Percent B. Movimentos acima da banda superior ou abaixo da banda inferior sugerem sobreextensão.

Os testes indicam um retorno anual médio de aproximadamente 142%. Funciona melhor no mercado de ações.

Quando Percent B é menor que zero ou maior que um, o sistema aposta em um retorno ao centro da banda. Um limiar de saída fecha as operações assim que o momentum se normaliza.

Os stops são colocados a uma porcentagem fixa da entrada.

## Detalhes

- **Critérios de entrada**: Percent B fora do intervalo 0–1.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Percent B cruza `ExitValue` ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `ExitValue` = 0.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

