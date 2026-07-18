# Estratégia Color JFATL Digit Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Color JFATL Digit Duplex é um sistema de módulo duplo convertido do consultor especialista de MetaTrader 5 `Exp_ColorJFatl_Digit_Duplex`. Opera dois fluxos de sinal independentes baseados no indicador Color Jurik Fast Adaptive Trend Line (JFATL). O módulo comprado busca transições altistas no mapa de cores do indicador, enquanto o módulo vendido reage às transições baixistas. Cada lado tem seus próprios parâmetros de suavização, fonte de preço, precisão de arredondamento, deslocamento de barra e offsets de proteção.

A implementação do StockSharp usa a API de alto nível com assinaturas de candles e uma classe de indicador dedicada que reproduz os pesos do kernel FATL e a suavização Jurik. O indicador gera o valor JFATL arredondado junto com os códigos de cor atual e anterior necessários para a detecção de sinais.

## Lógica do indicador
1. **Convolução FATL** – os últimos 39 preços (selecionados pela opção de preço aplicado) são ponderados com os coeficientes FATL originais para produzir uma série filtrada.
2. **Suavização Jurik** – a saída FATL é passada por uma Jurik Moving Average (JMA). O parâmetro de fase é emulado aplicando um ajuste diferencial que desloca o valor suavizado para frente ou para trás.
3. **Arredondamento de dígitos** – o resultado é arredondado para o número especificado de dígitos para imitar a saída "digitalizada" do indicador original.
4. **Atribuição de cor** – o buffer de cor é definido como 2 quando o valor atual sobe, 0 quando cai, e caso contrário herda a cor anterior. Um parâmetro configurável `SignalBar` seleciona qual barra histórica inspecionar, junto com sua barra anterior.

O indicador retorna um valor complexo contendo a leitura JFATL arredondada, a cor em `SignalBar`, a cor anterior e o tempo de fechamento da barra de sinal. Os manipuladores de estratégia usam essa informação para identificar transições de estado exatamente como no código do MetaTrader.

## Regras de negociação
- **Módulo comprado**
  - Abre uma posição comprada quando a cor em `SignalBar` muda para 2 enquanto a cor anterior não era 2 e não há exposição comprada presente.
  - Fecha uma posição comprada existente quando a cor em `SignalBar` se torna 0.
- **Módulo vendido**
  - Abre uma posição vendida quando a cor em `SignalBar` muda para 0 enquanto a cor anterior estava acima de 0 e não há exposição vendida presente.
  - Fecha uma posição vendida existente quando a cor em `SignalBar` se torna 2.
- **Gerenciamento de posição** – as ordens são dimensionadas para eliminar a exposição oposta antes de abrir uma nova negociação no outro lado. `ClosePosition()` é usado para saídas de modo que a estratégia mantém uma única posição líquida a qualquer momento.

## Gestão de risco
Cada módulo tem distâncias individuais de stop-loss e take-profit expressas em passos de preço. Quando uma nova posição é aberta, a estratégia registra o preço de entrada e calcula os níveis de proteção usando o `PriceStep` atual do instrumento. Em cada atualização do indicador, a máxima/mínima do candle correspondente é testada contra os níveis armazenados:

- Para negociações compradas, a estratégia fecha a posição se a mínima do candle atingir o preço de stop ou a máxima do candle atingir o preço de take-profit.
- Para negociações vendidas, a lógica é espelhada usando a máxima do candle para o stop e a mínima para o take-profit.

Desabilitar o stop ou take definindo a distância como zero deixa a negociação sem gerenciamento até que o indicador emita um sinal de saída.

