# Estratégia Aleatória
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista "RandomT". O EA original espera por uma oscilação ZigZag que coincide com um fractal confirmado e então filtra a entrada com uma comparação MACD. A versão StockSharp mantém o mesmo processo de decisão: observa um número configurável de velas (`BarWatch`), confirma que um fractal de cinco barras marca o extremo de oscilação mais recente e só negocia quando a linha principal MACD está acima ou abaixo da linha de sinal na mesma barra histórica.

## Lógica de negociação
- Construa buffers de velas rolantes e calcule o sinal MACD em cada barra finalizada do período de tempo selecionado (`CandleType`).
- Observe `Shift` barras no passado e verifique se essa barra forma um fractal para cima ou para baixo (duas velas de cada lado).
- Valide o fractal em relação à ação do preço circundante: a máxima deve ser o maior valor, ou a mínima, o menor valor, dentro da janela de lookback `BarWatch`. Isso reflete a confirmação de swing do ZigZag usada pela versão MetaTrader.
- Para uma configuração curta, o valor principal MACD deve ser maior que o valor do sinal na barra deslocada. Para uma configuração longa, a comparação oposta deve ser verdadeira.
- Quando surge um sinal, a estratégia utiliza uma única ordem de mercado cujo volume neutraliza qualquer posição oposta antes de abrir a nova negociação.

## Gerenciamento de trailing stop
- O bloco final é ativado somente quando `UseTrailingProfit` está ativado e o lucro flutuante (convertido por meio de `PriceStep` e `StepPrice`) excede `MinProfit`.
- A distância final é medida em faixas de preço. Quando `AutoStopLevel` é `true`, o mecanismo usa `StartStopLevelPoints`; caso contrário, usa `StopLevelPoints`.
- Para posições longas, o stop segue `ClosePrice - distance`, para posições curtas segue `ClosePrice + distance`. Se a vela ultrapassar o nível de stop, a estratégia fecha a negociação com uma ordem de mercado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho base da negociação em lotes usados para cada entrada. |
| `BarWatch` | Número de barras usadas para validar que um fractal também é um extremo de oscilação ZigZag. |
| `Shift` | Número de barras no histórico que são avaliadas quanto a sinais. Deve ficar em 2 para fractais clássicos. |
| `UseTrailingProfit` | Ativa a lógica de trailing stop. |
| `AutoStopLevel` | Muda a distância final para `StartStopLevelPoints`. |
| `StartStopLevelPoints` | Distância de fuga alternativa (pontos). |
| `StopLevelPoints` | Distância de fuga primária (pontos). |
| `MinProfit` | Lucro flutuante mínimo (moeda da conta) exigido antes da aplicação do trailing. |
| `CandleType` | Prazo usado para cálculos de velas e indicadores. |
| `MacdFastLength` | Período EMA rápido para o filtro MACD. |
| `MacdSlowLength` | Período EMA lento para o filtro MACD. |
| `MacdSignalLength` | Período de sinal EMA para o filtro MACD. |

## Notas
- A estratégia calcula fractais internamente (duas barras de cada lado) e reutiliza o resultado para validação ZigZag, correspondendo de perto aos buffers acessados no código MQL.
- A confirmação do ZigZag é aproximada verificando as velas `BarWatch` circundantes em vez de executar novamente o indicador MetaTrader completo, o que mantém o comportamento determinístico dentro de StockSharp.
- O lucro do trailing stop é derivado de `PriceStep` e `StepPrice` do instrumento. Verifique esses valores para o seu instrumento antes de executar a estratégia.
