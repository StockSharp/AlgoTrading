# Estratégia KWAN CCC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia KWAN CCC reproduz o especialista MetaTrader `Exp_KWAN_CCC.mq5` usando a API de alto nível do StockSharp. O sistema deriva sinais de trading de um oscilador personalizado construído da seguinte forma:

1. Calcular o oscilador Chaikin (diferença entre as médias móveis rápida e lenta da linha de acumulação/distribuição).
2. Multiplicar o valor Chaikin pelo Commodity Channel Index (CCI).
3. Dividir o resultado pelo valor do indicador Momentum. Quando o Momentum é zero, o script substitui um valor constante de 100 para evitar divisão por zero, exatamente como o código original.
4. Suavizar a série resultante com o método XMA selecionado pelo usuário.
5. Detectar a inclinação da série suavizada. Barras em alta são coloridas com `0`, barras em queda com `2`, caso contrário `1`.

Quando a cor muda de `0` para qualquer outra coisa, a estratégia fecha posições vendidas e abre uma posição comprada. Quando a cor muda de `2` para qualquer outra coisa, fecha posições compradas e abre uma vendida. Isso reflete a lógica implementada no especialista MQL, incluindo o deslocamento de sinal opcional (`SignalBar`).

## Regras de trading
- **Entrada comprada**: a cor na barra em `SignalBar + 1` é igual a `0` e a barra em `SignalBar` é diferente de `0`.
- **Entrada vendida**: a cor na barra em `SignalBar + 1` é igual a `2` e a barra em `SignalBar` é diferente de `2`.
- **Saída comprada**: habilitada quando `EnableLongExits = true` e a condição de entrada vendida é acionada.
- **Saída vendida**: habilitada quando `EnableShortExits = true` e a condição de entrada comprada é acionada.
- As ordens de stop protetor e alvo são criadas através de `StartProtection` usando deslocamentos de preço absolutos derivados de `StopLossPoints` e `TakeProfitPoints` multiplicados pelo `PriceStep` do instrumento.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `OrderVolume` | Tamanho de ordem base usado ao abrir uma nova posição. |
| `CandleType` | Período para todos os cálculos de indicadores. O padrão é 1 hora. |
| `FastPeriod` / `SlowPeriod` | Comprimentos das médias móveis dentro do oscilador Chaikin. |
| `ChaikinMethod` | Tipo de média móvel (simples, exponencial, suavizada, ponderada) aplicada à linha de acumulação/distribuição. |
| `CciPeriod` | Período do Commodity Channel Index. |
| `MomentumPeriod` | Período do indicador Momentum. |
| `SmoothingMethod` | Método de suavização XMA mapeado das opções originais. `JurX`, `Parabolic` e `T3` recorrem ao Jurik MA; `Vidya` usa suavização adaptativa baseada no Oscilador de Momentum de Chande; `Adaptive` usa Kaufman AMA. |
| `SmoothingLength` | Número de barras usadas pelo filtro de suavização selecionado. |
| `SmoothingPhase` | Parâmetro adicional usado por métodos específicos (p. ex., comprimento CMO de VIDYA, período lento de AMA). |
| `SignalBar` | Deslocamento (em barras completadas) usado para avaliar as transições de cor. `1` reproduz o padrão do MetaTrader. |
| `EnableLongEntries` / `EnableShortEntries` | Permitir ou bloquear a abertura de novas posições na direção correspondente. |
| `EnableLongExits` / `EnableShortExits` | Permitir ou bloquear o fechamento de posições impulsionado pelo indicador. |
| `StopLossPoints` / `TakeProfitPoints` | Stop protetor/alvo medido em passos de preço (definir como zero para desabilitar). |

## Notas de implementação
- A estratégia age apenas em velas terminadas e usa o helper `Bind` do StockSharp para transmitir dados de velas para os indicadores.
- A lista de métodos de suavização reflete a implementação XMA da biblioteca original. Métodos não disponíveis no StockSharp são mapeados para a alternativa mais próxima, conforme indicado na tabela de parâmetros.
- A entrada `VolumeType` do MetaTrader é omitida porque as velas do StockSharp já encapsulam as informações de volume total usadas pela linha de acumulação/distribuição.
- O gerenciamento de dinheiro no especialista original dependia de helpers personalizados de dimensionamento de lotes. A conversão assume um volume fixo especificado por `OrderVolume`.

## Dicas de uso
- Certifique-se de que o instrumento forneça dados de volume significativos se o comportamento do oscilador Chaikin for importante. Para instrumentos ilíquidos, considere aumentar `MomentumPeriod` para reduzir o ruído.
- Ao otimizar os parâmetros de suavização, combine `SmoothingLength` e `SmoothingPhase` com cuidado: combinações extremas podem atrasar os sinais consideravelmente.
- Os valores protetores padrão (`StopLossPoints = 1000`, `TakeProfitPoints = 2000`) correspondem a grandes deslocamentos. Ajuste-os para corresponder ao tamanho do tick do instrumento.
