# Estratégia MACD Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port para StockSharp do sistema MetaTrader 5 "MACD Stochastic". Combina um cruzamento clássico de MACD com um filtro de confirmação estocástico opcional e opera apenas durante três sessões intradiárias configuráveis. Cada posição usa controles de risco baseados em pips com lógica de trailing stop opcional que pode mover o stop em direção ao break-even assim que a negociação atingir um lucro especificado.

## Indicadores
- **MACD (Convergência/Divergência de Médias Móveis)** – gera os sinais primários de reversão de tendência rastreando o cruzamento entre as médias móveis exponenciais rápida e lenta e sua linha de sinal.
- **Oscilador Stochastic** – filtro opcional que confirma os sinais do MACD verificando se as linhas %K e %D cruzaram recentemente na mesma direção que a negociação.

## Lógica de Trading
### Entradas Compradas
1. A linha principal do MACD cruza acima da linha de sinal e ambas as linhas estão abaixo de zero, indicando uma possível reversão altista.
2. A posição mais recente foi aberta em uma barra anterior (apenas uma entrada por barra é permitida).
3. A hora atual (hora local do instrumento) cai dentro de uma das sessões de trading configuradas.
4. Se o filtro estocástico estiver habilitado, o valor atual de %K deve estar acima de %D e o valor de *StochasticBarsToCheck* barras atrás deve mostrar a relação oposta (%K abaixo de %D), confirmando um cruzamento altista recente.

### Entradas Vendidas
1. A linha principal do MACD cruza abaixo da linha de sinal e ambas as linhas estão acima de zero, sinalizando uma reversão baixista.
2. A estratégia não tem posição aberta e não abriu uma negociação na barra atual.
3. A hora atual está dentro de pelo menos uma janela de sessão ativa.
4. Quando o filtro estocástico está ativo, o %K atual deve estar abaixo de %D e o valor de *StochasticBarsToCheck* barras atrás deve estar acima de %D, confirmando um cruzamento baixista.

### Gestão de Posição
- **Stop-Loss / Take-Profit** – os níveis iniciais são calculados em pips usando o passo de preço do instrumento. A implementação ajusta automaticamente as cotações de 3 e 5 dígitos multiplicando o passo de preço por 10 para aproximar um pip padrão.
- **Trailing Stop** – assim que a posição tiver ganho pelo menos *WhenSetNoLossStopPips* de lucro, o stop pode seguir o mercado:
  - Posições compradas requerem um stop inicial. O stop é incrementado por *TrailingStopPips* sempre que permanecer pelo menos *TrailingStepPips + TrailingStopPips* afastado do fechamento atual e acima do buffer de break-even definido por *NoLossStopPips*.
  - Posições vendidas movem o stop para baixo sob restrições semelhantes. Se não existir stop inicial, o algoritmo pode colocar um stop de break-even em *NoLossStopPips* assim que o preço tiver avançado o suficiente.
- **Ativação de Take-Profit / Stop** – se o máximo ou mínimo de um candle tocar os níveis de saída armazenados, a posição é fechada a mercado e o estado interno é reiniciado.

## Parâmetros
- **MacdFastPeriod, MacdSlowPeriod, MacdSignalPeriod** – configuração do MACD.
- **UseStochastic** – habilita o filtro de confirmação estocástico.
- **StochasticBarsToCheck, StochasticLength, StochasticKPeriod, StochasticDPeriod** – configurações do oscilador estocástico.
- **Volume** – tamanho da negociação em lotes.
- **StopLossPips, TakeProfitPips** – distâncias em pips para saídas iniciais.
- **TrailingStopPips, TrailingStepPips** – configuração do trailing stop.
- **NoLossStopPips, WhenSetNoLossStopPips** – limiares de break-even e ativação para a lógica de trailing.
- **MaxPositions** – mantido por compatibilidade; StockSharp trabalha com posições líquidas, então a estratégia mantém apenas uma posição aberta por vez.
- **Session1/2/3 Start-End** – janelas intradiárias quando o trading é permitido. Defina início e fim como `00:00` para desabilitar uma janela.
- **CandleType** – série de candles usada para geração de sinais.

## Notas Adicionais
- As entradas são processadas apenas em candles concluídos. A estratégia não abrirá mais de uma posição por candle, refletindo o comportamento original do EA.
- Distâncias baseadas em pips dependem do passo de preço do instrumento. Certifique-se de que os metadados do símbolo forneçam um `PriceStep` válido.
- O filtro estocástico armazena um pequeno histórico rotativo para avaliar valores passados sem usar acesso de indicador de baixo nível, cumprindo com as melhores práticas da API de alto nível.
