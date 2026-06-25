# Estratégia OzFx Accelerator Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especializado MetaTrader *OzFx (edição de barabashkakvn)* para a API de estratégia de alto nível do StockSharp.
- Combina o oscilador Acceleration/Deceleration (AC) com um limiar estocástico para entrar em camadas nas tendências.
- Projetado para negociação forex de estilo discricionário onde as ordens são dimensionadas em lotes e a proteção é expressa em pips.

## Lógica de negociação
1. Calcular o oscilador Acceleration/Deceleration como a diferença entre o Awesome Oscillator e sua SMA de 5 períodos.
2. Assinar um oscilador estocástico com períodos `%K`, `%D` e de suavização configuráveis.
3. Quando um novo candle fecha, avaliar os dois valores de AC mais recentes junto com o nível estocástico:
   - **Configuração comprada**: `%K` cruza acima do nível configurado, o AC atual é positivo e subindo enquanto o valor anterior era negativo.
   - **Configuração vendida**: `%K` cruza abaixo do nível, o AC atual é negativo e caindo enquanto o valor anterior era positivo.
4. Com um sinal válido, abrir até cinco ordens a mercado de tamanho igual. A primeira camada espelha o EA original ao iniciar sem stop/alvo, enquanto as camadas restantes herdam o stop loss configurado e take profits escalonados.
5. A gestão de saídas emula o comportamento original do flag `modok`:
   - Quando trailing stops estão desabilitados, a estratégia apenas ajusta stops para o ponto de equilíbrio após uma saída rentável, e fechará todas as camadas se a combinação estocástico/AC virar contra a posição.
   - Com trailing stops habilitados, o stop segue o preço uma vez que o movimento supera *TrailingStop + TrailingStep*, e a mesma reversão de momentum fecha a pilha.

## Escalonamento de posição e alvos
- Posições compradas colocam quatro camadas adicionais com take profits em `entry + TakeProfit * i` para `i = 1..4`. Vendidos espelham isso abaixo do preço.
- Stop losses (quando configurados) são anexados a cada camada, exceto a primeira, exatamente como o script MT5.
- Take profits parciais atualizam o flag interno para que a próxima campanha comece imediatamente no estado "modok = true", desbloqueando a proteção de ponto de equilíbrio para a camada inicial.

## Gestão de risco
- `StopLossPips` e `TakeProfitPips` são definidos em pips. A estratégia os converte usando o tamanho de tick do instrumento e a precisão de dígitos (`5` ou `3` pares decimais contam como pips fracionários).
- `TrailingStopPips = 0` desativa a lógica de trailing e habilita apenas o ajuste de ponto de equilíbrio após um take profit. Qualquer valor positivo ativa o bloco de trailing descrito acima.
- Todas as saídas são executadas com ordens a mercado quando o range do candle cruza os níveis de stop ou alvo armazenados, correspondendo ao comportamento do especialista original que dependia de ordens protetoras do lado do broker.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Tamanho de lote por camada. | `0.1` |
| `StopLossPips` | Distância para ordens stop protetoras (pips). | `100` |
| `TakeProfitPips` | Distância base entre take profits em camadas (pips). | `50` |
| `TrailingStopPips` | Distância de trailing stop em pips (0 desabilita trailing). | `50` |
| `TrailingStepPips` | Distância adicional antes de avançar o trailing stop. | `5` |
| `KPeriod` | Lookback do `%K` estocástico. | `5` |
| `DPeriod` | Suavização do `%D` estocástico. | `3` |
| `SmoothingPeriod` | Suavização final aplicada ao `%K`. | `3` |
| `StochasticLevel` | Limite separando regimes de alta/baixa. | `50` |
| `CandleType` | Série de candles fonte para cálculos. | `Período 4h` |

## Notas de implementação
- Sinais, atualizações de trailing e saídas protetoras são processadas em candles completados para permanecer consistente com o EA que dispara em novas barras.
- O indicador AC é reproduzido vinculando o Awesome Oscillator e subtraindo sua SMA de 5 períodos; nenhum buffer de indicador de baixo nível é acessado.
- A conversão de pips se adapta automaticamente a símbolos forex de 4/5 dígitos e recorre a um padrão razoável quando metadados de tamanho de tick estão ausentes.
- A estratégia mantém um registro interno de entradas em camadas para que take profits parciais e ajustes de stop correspondam à lógica por posição da versão MetaTrader.
- Como o StockSharp executa saídas via ordens a mercado, os trades são nivelados quando a máxima/mínima do candle perfura os níveis armazenados de stop ou alvo, em vez de esperar gatilhos do lado do broker.
