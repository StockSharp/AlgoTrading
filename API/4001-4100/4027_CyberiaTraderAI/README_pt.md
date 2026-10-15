# Estratégia de IA do Cyberia Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão StockSharp do consultor especialista **CyberiaTrader.mq4 (build 8553)**. O programa MQL original mistura um
mecanismo de probabilidade com uma coleção de filtros de tendência opcionais. A porta C# mantém a mesma estrutura: um modelo de probabilidade pesquisa
para o período de amostragem mais confiável e então opcionais MACD, EMA e filtros de reversão podem vetar negociações.

## Indicadores e Modelo Interno

- **Probability Engine** – itera períodos de amostragem de candidatos (`MaxPeriod`) e avalia `SamplesPerPeriod` segmentos históricos.
Para cada período o mecanismo calcula:
  - Direção da decisão (compra/venda/flat) com base em velas consecutivas de alta/baixa de um minuto espaçadas pelo período de amostragem.
  - Amplitudes médias de "possibilidade" para resultados de compra, venda e indefinidos e a parcela de resultados bem-sucedidos acima
`SpreadThreshold`.
  - Índices de sucesso que selecionam o período de melhor desempenho.
- **EMA Filtro de tendência** – média móvel exponencial opcional (`EnableMa`) que bloqueia negociações contra a inclinação atual.
- **MACD Filtro** – convergência/divergência de média móvel opcional (`EnableMacd`) que proíbe a negociação contra o impulso.
- **Detector de reversão** – detector de pico opcional (`EnableReversalDetector`) que inverte as permissões quando as probabilidades aumentam
`ReversalFactor` múltiplos de suas médias.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `MaxPeriod` | Maior passo de amostragem inspecionado pelo mecanismo de probabilidade. |
| `SamplesPerPeriod` | Número de segmentos processados por período candidato (espelha o MQL `ValuesPeriodCount`). |
| `SpreadThreshold` | Amplitude mínima que conta como um resultado de probabilidade bem-sucedido. |
| `EnableCyberiaLogic` | Ativa as opções de probabilidade da Cyberia que podem desativar compras ou vendas. |
| `EnableMacd` | Ativa o filtro de impulso MACD. |
| `EnableMa` | Ativa o filtro de inclinação EMA. |
| `EnableReversalDetector` | Ativa as permissões de alternância do detector de reversão em picos extremos. |
| `MaPeriod` | Comprimento EMA usado pelo filtro de tendência. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD EMA rápida, EMA lenta e períodos de sinal. |
| `ReversalFactor` | Multiplicador que aciona o detector de reversão. |
| `CandleType` | Tipo de dados Candle processado pelo modelo (padrão 1 minuto). |
| `TakeProfitPercent` | Take Profit de proteção opcional expresso como uma porcentagem. |
| `StopLossPercent` | Stop loss de proteção opcional expresso em porcentagem. |

## Lógica de negociação

1. Cada vela concluída atualiza a fila do histórico local e recalcula estatísticas de probabilidade para cada período de 1 a
`MaxPeriod`. O período com maior taxa de sucesso torna-se a configuração ativa.
2. A lógica da Cyberia define sinalizadores `DisableBuy`/`DisableSell` usando as mesmas comparações do código MQL:
   - Compara as possibilidades médias de compra/venda e suas variantes ponderadas pelo sucesso quando o período aumenta ou diminui.
   - Desativa entradas se novas possibilidades excederem o dobro de suas médias de sucesso.
3. Filtros opcionais são aplicados na ordem: MACD, EMA inclinação e, em seguida, o detector de reversão.
4. Quando nenhuma posição está aberta, a estratégia entra se a decisão atual for de compra (ou venda) e a possibilidade correspondente exceder
sua média de sucesso enquanto a direção oposta está desativada.
5. Enquanto existir uma posição, o código verifica as mesmas condições para fechar quando o mecanismo de probabilidade muda ou quando os filtros proíbem a posição.
direção atual.
6. `StartProtection` reproduz os blocos originais de gerenciamento de dinheiro quando parâmetros de risco diferentes de zero são fornecidos.

## Notas sobre a conversão

- A porta mantém os cálculos estatísticos, mas substitui a verificação de spread baseada em ticks pelo configurável `SpreadThreshold`.
- O dimensionamento automático de lote e o diagnóstico de equilíbrio do script MQL não são implementados; O volume de StockSharp é controlado por meio de `Volume`.
- Os módulos MoneyTrain e Pipsator são condensados na lógica unificada de entrada/saída descrita acima para corresponder ao uso de API de alto nível.
- A estratégia adiciona desenho de gráfico para velas, EMA e MACD para facilitar a validação no designer.
