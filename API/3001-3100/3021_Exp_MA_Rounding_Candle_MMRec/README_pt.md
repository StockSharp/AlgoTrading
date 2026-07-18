# Estratégia Exp MA Rounding Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Exp MA Rounding Candle MMRec** é o port do StockSharp do consultor especialista MQL5 `Exp_MA_Rounding_Candle_MMRec`. O sistema original depende de um indicador personalizado "MA Rounding Candle" que converte cada vela de mercado em uma vela sintética suavizada e rastreia suas mudanças de cor. A versão em C# reproduz o mesmo comportamento reconstruindo a lógica do indicador on-the-fly e reagindo ao fluxo de cores resultante.

## Construção do MA Rounding Candle
1. Cada vela recebida é processada por quatro médias móveis idênticas (abertura, máximo, mínimo, fechamento). Os tipos de suavização suportados são **Simple**, **Exponential**, **Smoothed (RMA/SMMA)** e **Weighted**.
2. A saída bruta da média móvel é passada pelo filtro de "arredondamento" original. O filtro só aceita um novo valor se ele diferir da saída anterior em mais de `RoundingFactor * PriceStep`. Caso contrário, o valor arredondado anterior é mantido. Isso reproduz o comportamento do MQL5 onde o sinal permanece plano durante pequenas oscilações.
3. Um filtro de gap ancora o open arredondado ao close arredondado anterior sempre que a diferença absoluta entre o open e o close reais é menor que `GapSize * PriceStep`. Isso evita que pequenas velas doji alterem a cor da vela sintética.
4. Após o arredondamento, a cor do indicador é definida como:
   * `2` – vela sintética altista (`open < close`)
   * `0` – vela sintética baixista (`open > close`)
   * `1` – vela neutra (`open == close`)

A estratégia armazena apenas os últimos valores de cor (suficientes para o look-back configurado) e não mantém histórico longo, alinhada com o especialista original.

## Lógica de sinais
Os sinais são avaliados em velas finalizadas usando um deslocamento `SignalBar` configurável:

* `SignalBar` denota quantas velas fechadas atrás devem ser tratadas como a barra de gatilho (`0` = barra fechada atual, `1` = a barra completamente fechada mais recente, etc.).
* A estratégia também inspeciona a cor da barra que a precede imediatamente (`SignalBar + 1`).
* Uma transição **altista para não altista** (`color[SignalBar + 1] = 2` e `color[SignalBar] != 2`) gera:
  * fechamento opcional de posições vendidas existentes (`EnableShortExits`), e
  * abertura opcional de uma nova posição comprada (`EnableLongEntries`).
* Uma transição **baixista para não baixista** (`color[SignalBar + 1] = 0` e `color[SignalBar] != 0`) gera:
  * fechamento opcional de posições compradas existentes (`EnableLongExits`), e
  * abertura opcional de uma nova posição vendida (`EnableShortEntries`).

O gerenciamento de posições segue o EA original: as saídas são executadas antes das novas entradas, e ao mudar de direção a estratégia adiciona o valor absoluto da posição existente ao volume de negociação base para que o tamanho líquido corresponda à direção desejada.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 1 hora | Série de velas usada para conduzir a estratégia. |
| `SmoothingMethod` | `Simple` | Tipo de média móvel para todas as séries de preços arredondadas. |
| `MaLength` | `12` | Número de períodos usados pela média móvel escolhida. |
| `RoundingFactor` | `50` | Multiplicador aplicado ao `PriceStep` do instrumento para construir o limiar de arredondamento. Valores maiores fazem a série arredondada mudar com menos frequência. |
| `GapSize` | `10` | Multiplicador aplicado ao `PriceStep` para o filtro de gap que bloqueia o open arredondado no close arredondado anterior em velas pequenas. |
| `SignalBar` | `1` | Quantas velas fechadas atrás são analisadas para o sinal. |
| `TradeVolume` | `1` | Volume de posição base usado para novas entradas. O parâmetro é sincronizado com a propriedade integrada `Strategy.Volume`. |
| `EnableLongEntries` / `EnableShortEntries` | `true` | Alternadores para entradas compradas/vendidas. |
| `EnableLongExits` / `EnableShortExits` | `true` | Alternadores para fechamento de posições existentes. |

## Notas de implementação
* Apenas os modos de suavização disponíveis no StockSharp são expostos. Suavizadores exóticos específicos do MQL5 (JJMA, JurX, VIDYA, AMA, etc.) não estão presentes neste port.
* O complexo recontador de gestão de dinheiro do EA original é substituído por um único parâmetro `TradeVolume`. Isso mantém a estratégia determinista e mais fácil de otimizar dentro do StockSharp.
* Todos os limiares baseados em preço (`RoundingFactor`, `GapSize`) são interpretados em passos de preço multiplicando o valor por `Security.PriceStep` cada vez que uma vela é processada.
* A estratégia usa a API de assinatura de velas de alto nível (`SubscribeCandles`) e opera estritamente em velas completadas, assim como o especialista MQL5 que aguarda `IsNewBar` antes de emitir ordens.
* Proteção comprada/vendida, trailing stops e outras saídas são omitidos intencionalmente porque não faziam parte da implementação original.

## Uso
1. Vincule a estratégia ao instrumento desejado e atribua uma série de velas adequada através de `CandleType` (ex.: `TimeSpan.FromHours(1).TimeFrame()`).
2. Configure o método de suavização, o comprimento da média móvel, o fator de arredondamento e o filtro de gap para corresponder às configurações do EA original ou aos seus próprios resultados de otimização.
3. Defina `TradeVolume` para o tamanho de lote que planeja negociar. A estratégia sincroniza automaticamente a propriedade `Volume` interna com este parâmetro.
4. Habilite ou desabilite entradas e saídas compradas/vendidas de acordo com o comportamento desejado.
5. Inicie a estratégia. Negociações serão geradas sempre que a cor MA Rounding Candle realizar as transições configuradas.

O README reflete a implementação em C# contida em `CS/ExpMaRoundingCandleMmrecStrategy.cs` e deve ser usado como documentação de referência para este port.
