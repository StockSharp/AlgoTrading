# Estratégia JB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

A estratégia JB vem de um expert advisor do fxDreema que combina filtros de tendência de longo prazo, confirmação de momentum e rompimentos de volatilidade:

- **Filtro de tendência:** exige que o fechamento do candle anterior permaneça acima (compras) ou abaixo (vendas) de uma média móvel simples de 100 períodos.
- **Filtro de momentum:** confirma a direção com um Force Index de 100 períodos (positivo para compras, negativo para vendas).
- **Gatilho de volatilidade:** entra quando o fechamento anterior rompe a banda de Bollinger correspondente (20 períodos, desvio 2,0).
- **Gestão de posição:** aumenta o volume da ordem com um multiplicador em estilo martingale após um ciclo perdedor e volta ao tamanho base após ciclos lucrativos.
- **Regra de saída:** fecha todas as posições abertas quando o lucro médio não realizado por contrato alcança uma meta monetária configurável.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `SmaPeriod` | Comprimento do filtro de tendência SMA. Padrão: 100. |
| `ForcePeriod` | Comprimento do indicador Force Index. Padrão: 100. |
| `BollingerPeriod` | Comprimento das bandas de Bollinger. Padrão: 20. |
| `BollingerDeviation` | Multiplicador do desvio padrão para as bandas de Bollinger. Padrão: 2,0. |
| `BaseVolume` | Volume inicial da ordem antes dos ajustes de martingale. Padrão: 0,1. |
| `LossMultiplier` | Multiplicador aplicado ao próximo volume de ordem após um ciclo perdedor. Padrão: 1,55. |
| `AverageProfitTarget` | Lucro médio não realizado por contrato necessário para fechar todas as posições. Padrão: 2,8. |
| `CandleType` | Tipo de candle usado nos cálculos (por padrão, timeframe de 1 minuto). |

## Sinais

### Entrada comprada
1. O fechamento do candle anterior está abaixo ou igual à banda de Bollinger inferior.
2. O fechamento anterior é maior que a SMA de 100 períodos (tendência de alta).
3. O valor do Force Index é positivo.

### Entrada vendida
1. O fechamento do candle anterior está acima ou igual à banda de Bollinger superior.
2. O fechamento anterior é menor que a SMA de 100 períodos (tendência de baixa).
3. O valor do Force Index é negativo.

### Saídas
- Quando o lucro médio não realizado por contrato em todas as posições abertas atinge `AverageProfitTarget`, todas as posições são fechadas a mercado.
- Após cada posição zerada, a estratégia ajusta o próximo volume de ordem: multiplica por `LossMultiplier` após um ciclo perdedor e redefine para `BaseVolume` após um ciclo lucrativo.

## Notas

- A adaptação de martingale usa PnL realizado para decidir quando ocorreu uma sequência de perdas; garanta que a estratégia seja usada apenas em instrumentos onde aumentar volume seja aceitável.
- Como as estratégias StockSharp trabalham com posições líquidas, o hedge da versão MQL (cestas long e short simultâneas) é aproximado por posições agregadas.
