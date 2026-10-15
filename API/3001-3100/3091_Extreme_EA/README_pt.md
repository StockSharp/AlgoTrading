# Extreme EA (Conversão para StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Extreme EA** é um Consultor Especialista seguidor de tendência originalmente escrito para MetaTrader. Combina duas médias móveis com um filtro de Índice de Canal de Commodity (CCI) e um módulo de gerenciamento adaptativo de dinheiro. Este porte mantém a lógica de negociação intacta enquanto expõe todos os controles importantes através da API de alto nível do StockSharp. A estratégia opera apenas em velas fechadas e é compatível com múltiplos períodos executando as médias móveis e o CCI em assinaturas de velas independentes.

## Visão Geral da Estratégia

1. **Filtro de tendência:** Duas médias móveis são calculadas no `MaCandleType` configurável. A média rápida rastreia o momentum de curto prazo enquanto a média lenta define a inclinação da tendência dominante. A estratégia verifica a inclinação da média lenta usando os dois valores anteriores para imitar os deslocamentos do array `CopyBuffer` do código MQL.
2. **Filtro de momentum:** O CCI é avaliado em seu próprio período (`CciCandleType`) e fonte de preço. O último valor completado é armazenado em cache e reutilizado até que uma nova vela CCI apareça, o que corresponde ao comportamento dos buffers do MetaTrader.
3. **Regras de entrada:**
   - Entrar comprado quando a MA lenta sobe, a MA rápida sobe e o CCI cai abaixo do nível inferior.
   - Entrar vendido quando a MA lenta cai, a MA rápida cai e o CCI sobe acima do nível superior.
4. **Regras de saída:**
   - Fechar todos os comprados se a MA lenta parar de subir.
   - Fechar todos os vendidos se a MA lenta parar de cair.

## Gerenciamento de Risco

- **MaximumRisk** controla o tamanho de posição alvo baseado no patrimônio atual do portfólio e no último preço. Se o volume calculado for zero ou os valores do portfólio não estiverem disponíveis, a estratégia recorre ao `Volume` configurado ou ao mínimo da bolsa.
- **DecreaseFactor** reduz o volume calculado após duas ou mais negociações perdedoras consecutivas. A redução espelha a fórmula original `lot = lot - lot * losses / DecreaseFactor`.
- **HistoryDays** limita por quanto tempo uma sequência de perdas é lembrada. Se uma negociação de fechamento ocorrer após o número especificado de dias, a sequência é reiniciada antes de aplicar a redução.
- **MaxPositions** limita a pirâmide restringindo a exposição líquida por direção. Quando o limite é atingido, novas entradas são suprimidas até que a exposição diminua.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `MaximumRisk` | `decimal` | `0.05` | Fração do patrimônio usada para dimensionar cada nova negociação. |
| `DecreaseFactor` | `decimal` | `6` | Fator de redução por sequência de perdas. Definir como `0` para desativar. |
| `HistoryDays` | `int` | `60` | Número de dias preservados ao contar perdas consecutivas. |
| `MaxPositions` | `int` | `3` | Máximo de entradas simultâneas por direção. |
| `FastMaPeriod` | `int` | `15` | Período para a média móvel rápida. |
| `SlowMaPeriod` | `int` | `75` | Período para a média móvel lenta. |
| `CciPeriod` | `int` | `12` | Comprimento de lookback para o CCI. |
| `CciUpperLevel` | `decimal` | `50` | Limiar CCI superior usado para vendidos. |
| `CciLowerLevel` | `decimal` | `-50` | Limiar CCI inferior usado para comprados. |
| `MaCandleType` | `DataType` | `15m` | Período para ambas as médias móveis e execução. |
| `CciCandleType` | `DataType` | `30m` | Período para o filtro CCI. |
| `MaMethod` | `MaMethod` | `Exponential` | Método de suavização (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaPriceMode` | `AppliedPriceMode` | `Median` | Entrada de preço para as médias móveis. |
| `CciPriceMode` | `AppliedPriceMode` | `Typical` | Entrada de preço para o CCI. |

## Notas de Implementação

- A estratégia assina o período das médias móveis uma vez e opcionalmente uma segunda assinatura para o CCI. Quando ambos os períodos coincidem, uma única assinatura alimenta ambos os componentes, reproduzindo o fluxo de trabalho original de gráfico único.
- Os valores anteriores dos indicadores são armazenados em campos privados para emular as comparações `ma_slow_array[1]`, `ma_slow_array[2]` e `ma_fast_array[0]` sem recorrer a buffers de indicadores manuais.
- O dimensionamento de posição é normalizado contra o passo de volume do instrumento, mínimo e máximo para evitar ordens rejeitadas.
- O módulo de risco registra os preços de entrada e saída para estimar o PnL realizado por posição completada, o que substitui o loop `HistoryDealGet` usado no MetaTrader.

## Diferenças em relação à Versão MQL

- Funções específicas do MetaTrader como `FreeMarginCheck`, `MarginCheck` e `HistorySelect` são aproximadas com as métricas de portfólio do StockSharp e o rastreador interno de sequências de perdas.
- O porte do StockSharp opera em posições líquidas. As ordens de fechamento achatam toda a exposição na direção relevante, alinhando-se com o modelo de posição consolidada.
- As rotinas de registro do EA original foram omitidas em favor dos diagnósticos integrados do StockSharp.
