# Estratégia de Impulso de Seis Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o MetaTrader 4 consultor especialista **6xIndics_M** usando o StockSharp API de alto nível. Ele mistura seis entradas de impulso derivadas do Accelerator Oscillator (AC) e do Awesome Oscillator (AO) de Bill Williams e as alimenta por meio de uma matriz de decisão selecionável. Um oscilador estocástico lento atua como filtro final. Apenas uma posição está aberta por vez; gestão de dinheiro martingale, stop-loss/take-profit e trailing stops opcionais emulam o comportamento original.

## Como funciona a estratégia

1. **Assinatura de dados** – a estratégia assina a série de velas configurada (`CandleType`, barras padrão de 1 hora).
2. **Indicadores**
   - Awesome Oscillator calcula a diferença entre as médias móveis simples de 5 e 34 períodos do preço médio.
   - Uma média móvel simples de 5 períodos do AO produz os valores do oscilador do acelerador (AC).
   - Um oscilador Stochastic com parâmetros 5/5/5 fornece a linha %K que está atrasada por uma vela fechada (deslocamento MT4 = 1).
3. **Seis slots de indicadores** – cada vela finalizada preenche os seguintes buffers:
   - Slot 0: valor AC alterado em 1 vela (`AC[1]`).
   - Slot 1: valor AC alterado em 10 velas (`AC[10]`).
   - Slot 2: valor AC alterado em 20 velas (`AC[20]`).
   - Slot 3: Momentum AO, ou seja, `AO[0] - AO[shift]`, onde a mudança é configurável (`AoMomentumShift`).
   - Slot 4: Momento AC `AC[0] - AC[shift #1]` (`AcPrimaryShift`).
   - Slot 5: Momento AC `AC[0] - AC[shift #2]` (`AcSecondaryShift`).
4. **Matriz de sinal selecionável** – parâmetros `FirstSourceIndex`… `SixthSourceIndex` escolhem qual slot alimenta as seis verificações booleanas originalmente chamadas `k`, `u`, `t`, `e`, `r`, `o`. Os mesmos índices são reutilizados tanto para gerar entradas quanto para fechar negociações quando `CloseOnReverseSignal` está habilitado.
5. **Lógica de entrada**
   - **Compre** quando os slots escolhidos satisfizerem: `A > 0`, `B > 0.0001 × Sensitivity`, `C > 0.0002 × Sensitivity`, `D < 0`, `E < 0.0001 × Sensitivity`, `F < 0.0002 × Sensitivity`, e o %K estocástico anterior estiver abaixo de 15.
   - **Venda** quando `A < 0`, `B < 0.0001 × Sensitivity`, `C < 0.0002 × Sensitivity`, `D > 0`, `E > 0.0001 × Sensitivity`, `F > 0.0002 × Sensitivity` e o %K estocástico anterior estiver acima de 85.
6. **Gerenciamento de posição**
   - Apenas uma posição é permitida. Quando uma negociação é aberta, a estratégia ignora novas entradas, espelhando o especialista MT4.
   - Os níveis de stop-loss e take-profit são convertidos de pips em preços absolutos usando o tamanho do tick do instrumento (exatamente como `Point` funciona no MT4).
   - O trailing stop opcional replica o comportamento original: ele é ativado quando o preço se move `TrailingStopPips` além da entrada (e, quando `RequireProfitForTrailing` for verdadeiro, por um `LockProfitPips` extra). A parada segue o preço apenas na direção favorável.
   - `CloseOnReverseSignal` fecha uma negociação lucrativa se o sinal oposto aparecer (ofereça acima da entrada para posições compradas, pergunte abaixo para posições vendidas).
7. **Martingale dimensionamento** – quando ativado, o próximo volume do pedido é igual ao volume de negociação anterior multiplicado por `(TakeProfitPips + StopLossPips) / TakeProfitPips` sempre que uma negociação fecha com perda ou ponto de equilíbrio. As negociações vencedoras redefinem o tamanho para a base `Volume`.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `AllowBuy`, `AllowSell` | Ative ou desative entradas longas/curtas. | `true` |
| `CloseOnReverseSignal` | Feche a posição atual quando um sinal oposto aparecer enquanto a negociação estiver com lucro. | `false` |
| `FirstSourceIndex` … `SixthSourceIndex` | Escolha qual dos seis slots de indicadores alimenta cada verificação lógica. Valores fora de 0–5 são fixados. | `1,2,3,4,3,4` |
| `AoMomentumShift` | Número de barras entre o valor AO atual e a comparação usada no slot 3. | `10` |
| `AcPrimaryShift`, `AcSecondaryShift` | Número de barras entre o valor AC atual e as comparações dos slots 4 e 5. | `10` / `10` |
| `SensitivityMultiplier` | Multiplicador aplicado aos limites de 0,0001 e 0,0002 usados nas verificações de slot. | `1.0` |
| `TakeProfitPips`, `StopLossPips` | Distâncias de saída expressas em pips no estilo MetaTrader (eles são redimensionados pelo tamanho do tick). | `300` / `300` |
| `UseTrailingStop` | Habilite a lógica de trailing stop. | `false` |
| `TrailingStopPips` | Distância entre o preço e o trailing stop, em pips. | `300` |
| `RequireProfitForTrailing` | Quando ativado, o trailing stop é ativado somente depois que a negociação ganha um `LockProfitPips` extra. | `false` |
| `LockProfitPips` | Lucro adicional (em pips) que deve ser bloqueado antes que o trailing stop comece a se mover. | `300` |
| `Volume` | Tamanho base do pedido. | `0.1` |
| `UseMartingale` | Ative o dimensionamento da posição martingale. | `false` |
| `CandleType` | Série de velas usada para todos os cálculos. | `TimeSpan.FromHours(1)` |

## Notas e práticas recomendadas

- Cada vela é processada somente após seu término, portanto, os sinais imitam o especialista MT4 que foi executado uma vez por barra (`prevtime` guarda no código original).
- A estratégia armazena apenas o histórico necessário (até 256 barras) para reproduzir os cálculos de mudança MT4 sem chamar `GetValue()` nos indicadores, satisfazendo as diretrizes do projeto.
- As saídas de trailing e stop/limit são simuladas nas máximas/mínimas das velas. Em um ambiente ao vivo, você deve usar ordens de parada reais para execução garantida.
- O dimensionamento de Martingale usa os limites `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento para manter os volumes dentro das regras do corretor.
- Quando `AllowBuy` ou `AllowSell` está desativado, os sinais correspondentes são ignorados, mas o sinal oposto ainda pode ser usado para `CloseOnReverseSignal`.

## Diferenças versus o especialista MT4

- Os cálculos do indicador usam o Awesome Oscillator integrado do StockSharp e as classes SMA; nenhum gerenciamento manual de buffer é necessário.
- Todas as negociações são executadas por meio de ordens de mercado (`BuyMarket` / `SellMarket`) e saídas por meio de `ClosePosition()`, enquanto a versão MT4 envia solicitações explícitas de `OrderSend`/`OrderClose`.
- O dimensionamento do lote respeita a granularidade do volume de troca arredondando para `VolumeStep` e fixando para `[MinVolume, MaxVolume]`.
- Auxiliares de gráfico (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) são adicionados para inspeção visual quando um gráfico está disponível.
