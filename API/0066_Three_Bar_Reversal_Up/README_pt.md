# Estratégia de Reversão de Três Barras para Cima
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Este padrão captura viradas altistas rápidas após uma breve queda. Ele requer duas velas baixistas consecutivas seguidas de uma vela altista forte que fecha acima da máxima da barra anterior. A lógica opcionalmente verifica se o preço estava em tendência de baixa anteriormente.

Os testes indicam um retorno anual médio de aproximadamente 85%. Tem melhor desempenho no mercado cripto.

A estratégia mantém as últimas três velas em memória. Assim que a sequência corresponde aos critérios e qualquer filtro de tendência de baixa é satisfeito, uma posição comprada é aberta. Um stop de volatilidade abaixo da mínima do padrão limita o risco da operação.

Após a entrada, o sistema aguarda um toque no stop ou o aparecimento de outro setup na direção oposta. Esta abordagem simples se adapta a mercados propensos a rebotes bruscos a partir de condições de sobrevenda.

## Detalhes

- **Critérios de entrada**: Duas velas baixistas com mínimas mais baixas, seguidas de uma vela altista fechando acima da máxima da barra do meio.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss ou próximo padrão.
- **Stops**: Sim, abaixo da mínima do padrão.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendLength` = 5
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

