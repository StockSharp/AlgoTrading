# Estratégia de Redução de Riscos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Redução de Riscos é um sistema de seguimento de tendência multi-temporal convertido do assessor especialista do MetaTrader "Reduce_risks.mq5". Ela analisa velas de um minuto para acionar entradas enquanto filtra o regime de mercado com médias de 15 minutos e 1 hora. O algoritmo original foi projetado para pares de divisas principais altamente líquidos (EURUSD, USDCHF, USDJPY) e se concentra em entrar em tendências apenas quando a volatilidade é reduzida e a estrutura confirma a continuação.

## Mercado e períodos
- **Período primário:** velas de 1 minuto para geração de sinais.
- **Período de confirmação:** velas de 15 minutos para validação de momentum e posicionamento de onda.
- **Filtro de tendência:** velas de 1 hora para garantir trading na direção da tendência mais ampla.
- **Instrumentos recomendados:** EURUSD, USDCHF, USDJPY ou instrumentos com estrutura de pip similar (cotação de 4 ou 5 casas decimais).

## Indicadores e dados
- Quatro médias móveis simples (SMA) no M1: períodos 5, 8, 13 e 60 calculados sobre o preço típico.
- Três SMAs no M15: períodos 4, 5 e 8 calculados sobre o preço típico.
- Uma SMA no H1: período 24 calculado sobre o preço típico.
- Estatísticas de velas (tamanho do corpo, amplitude, sombras) tanto para M1 quanto M15.
- Contadores internos rastreiam o preço mais alto ou mais baixo desde a entrada para emular a lógica de trailing do MQL.

## Regras de entrada
### Configuração comprada
1. Velas recentes de M1 e M15 devem exibir baixa volatilidade: três barras anteriores em cada período têm amplitudes abaixo de 20 e 30 pips respectivamente, e a largura do canal de 15 minutos é limitada a 30 pips.
2. A última vela M1 concluída é mais ativa que sua predecessora, mas não três vezes maior, e o preço atual rompe tanto as máximas recentes de M1 quanto de M15 (resistência local removida).
3. A hierarquia de SMA aponta para cima: SMA5 > SMA8 > SMA13 e SMA60 em alta; o preço de fechamento fica acima de todas as quatro médias.
4. SMA4 no M15 está em alta e posicionada acima da SMA8, enquanto o preço de fechamento está acima das médias de M15 e H1.
5. Confirmação de onda: SMA8 no M1 cruzou dentro de qualquer uma das três velas anteriores, e SMA5 no M15 fica dentro da amplitude da vela M15 anterior.
6. Filtros de estrutura de velas: as velas anteriores de M1 e M15 têm corpos de alta excedendo metade de suas amplitudes, mantêm máximas mais altas, mostram retrações aceitáveis (<25% da amplitude da vela anterior) e contêm sombras intrábarra (sem marubozu).
7. Todas as condições acima devem ser satisfeitas simultaneamente sem posição aberta antes de emitir uma ordem de compra a mercado.

### Configuração vendida
1. Os mesmos filtros de volatilidade se aplicam, mas o rompimento deve ocorrer abaixo das mínimas recentes (violação de suporte).
2. A hierarquia de SMA se inverte: SMA5 < SMA8 < SMA13 com SMA60 caindo; o preço de fechamento fica abaixo de todas as quatro médias.
3. SMA4 no M15 declina e fica abaixo da SMA8; o preço de fechamento está abaixo das médias de M15 e H1.
4. Validação de onda: SMA8 no M1 fica dentro de qualquer uma das três amplitudes de velas M1 anteriores, SMA5 no M15 fica dentro da última vela M15, e velas recentes mostram estrutura de baixa persistente (mínimas mais baixas, corpos de baixa, retrações limitadas, sombras presentes).
5. Sem posição ativa, uma ordem de venda a mercado é enviada quando todas as condições se alinham.

## Regras de saída
- As ordens de stop-loss e take-profit de proteção são anexadas automaticamente usando as distâncias de pip configuradas (espelha o comportamento original do EA).
- Saídas discricionárias adicionais replicam a lógica MQL:
  - Fechar comprados se a vela M1 atual colapsa pelo menos 10 pips desde sua abertura ou se uma vela M1 fortemente de baixa aparece após a operação ter estado aberta por mais de um minuto.
  - Tomar lucro antecipadamente quando o preço avança pelo menos 10 pips, ou quando ocorre uma reversão de trailing: após a primeira barra após a entrada, se o preço retrai 20 pips do nível mais alto atingido desde a entrada enquanto essa máxima está acima do preço de entrada.
  - Fechar comprados em uma excursão adversa de 20 pips ou sempre que o capital do portfólio cair abaixo do limite de drawdown configurado. As posições vendidas usam lógica simétrica com comparações invertidas.

## Gestão de risco
- O trading para automaticamente quando o capital do portfólio cai abaixo de `(InitialDeposit * (100% - RiskPercent))`. O limite é verificado em cada tentativa de sinal e redefinido quando o capital se recupera acima do limiar.
- O script MQL original incluía extensas verificações de terminal; essas são omitidas porque o StockSharp lida com conectividade e permissões nativamente.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `StopLossPips` | Distância do stop de proteção em pips (espelhada pela lógica de trailing). | `30` |
| `TakeProfitPips` | Distância do take-profit em pips. | `60` |
| `InitialDeposit` | Capital de referência usado para calcular o stop de drawdown. | `10000` |
| `RiskPercent` | Percentual máximo do depósito inicial que pode ser perdido antes de bloquear novas operações e forçar o fechamento de posições ativas. | `5` |
| `M1CandleType` | Tipo de dados para a assinatura de velas de 1 minuto. | Período de `1 minuto` |
| `M15CandleType` | Tipo de dados para a assinatura de confirmação de 15 minutos. | Período de `15 minutos` |
| `H1CandleType` | Tipo de dados para a assinatura de filtro de tendência de 1 hora. | Período de `1 hora` |

## Notas
- A estratégia espera instrumentos cotados com tamanhos de pip similares aos principais pares de divisas. Ajuste os parâmetros baseados em pips quando usar outros mercados.
- Apenas a implementação em C# é fornecida; a versão em Python é intencionalmente omitida conforme os requisitos.
