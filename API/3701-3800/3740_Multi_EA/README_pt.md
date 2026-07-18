# MultiStrategyEA v1.2 (StockSharp Porta)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp de alto nível do consultor especialista MetaTrader **MultiStrategyEA v1.2**. O EA original agrega sete osciladores e gerencia múltiplas grades de pedidos. A versão StockSharp concentra-se no aspecto de geração de sinal e negocia uma única posição líquida que é impulsionada por um consenso entre os módulos do indicador. Gerenciamento de pedidos, perfis de gerenciamento de dinheiro, grades e recursos de recuperação do código MT5 são intencionalmente omitidos para manter a implementação alinhada com o API de alto nível de StockSharp e para manter a clareza.

## Módulos
A estratégia avalia os seguintes módulos de indicadores no período selecionado:

1. **Oscilador de aceleração/desaceleração (AC)** – Usa a diferença entre o Oscilador Awesome e seu SMA de 5 períodos. Requer que o valor atual exceda o limite `AcLevel` e aumente (ou diminua) em relação à leitura anterior.
2. **Índice Direcional Médio (ADX)** – Confirma tendências quando a força de ADX está acima de `AdxTrendLevel` e o movimento direcional que domina também excede `AdxDirectionalLevel`.
3. **Awesome Oscillator (AO)** – Detecta explosões de impulso quando o oscilador ultrapassa `AoLevel` e continua na mesma direção.
4. **DeMarker** – Sinaliza possíveis reversões quando o oscilador sai dos territórios de sobrevenda (`100 - DeMarkerThreshold`) ou sobrecompra (`DeMarkerThreshold`).
5. **Índice de Força + Bandas Bollinger** – Requer que o preço toque uma banda Bollinger enquanto o Índice de Força (escalonado na porta exatamente como no script MT5) confirma o impulso além de `ForceConfirmationLevel`. Um `BandDistanceFilter` opcional rejeita sinais quando a largura da banda, medida em pips, é muito estreita ou muito larga.
6. **Índice de Fluxo de Dinheiro (MFI)** – Semelhante ao DeMarker; reage às zonas de sobrecompra e sobrevenda determinadas por `MfiThreshold`.
7. **MACD + Stochastic** – Exige que MACD (`MacdLevel`) e Stochastic (`StochasticLevel`) confirmem o mesmo viés direcional. MACD deve estar acima/abaixo do nível e acima/abaixo de sua linha de sinal. Stochastic deve estar acima/abaixo do limite e acima/abaixo da linha de sinal.

Cada módulo contribui com um voto de **compra**, **venda** ou **neutro** com base na última vela finalizada.

## Lógica de Consenso
- Quando `TradeAllStrategies` é **true** (padrão), a estratégia espera até que pelo menos `RequiredConfirmations` votos de alta com **zero** votos de baixa apareçam antes de entrar comprado. A mesma lógica se aplica aos shorts.
- Quando `TradeAllStrategies` é **falso**, um único voto de alta ou baixa é suficiente para negociar.
- Se `CloseInReverse` estiver ativado, a estratégia fecha imediatamente uma posição oposta antes de abrir uma nova.

A implementação opera apenas uma posição agregada e não tenta recriar a escrituração de pedidos por módulo do EA original.

## Gestão de risco
- `StopLossPips` e `TakeProfitPips` são traduzidos em compensações de preço usando o `PriceStep` do instrumento. Para símbolos com 3 ou 5 dígitos decimais, o tamanho do pip é automaticamente multiplicado por 10, imitando o comportamento do pip FX.
- Stops e alvos são verificados em cada vela finalizada usando os máximos/mínimos da vela. Quando qualquer um dos limites é atingido, toda a posição é fechada.

## Diferenças do Expert Advisor MT5
- Sem recursos de grade, martingale ou recuperação. O dimensionamento da posição é fixado por meio do parâmetro `Volume`.
- Variantes de sinal próximo (opções `CloseOrdersType` no MT5) não são implementadas; as saídas dependem do stop-loss/take-profit global ou do comportamento opcional de reversão no sinal oposto.
- A configuração do indicador em StockSharp reflete a ideia principal de cada módulo, mas suporta apenas a interpretação mais comum em vez das muitas enumerações de modo encontradas no script original.
- Blocos de gerenciamento de dinheiro (lote automático, proteção de conta, avaliação de pip específica de símbolo) estão fora do escopo desta porta de alto nível.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de dados usada por cada módulo de indicador. |
| `Volume` | Volume líquido negociado quando aparece um sinal de consenso. |
| `TradeAllStrategies` | Permite votação por consenso; caso contrário, qualquer voto desencadeia uma negociação. |
| `RequiredConfirmations` | Número de votos correspondentes de alta ou baixa necessários quando o consenso está habilitado. |
| `CloseInReverse` | Feche uma posição existente antes de abrir o lado oposto. |
| `StopLossPips` / `TakeProfitPips` | Stop protetor e meta de lucro medidos em pips. |
| `UseAcModule`, `AcLevel` | Alternar e limite para o módulo Accelerator Oscillator. |
| `UseAdxModule`, `AdxPeriod`, `AdxTrendLevel`, `AdxDirectionalLevel` | Configuração ADX. |
| `UseAoModule`, `AoLevel` | Configuração impressionante do oscilador. |
| `UseDeMarkerModule`, `DeMarkerPeriod`, `DeMarkerThreshold` | Configurações do oscilador DeMarker. |
| `UseForceBollingerModule`, `BollingerPeriod`, `BollingerDeviation`, `ForceConfirmationLevel`, `BandDistanceFilter` | Índice de força + Bollinger configurações de filtro de banda. |
| `UseMfiModule`, `MfiPeriod`, `MfiThreshold` | Configurações do índice de fluxo de dinheiro. |
| `UseMacdStochasticModule`, `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdLevel`, `StochasticPeriod`, `StochasticSignalPeriod`, `StochasticSlowing`, `StochasticLevel` | Configuração combinada MACD e Stochastic. |

## Notas de uso
1. Anexe a estratégia a um instrumento com dados históricos suficientes para a formação de todos os indicadores.
2. Configure o prazo e os limites do módulo para corresponder às condições de mercado desejadas. Os padrões replicam os valores usados ​​nas entradas MT5 EA.
3. A lógica de consenso é sensível a quantos módulos estão ativos. Se você desabilitar módulos, considere diminuir `RequiredConfirmations` adequadamente.
4. Como a estratégia negocia uma única posição líquida, ela é adequada para uso dentro de Designer, Runner ou outros ambientes StockSharp de alto nível sem roteamento adicional de portfólio.

## Isenção de responsabilidade
Esta porta se concentra na paridade do sinal em vez de reproduzir toda a pilha de gerenciamento de risco e dinheiro do especialista MetaTrader original. A arquitetura simplificada facilita o teste, a extensão ou a integração em soluções baseadas em StockSharp, mas os resultados serão diferentes da versão MT5 quando recursos complexos (grades, lotes de recuperação, fechamentos parciais) forem o principal impulsionador de desempenho.
