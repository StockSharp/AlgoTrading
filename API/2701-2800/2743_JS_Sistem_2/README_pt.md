# Estratégia JS Sistem 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
JS Sistem 2 é um sistema de seguimento de tendência originalmente escrito para MetaTrader 5. O port para StockSharp mantém o bloco de confirmação multi-indicador do expert advisor e opera em velas fechadas do período selecionado. As ordens têm volume fixo e podem ser opcionalmente bloqueadas se o saldo do portfólio conectado cair abaixo de um limite configurável. O risco é controlado por meio de distâncias rígidas de stop-loss e take-profit expressas em pips, juntamente com um trailing stop adaptativo que segue as sombras das velas.

## Indicadores e filtros
- **EMA(55), EMA(89), EMA(144)** – formam um filtro direcional. As configurações compradas exigem a EMA rápida acima da média e a média acima da linha lenta, enquanto a distância entre as curvas rápida e lenta deve permanecer abaixo de `MinDifferencePips`.
- **Histograma MACD (OsMA)** – usa comprimentos de EMA rápida, lenta e sinal idênticos à versão MQL. Uma operação comprada requer que o histograma seja positivo, uma operação vendida requer que seja negativo.
- **Índice de Vigor Relativo (RVI)** – calculado com período `RviPeriod` e suavizado por uma média móvel simples adicional com `RviSignalLength`. Operações compradas precisam que o RVI esteja acima de sua linha de sinal e acima do limiar `RviMax`; vendidas precisam do inverso.
- **Envelopes de swing mais alto/mais baixo** – rastreiam o máximo mais alto e o mínimo mais baixo durante `VolatilityPeriod` velas. Esses valores impulsionam a lógica do trailing stop e replicam o modo de trailing por sombras do expert advisor original.

## Lógica de trading
1. A estratégia processa apenas velas terminadas do `CandleType` configurado.
2. Antes de avaliar entradas, atualiza o trailing stop para posições existentes usando os últimos extremos de swing e, em seguida, verifica se os níveis de stop-loss ou take-profit foram atingidos durante a vela.
3. Condições de entrada comprada:
   - O saldo do portfólio está acima de `MinBalance`.
   - EMA55 > EMA89 > EMA144 e a diferença entre EMA55 e EMA144 está abaixo de `MinDifferencePips` (convertida para unidades de preço através do tamanho de pip do instrumento).
   - O histograma MACD (`macdLine`) é maior que zero.
   - O RVI está acima de sua linha de sinal e a linha de sinal está em ou acima de `RviMax`.
   - Não há posição comprada existente (`Position <= 0`). Quando existe uma posição vendida, ela é nivelada antes de abrir a comprada.
4. As condições de entrada vendida espelham as regras compradas com comparações invertidas e usam o limiar `RviMin`.
5. Ao entrar, a estratégia armazena o preço de fechamento da vela como referência, coloca níveis virtuais de stop-loss e take-profit deslocando esse preço por `StopLossPips` e `TakeProfitPips`, e redefine o estado de trailing.

## Gestão de saída e trailing
- **Stop-loss / take-profit rígido:** Sempre que o range da vela se sobreponha com o nível de stop ou alvo armazenado, a estratégia fecha toda a posição imediatamente.
- **Trailing stop:** Quando `TrailingEnabled` é verdadeiro, a estratégia tenta mover o stop na direção do lucro. Para comprados, o stop é elevado para o mínimo mais baixo das últimas `VolatilityPeriod` velas, uma vez que esse mínimo esteja acima tanto do preço de entrada quanto do stop anterior em pelo menos `TrailingIndentPips`. Vendidos seguem a regra simétrica usando o máximo mais alto. Isso reproduz o "trailing por sombras" do advisor MQL e impede que os stops se apertem prematuramente.
- **Proteção de saldo:** Se o valor atual do portfólio cair abaixo de `MinBalance`, a estratégia se abstém de enviar novas ordens, mas ainda gerencia operações abertas e trailing stops.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `MinBalance` | Saldo mínimo do portfólio necessário para novas entradas. | 100 |
| `Volume` | Volume da ordem enviado com cada operação. | 1 |
| `StopLossPips` | Distância do stop-loss medida em pips. Definir como 0 para desabilitar. | 35 |
| `TakeProfitPips` | Distância do take-profit medida em pips. Definir como 0 para desabilitar. | 40 |
| `MinDifferencePips` | Diferencial máximo permitido entre a EMA rápida e lenta em pips. | 28 |
| `VolatilityPeriod` | Número de velas usadas para calcular máximos e mínimos de swing para o trailing stop. | 15 |
| `TrailingEnabled` | Habilita ou desabilita a lógica do trailing stop. | true |
| `TrailingIndentPips` | Intervalo mínimo entre preço, entrada e stop ao atualizar o trailing stop. | 1 |
| `MaFastPeriod` | Período para a EMA rápida. | 55 |
| `MaMediumPeriod` | Período para a EMA média. | 89 |
| `MaSlowPeriod` | Período para a EMA lenta. | 144 |
| `OsmaFastPeriod` | Comprimento da EMA rápida para o histograma MACD. | 13 |
| `OsmaSlowPeriod` | Comprimento da EMA lenta para o histograma MACD. | 55 |
| `OsmaSignalPeriod` | Comprimento de suavização do sinal para o histograma MACD. | 21 |
| `RviPeriod` | Período do Índice de Vigor Relativo. | 44 |
| `RviSignalLength` | Comprimento da SMA aplicada ao RVI para obter sua linha de sinal. | 4 |
| `RviMax` | Limite superior que o sinal RVI deve atingir antes que as entradas compradas sejam permitidas. | 0.04 |
| `RviMin` | Limite inferior que o sinal RVI deve atingir antes que as entradas vendidas sejam permitidas. | -0.04 |
| `CandleType` | Período das velas usadas para todos os cálculos. | Velas de 5 minutos |

## Notas de implementação
- A distância de pip é derivada do passo de preço do instrumento. Instrumentos cotados com 3 ou 5 casas decimais usam um pip igual a dez passos de preço, correspondendo à lógica MQL original.
- O gerenciamento de stop e alvo ocorre dentro do loop da estratégia porque o StockSharp não envia automaticamente ordens do lado do servidor para eles neste template.
- A estratégia chama `StartProtection()` durante a inicialização para que a classe base possa monitorar desconexões inesperadas e posições pendentes.
