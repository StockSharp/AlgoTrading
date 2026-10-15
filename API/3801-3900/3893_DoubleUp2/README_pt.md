# Estratégia DoubleUp2 Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia DoubleUp2 Martingale reproduz o especialista MetaTrader original combinando o Commodity Channel Index (CCI) e o oscilador MACD. As negociações são abertas somente quando ambos os indicadores atingem níveis extremos na mesma direção. O dimensionamento da posição segue um esquema de martingale onde o volume dobra após uma negociação perdedora. As negociações lucrativas são parcialmente bloqueadas ao fechar a posição quando o preço percorre uma distância configurável em favor da posição.

## Como funciona
1. Assine uma única série de velas (padrão 1 minuto) e calcule CCI e MACD em cada barra concluída.
2. Detectar impulso extremo:
   * Insira curto quando CCI e MACD excederem o limite positivo.
   * Insira longo quando ambos caírem abaixo do limite negativo.
3. Antes da reversão, a posição atual é fechada e o passo martingale é atualizado com base no lucro simulado da última negociação.
4. O volume de negociação é igual ao volume base derivado do patrimônio da conta dividido por um divisor de saldo, multiplicado pelo fator martingale elevado à etapa atual.
5. Garanta lucros fechando qualquer posição aberta assim que o preço avançar um número predefinido de pontos desde a última entrada. As saídas vencedoras aumentam o passo martingale em dois para corresponder ao comportamento original EA.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `CciPeriod` | Período de lookback para o indicador CCI. | 8 |
| `MacdFastPeriod` | Comprimento EMA rápido para MACD. | 13 |
| `MacdSlowPeriod` | Comprimento EMA lento para MACD. | 33 |
| `MacdSignalPeriod` | Comprimento EMA do sinal para suavização MACD. | 2 |
| `Threshold` | Limite absoluto do indicador que deve ser excedido para acionar entradas. | 230 |
| `ExitDistancePoints` | Distância de lucro em pontos que aciona o fechamento da posição. | 120 |
| `BalanceDivisor` | Divisor aplicado ao patrimônio do portfólio para obter o volume base. | 50001 |
| `MinimumVolume` | Limite inferior para o volume de negociação calculado. | 0,1 |
| `MartingaleMultiplier` | Multiplicador aplicado ao tamanho da posição após cada fechamento perdedor. | 2 |
| `CandleType` | Período de vela usado para todos os cálculos. | 1 minuto |

## Notas
* A lógica martingale aumenta o tamanho da posição após perdas e reinicia após reversões lucrativas, espelhando a lógica de origem MQL.
* As informações da etapa de preço são usadas para converter a distância de saída (pontos) em unidades de preço absoluto. Se o instrumento não fornecer uma etapa de preço, será utilizado o valor 1.
* A estratégia espera um único instrumento e não coloca posições longas e curtas simultaneamente.
