# Estratégia RSI Levels
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia RSI Levels** é um port direto do expert advisor "RSI Levels" do MetaTrader 5. O sistema observa um único símbolo no timeframe selecionado e age quando o Relative Strength Index (RSI) cruza limiares configuráveis de sobrecompra e sobrevenda. A estratégia assume que o mercado fará reversão à média depois que o RSI entra em uma zona extrema. Quando o indicador cai abaixo do nível de sobrevenda, uma posição comprada é iniciada; quando sobe acima do nível de sobrecompra, uma posição vendida é aberta. Apenas uma posição é mantida por vez e qualquer exposição oposta é fechada antes de uma nova entrada.

## Lógica de negociação

1. **Cálculo RSI:** o RSI é calculado no timeframe de trabalho com período configurável. A barra atual deve estar concluída antes que sinais sejam avaliados.
2. **Entrada comprada:** disparada quando o RSI atual fecha abaixo do nível de sobrevenda enquanto o RSI anterior estava acima desse nível. Se existir uma posição vendida, ela é fechada imediatamente; caso contrário, uma nova compra é aberta usando dimensionamento baseado em risco.
3. **Entrada vendida:** disparada quando o RSI atual fecha acima do nível de sobrecompra enquanto o RSI anterior estava abaixo desse nível. Qualquer exposição comprada existente é fechada primeiro e então uma nova venda é criada.
4. **Stop Loss:** um stop fixo é colocado a uma distância configurável em pontos do símbolo a partir do preço de entrada. Se o stop for zero, ele é desabilitado.
5. **Take Profit:** um take-profit fixo é colocado a uma distância configurável em pontos do símbolo a partir do preço de entrada. Se o take-profit for zero, ele é desabilitado.
6. **Gestão de posição:** apenas uma posição pode estar aberta por vez. Após o fechamento de uma posição, o estado interno é reiniciado para que o próximo sinal comece limpo.

## Dimensionamento de posição

O tamanho da posição é calculado a partir do *Risk % per Trade* configurado. O algoritmo multiplica o patrimônio da carteira pelo percentual de risco e divide o capital em risco pelo valor monetário da distância do stop (pontos de stop x preço do passo). O volume resultante é arredondado para baixo até o passo de lote negociável mais próximo e limitado pelo volume mínimo/máximo fornecido pelo ativo. Quando as informações de mercado necessárias (price step ou step price) faltam, a estratégia registra um aviso e usa o menor volume disponível.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Timeframe de 1 hora | Timeframe usado para assinatura de candles e cálculo RSI. |
| `RsiPeriod` | 14 | Número de períodos do indicador RSI. |
| `OverboughtLevel` | 70 | Limiar RSI que define a zona de sobrecompra. |
| `OversoldLevel` | 30 | Limiar RSI que define a zona de sobrevenda. |
| `RiskPercent` | 2 | Percentual do patrimônio da carteira arriscado em cada operação. |
| `StopLossPoints` | 500 | Distância de stop-loss expressa em pontos do símbolo. Defina como zero para desabilitar. |
| `TakeProfitPoints` | 1000 | Distância de take-profit expressa em pontos do símbolo. Defina como zero para desabilitar. |

## Notas práticas

- A estratégia exige `PriceStep`, `StepPrice`, `MinVolume` e `VolumeStep` configurados no ativo para dimensionamento de risco preciso. Se algum desses valores faltar, padrões conservadores são usados e avisos são registrados.
- A lógica usa `SubscribeCandles` e `Bind` para obter valores de indicadores sem puxar dados manualmente, seguindo as diretrizes da API de alto nível.
- Stops e metas são avaliados em dados de candles; slippage e gaps podem causar execuções longe do preço pretendido.
- Como o sistema reage apenas quando um candle é concluído, ele é adequado para timeframes como M15, H1 ou H4. Timeframes menores podem exigir filtros adicionais para reduzir ruído.

## Uso

1. Anexe a estratégia ao ativo e carteira desejados.
2. Ajuste os limiares RSI e controles de risco para corresponder à volatilidade do instrumento.
3. Inicie a estratégia e monitore o log para avisos relacionados a informações ausentes do símbolo.
4. Revise resultados de operações e ajuste distâncias de stop/take-profit ou níveis RSI conforme o desempenho.

Esta implementação StockSharp espelha o comportamento original do MetaTrader enquanto expõe configuração e gestão de risco por parâmetros padrão de estratégia.
