# Estratégia de couro cabeludo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Gonna Scalp é um consultor especialista MetaTrader de alta frequência portado para o StockSharp API de alto nível. O sistema procura entradas rápidas de reversão à média em um gráfico de curto prazo, respeitando ao mesmo tempo a tendência dominante do mercado. A confirmação é produzida por um mecanismo de votação que avalia o momentum, CCI, ATR, oscilador estocástico e filtros MACD antes de permitir uma negociação. Apenas uma posição pode ser aberta por vez e cada negociação é protegida por distâncias fixas de stop-loss e take-profit expressas em MetaTrader pontos.

## Lógica de negociação

1. **Preparação de indicadores**
   - Médias móveis ponderadas rápidas e lentas (WMA) calculadas com base no preço típico.
   - Momentum (período 14) avaliado no período de negociação e convertido em distância absoluta do valor neutro 100.
   - Commodity Channel Index (período 20) e Average True Range (período 12) usados como filtros direcionais.
   - Oscilador Stochastic %K/%D (5/3/3) e MACD (26/12/9) processados na mesma série de velas.
2. **Votação de sinal**
   - Cada indicador contribui com um voto para o lado altista ou baixista quando sua leitura atual apoia a tendência identificada no código MetaTrader original.
   - A estratégia coleta três distâncias de impulso recentes e exige que pelo menos uma delas exceda um limite configurável antes de permitir uma nova negociação.
   - Verificações adicionais da estrutura exigem que a mínima da barra há duas velas permaneça abaixo da máxima da barra anterior para posições compradas (condição de espelho para posições vendidas).
3. **Execução de pedido**
   - Quando os votos de alta excedem os votos de baixa e todos os filtros concordam, a estratégia abre uma posição longa usando o tamanho de lote configurado.
   - Quando os votos de baixa dominam os votos de alta e o filtro de momentum aprova, uma posição curta é aberta.
4. **Gerenciamento de riscos**
   - Cada negociação aberta é acompanhada por distâncias fixas de stop-loss e take-profit medidas em MetaTrader pontos e traduzidas em etapas de preço do instrumento.
   - A lógica de proteção fecha a posição na vela atual quando um dos níveis for violado.

## Parâmetros principais

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `TradeVolume` | Tamanho base do pedido em lotes após alinhamento de volume. | `0.01` |
| `FastMaPeriod` | Comprimento do filtro WMA rápido. | `1` |
| `SlowMaPeriod` | Comprimento do filtro WMA lento. | `5` |
| `MomentumPeriod` | Número de barras usadas pelo indicador de momentum. | `14` |
| `MomentumBuyThreshold` | Desvio de momento absoluto mínimo necessário para entradas longas. | `0.3` |
| `MomentumSellThreshold` | Desvio de momento absoluto mínimo exigido para entradas curtas. | `0.3` |
| `StopLossSteps` | Distância de stop-loss expressa em MetaTrader pontos. | `200` |
| `TakeProfitSteps` | Distância de lucro expressa em MetaTrader pontos. | `200` |
| `CandleType` | Prazo usado para todos os indicadores (o padrão é velas de 5 minutos). | `M5` |

## Notas de uso

- Alinhe o volume da estratégia com o instrumento negociado ajustando `TradeVolume`; a implementação normaliza automaticamente para a etapa do lote de troca.
- Os parâmetros stop-loss e take-profit operam em MetaTrader pontos. Eles são convertidos em unidades de preço do instrumento com base na precisão do instrumento.
- São necessárias pelo menos três velas concluídas antes que a lógica de votação possa produzir sinais devido ao buffer histórico de impulso.
- A estratégia evita deliberadamente a pirâmide; uma nova negociação não é aberta até que a posição anterior tenha sido fechada pela gestão de risco ou por um sinal oposto.
- Você pode conectar a estratégia aos gráficos StockSharp para visualizar as séries WMAs, estocásticas e MACD para validação de sinal.