## Parâmetros
| Grupo | Parâmetro | Descrição |
| --- | --- | --- |
| Geral | `LongCandleType` | Período usado para a assinatura do indicador comprado. |
| Geral | `ShortCandleType` | Período usado para a assinatura do indicador vendido. |
| Indicador (Long) | `LongJmaLength` | Comprimento da média móvel Jurik para o módulo comprado. |
| Indicador (Long) | `LongJmaPhase` | Ajuste de fase aplicado à saída JMA comprada (intervalo −100…100). |
| Indicador (Long) | `LongAppliedPrice` | Fonte de preço aplicado usada na convolução FATL. |
| Indicador (Long) | `LongDigit` | Número de dígitos usados para arredondar o valor do indicador. |
| Indicador (Long) | `LongSignalBar` | Offset de barra histórica inspecionada para sinais (0 = barra fechada atual). |
| Risco (Long) | `LongStopLossPoints` | Distância de stop-loss para comprados medida em passos de preço. |
| Risco (Long) | `LongTakeProfitPoints` | Distância de take-profit para comprados medida em passos de preço. |
| Negociação (Long) | `EnableLongOpen` | Habilita ou desabilita novas entradas compradas. |
| Negociação (Long) | `EnableLongClose` | Habilita ou desabilita saídas compradas geradas pelo indicador. |
| Indicador (Short) | `ShortJmaLength` | Comprimento da média móvel Jurik para o módulo vendido. |
| Indicador (Short) | `ShortJmaPhase` | Ajuste de fase aplicado à saída JMA vendida. |
| Indicador (Short) | `ShortAppliedPrice` | Fonte de preço aplicado para o indicador vendido. |
| Indicador (Short) | `ShortDigit` | Número de dígitos usados para arredondar o valor do indicador vendido. |
| Indicador (Short) | `ShortSignalBar` | Offset de barra histórica inspecionada para sinais vendidos. |
| Risco (Short) | `ShortStopLossPoints` | Distância de stop-loss para vendidos medida em passos de preço. |
| Risco (Short) | `ShortTakeProfitPoints` | Distância de take-profit para vendidos medida em passos de preço. |
| Negociação (Short) | `EnableShortOpen` | Habilita ou desabilita novas entradas vendidas. |
| Negociação (Short) | `EnableShortClose` | Habilita ou desabilita saídas vendidas geradas pelo indicador. |

## Notas de uso
1. Atribua tipos de candle apropriados para os módulos comprado e vendido. Eles podem apontar para diferentes períodos se desejado.
2. Configure o preço aplicado e os dígitos de arredondamento para corresponder às características do instrumento do Consultor Especialista original.
3. O parâmetro `SignalBar` controla quantos candles fechados atrás o sinal é validado. Defina-o como 1 para replicar o padrão MT5 (candle completado anterior).
4. Certifique-se de que a propriedade `Volume` da estratégia reflita o tamanho de negociação desejado. Ao reverter posições, a estratégia adiciona automaticamente a magnitude da exposição existente para que a posição líquida mude corretamente.
5. Stops e alvos dependem do `PriceStep` do instrumento. Para instrumentos sem um tamanho de tick definido, os offsets padrão para passos numéricos brutos.

## Notas de conversão
- O parâmetro de fase Jurik no StockSharp é emulado aplicando um ajuste diferencial de avanço/atraso porque o `JurikMovingAverage` empacotado não expõe uma propriedade de fase direta. Isso preserva o comportamento do especialista original, incluindo respostas agressivas ou atrasadas.
- A estratégia usa um modelo de posição líquida única. A versão MetaTrader poderia executar múltiplas ordens por direção; no StockSharp a lógica as consolida em uma exposição comprada ou vendida por vez.
- Os níveis de proteção são avaliados em cada fechamento de candle do indicador em vez de em cada tick. Isso corresponde à frequência de sinal do especialista MT5 e mantém a implementação dentro das diretrizes da API de alto nível.

## Arquivos
- `CS/ColorJfatlDigitDuplexStrategy.cs` – implementação da estratégia com o indicador personalizado.
- `README.md` / `README_zh.md` / `README_ru.md` – documentação em inglês, chinês e russo.
