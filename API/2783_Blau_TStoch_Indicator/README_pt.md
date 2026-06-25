# Estratégia com Indicador Blau TStoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Port do expert advisor do MetaTrader 5 `Exp_BlauTStochI` para a API de alto nível do StockSharp.
- Negocia o Índice Estocástico Triplo de Blau (William Blau) em períodos configuráveis.
- Suporta dois modos de execução: **Breakdown** (rompimentos da linha zero) e **Twist** (reversões de inclinação).
- As permissões de posição reproduzem os sinalizadores originais do expert advisor (interruptores independentes para abrir/fechar operações compradas e vendidas).

## Construção do indicador
- Calcula uma série de momentum como `preço aplicado - mínimo` ao longo de `MomentumLength` barras e seu intervalo `máximo - mínimo`.
- Aplica três estágios de suavização consecutivos tanto ao numerador quanto ao denominador.
- Métodos de suavização suportados: Exponencial (EMA), Simples (SMA), Suavizado/Contínuo (SMMA) e Linear Ponderado (LWMA).
- As opções MQL originais (JJMA, JurX, ParMA, T3, VIDYA, AMA) **não** são reproduzidas; o parâmetro `Phase` é retido por compatibilidade, mas ignorado.
- As opções de preço aplicado correspondem às enumerações MQL (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado, simples, quartil, variantes de seguimento de tendência, DeMark).
- Valor final do indicador: `100 * stochSuavizado / rangoSuavizado - 50`.

## Regras de trading
### Modo Breakdown
- Inspeciona o indicador na barra definida por `SignalBar` (padrão 1, ou seja, a última vela fechada).
- **Entrada comprada:** valor anterior (`SignalBar+1`) acima de zero **e** valor atual (`SignalBar`) cruza abaixo ou igual a zero.
- **Entrada vendida:** valor anterior abaixo de zero **e** valor atual cruza acima ou igual a zero.
- **Saída comprada:** valor anterior abaixo de zero e saídas compradas permitidas.
- **Saída vendida:** valor anterior acima de zero e saídas vendidas permitidas.

### Modo Twist
- **Entrada comprada:** indicador subindo (`value[SignalBar+1] < value[SignalBar+2]`) e o último valor não menor que o anterior.
- **Entrada vendida:** indicador caindo (`value[SignalBar+1] > value[SignalBar+2]`) e o último valor não maior que o anterior.
- **Saída comprada:** a inclinação do indicador vira para baixo (`value[SignalBar+1] > value[SignalBar+2]`).
- **Saída vendida:** a inclinação do indicador vira para cima (`value[SignalBar+1] < value[SignalBar+2]`).

### Gestão de posição
- As entradas revertem posições opostas existentes adicionando o tamanho absoluto da posição ao `Volume` configurado.
- As saídas fecham a posição existente completa com ordens de mercado.
- O processamento de operações é realizado apenas em velas concluídas e depois que o indicador estiver totalmente formado.

## Gestão de risco
- Stop-loss e take-profit opcionais medidos em passos de preço (`StopLossPoints`, `TakeProfitPoints`).
- Ambos são implementados via `StartProtection` e podem ser desabilitados definindo a distância como zero.

## Parâmetros
| Parâmetro | Descrição | Valor padrão |
|-----------|-----------|-------------|
| `CandleType` | Tipo de dado/período para cálculos. | Velas de 4 horas |
| `Smoothing` | Método de suavização (EMA/SMA/SMMA/LWMA). | EMA |
| `MomentumLength` | Retrospectiva para detecção de máximos/mínimos. | 20 |
| `FirstSmoothing` | Comprimento do estágio de suavização 1. | 5 |
| `SecondSmoothing` | Comprimento do estágio de suavização 2. | 8 |
| `ThirdSmoothing` | Comprimento do estágio de suavização 3. | 3 |
| `Phase` | Mantido por compatibilidade (ignorado). | 15 |
| `PriceType` | Constante de preço aplicado. | Close |
| `SignalBar` | Deslocamento de barra usado para avaliação de sinal (>= 1). | 1 |
| `Mode` | Modo de trading (Breakdown/Twist). | Twist |
| `AllowLongEntries` | Habilitar entradas compradas. | true |
| `AllowShortEntries` | Habilitar entradas vendidas. | true |
| `AllowLongExits` | Habilitar fechamento de operações compradas. | true |
| `AllowShortExits` | Habilitar fechamento de operações vendidas. | true |
| `TakeProfitPoints` | Distância de take-profit em passos (0 desabilita). | 2000 |
| `StopLossPoints` | Distância de stop-loss em passos (0 desabilita). | 1000 |

## Diferenças do expert MT5
- Algoritmos de suavização avançados do SmoothAlgorithms.mqh não estão implementados; escolha entre EMA/SMA/SMMA/LWMA.
- O gerenciamento de capital (dimensionamento de lotes) é simplificado: a estratégia depende da propriedade `Volume` do StockSharp.
- A avaliação de sinais ocorre apenas em velas concluídas; não há execução intra-barra.

## Notas de uso
- Certifique-se de que `SignalBar` permaneça em pelo menos 1; a implementação mantém histórico suficiente do indicador automaticamente.
- Aumentar os comprimentos de suavização aumenta o tempo de formação porque cada estágio requer a janela completa para ser concluído.
- Para trading de reversão em períodos mais altos, considere ampliar as distâncias de stop/take ou desabilitar um lado através das permissões.
