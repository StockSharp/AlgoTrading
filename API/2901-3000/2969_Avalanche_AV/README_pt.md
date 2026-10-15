# Estratégia Avalanche AV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Avalanche AV é uma estratégia de martingale aleatorizada que alterna entre entradas compradas e vendidas com igual probabilidade. As operações são abertas apenas após um número configurável de velas terminadas, e cada posição herda níveis fixos de stop-loss e take-profit definidos em pips. Quando uma operação fecha com prejuízo, o tamanho da posição é multiplicado pelo coeficiente de martingale para buscar a recuperação; operações lucrativas redefinem o tamanho de volta ao volume inicial assim que o saldo da conta registra um novo máximo de patrimônio. A estratégia também aplica um drawdown flutuante máximo como porcentagem do saldo da conta e fechará qualquer posição que ultrapasse esse limite.

A versão MQL original abria operações em ticks. O port do StockSharp mantém o mesmo comportamento probabilístico, mas trabalha em atualizações de velas, tornando-a adequada tanto para backtesting quanto para trading ao vivo com dados de barras.

## Regras de trading

- **Intervalo de decisão:** aguardar o número especificado de velas terminadas antes de avaliar um novo sinal. Se uma posição ainda estiver aberta, o intervalo continua contando, mas nenhuma nova operação é realizada.
- **Direção de entrada:** gerar um número aleatório; valores acima de 16384 acionam uma entrada comprada, caso contrário uma entrada vendida. As posições são abertas apenas quando não há operação ativa.
- **Tamanho da ordem:** iniciar com `InitialVolume`. Após cada operação perdedora, o próximo tamanho de ordem torna-se `PreviousVolume * MartingaleMultiplier` (normalizado para o passo de volume do instrumento). Operações vencedoras redefinem o tamanho para `InitialVolume` assim que o saldo realizado atinge um novo máximo; caso contrário, a expansão do martingale continua.
- **Stops e alvos:** stop-loss e take-profit são calculados em pips a partir do preço de entrada. Um pip é igual ao passo de preço do instrumento.
- **Drawdown flutuante:** enquanto uma posição está ativa, a estratégia monitora o PnL não realizado. Se a perda exceder `MaxDrawdownPercent` do saldo realizado da conta (`saldo inicial + PnL realizado`), a posição é fechada imediatamente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `InitialVolume` | 0.1 | Volume inicial de operação. |
| `StopLossPips` | 15 | Distância de stop em pips (0 desabilita o stop). |
| `TakeProfitPips` | 30 | Distância de take profit em pips (0 desabilita o alvo). |
| `MaxDrawdownPercent` | 75 | Perda flutuante máxima tolerada como porcentagem do saldo. |
| `MartingaleMultiplier` | 1.6 | Multiplicador de volume aplicado após uma perda. |
| `DecisionInterval` | 9 | Número de velas terminadas entre novas decisões de operação. |
| `CandleType` | Período de 1 minuto | Tipo de vela que impulsiona a estratégia. |

## Notas

- O volume é automaticamente normalizado para os limites `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento. Se a normalização falhar, o tamanho é redefinido para o volume inicial.
- Os níveis de stop-loss e take-profit dependem do `PriceStep` do instrumento como um pip; verifique o passo para símbolos exóticos.
- A proteção de drawdown requer que tanto `PriceStep` quanto `StepPrice` estejam definidos; caso contrário, a verificação de segurança é ignorada.
- Como a estratégia depende de aleatoriedade, os resultados variam entre execuções mesmo com dados de mercado idênticos, a menos que a semente aleatória seja controlada externamente.
