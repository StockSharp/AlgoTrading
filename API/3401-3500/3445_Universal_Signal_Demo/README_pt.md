# Demonstração de sinal universal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista MetaTrader 5 "Universal Signal" usando StockSharp APIs de alto nível. Avalia oito padrões de mercado ponderados e os agrega em uma única pontuação composta. Quando a pontuação ultrapassa limites configuráveis, a estratégia abre ou fecha posições longas e curtas, opcionalmente usando ordens de limite pendentes que expiram após um determinado número de barras.

## Parâmetros de Estratégia
- `CandleType` – dados de velas usados para a análise.
- `SignalThresholdOpen` – pontuação composta mínima necessária para abrir uma posição.
- `SignalThresholdClose` – pontuação adversária necessária para sair de uma posição existente.
- `PriceLevel` – compensação de preço para colocação de entradas de limite pendentes (0 significa execução de mercado).
- `StopLevel` / `TakeLevel` – distâncias absolutas de stop-loss e take-profit usadas pelo módulo de proteção integrado.
- `SignalExpiration` – número de barras após as quais as entradas pendentes ainda ativas são canceladas.
- `Pattern0Weight`… `Pattern7Weight` – peso aplicado a cada padrão antes da agregação.
- `UniversalWeight` – multiplicador final aplicado à soma de todas as contribuições do padrão.
- `ShortMaPeriod`, `LongMaPeriod`, `RsiPeriod`, `BollingerPeriod`, `BollingerWidth`, `TrendSmaPeriod`, `VolumeSmaPeriod` – configurações do indicador usadas nas verificações de padrão.

## Lógica de negociação
1. Assine o fluxo de vela configurado e vincule EMA, RSI, MACD sinal, Bollinger bandas e SMAs de suporte.
2. Após cada vela concluída, calcule oito padrões booleanos (alinhamento de tendência, RSI impulso, MACD histograma, Bollinger posicionamento, direção da vela e expansão de volume).
3. Multiplique cada padrão pelo seu peso, some as contribuições e aplique o peso global para obter a pontuação final.
4. Feche as posições abertas quando a pontuação ultrapassar o limite de fechamento na direção oposta.
5. Abra novas posições longas ou curtas quando a pontuação exceder o limite de abertura. Se `PriceLevel` for positivo, envie uma ordem de limite compensada pela distância configurada e cancele-a automaticamente após `SignalExpiration` barras.
6. `StartProtection` define níveis fixos de stop-loss e take-profit para todas as posições usando os auxiliares de gerenciamento de risco de StockSharp.

A conversão mantém o fluxo de trabalho de ponderação flexível do especialista MQL5 original, ao mesmo tempo que segue as convenções de codificação StockSharp e o processamento baseado em indicadores.
