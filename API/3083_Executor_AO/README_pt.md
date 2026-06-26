# Estratégia Executor AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Executor AO é uma estratégia de estilo saucer (pires) baseada no Awesome Oscillator, originalmente distribuída como o consultor
especialista "Executor AO" do MetaTrader. O porte para StockSharp mantém a detecção de reversão baseada em indicadores enquanto
simplifica o gerenciamento de dinheiro para um tamanho de ordem fixo. A estratégia observa velas completadas do período configurado,
avalia a mudança de inclinação do Awesome Oscillator e abre uma única posição líquida quando ocorrem condições de alta ou baixa abaixo
ou acima da linha zero. O stop de proteção opcional, take-profit e lógica de trailing reproduzem o comportamento de gerenciamento de
risco do EA original.

## Lógica de negociação
1. Assinar a série de velas definida por `CandleType` e alimentar cada vela concluída no Awesome Oscillator com os parâmetros
   `AoShortPeriod` e `AoLongPeriod` configurados.
2. Armazenar os três últimos valores completados do Awesome Oscillator para reproduzir o padrão de acesso ao buffer do MetaTrader
   usado pelo especialista original.
3. Quando nenhuma posição está aberta:
   - **Configuração de alta**: o último valor de AO é maior que o anterior, o valor anterior é menor que o valor de duas barras atrás
     (um vale), e o último valor permanece abaixo de `-MinimumAoIndent`. Nesse caso, enviar uma ordem de compra a mercado com
     `TradeVolume` lotes.
   - **Configuração de baixa**: o último valor de AO é menor que o anterior, o valor anterior é maior que o valor de duas barras atrás
     (um pico), e o último valor permanece acima de `MinimumAoIndent`. Nesse caso, enviar uma ordem de venda a mercado com o volume fixo.
4. Quando existe uma posição, a estratégia emula as saídas do EA:
   - Calcular os preços de stop-loss e take-profit a partir da entrada usando `StopLossPips` e `TakeProfitPips` multiplicados pelo
     tamanho de pip ajustado (o tratamento de 3/5 dígitos do MetaTrader é replicado).
   - Aplicar a regra de trailing stop quando o preço se move a favor da posição por mais de `TrailingStopPips +
     TrailingStepPips` pips. O stop só avança se o novo nível estiver além do anterior, correspondendo ao requisito de passo de
     trailing do EA.
   - Fechar posições compradas quando o preço toca o take-profit ou stop-loss ou quando o valor do Awesome Oscillator da barra anterior
     fica positivo. Fechar posições vendidas quando o preço atinge seus alvos ou o valor anterior de AO cai abaixo de zero.
5. Todas as ordens são a mercado; o modelo de posição líquida do StockSharp garante que apenas uma direção esteja ativa por vez.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Velas de 5 minutos | Período principal usado para calcular e negociar a estratégia. |
| `TradeVolume` | `decimal` | `1` | Tamanho de ordem fixo usado para cada entrada. |
| `AoShortPeriod` | `int` | `5` | Período rápido para a SMA curta do Awesome Oscillator. |
| `AoLongPeriod` | `int` | `34` | Período lento para a SMA longa do Awesome Oscillator. |
| `MinimumAoIndent` | `decimal` | `0.001` | Distância absoluta mínima de zero necessária para novos sinais. Evita negociações quando AO paira em torno de zero. |
| `StopLossPips` | `decimal` | `50` | Distância de stop-loss protetor expressa em pips estilo MetaTrader. Definir como `0` para desativar o stop. |
| `TakeProfitPips` | `decimal` | `50` | Distância de take-profit expressa em pips. Definir como `0` para desativar o alvo. |
| `TrailingStopPips` | `decimal` | `5` | Distância de ativação do trailing stop. Usado somente quando maior que zero. |
| `TrailingStepPips` | `decimal` | `5` | Melhoria mínima de preço necessária antes de atualizar o trailing stop. Deve permanecer positivo quando o trailing está habilitado. |

## Diferenças em relação ao EA do MetaTrader
- A versão do MetaTrader permitia dimensionamento de posição baseado em risco. O porte para StockSharp implementa a opção de lote
  fixo (`TradeVolume`) e deixa o gerenciamento por percentual de risco de fora por clareza.
- O gerenciamento de ordens é simulado dentro da estratégia: quando os limites de stop-loss ou take-profit são atingidos em velas
  completadas, a estratégia envia ordens a mercado para fechar a posição. Isso espelha o comportamento do EA sem criar ordens filho
  separadas.
- Os ajustes de trailing ocorrem em eventos de fechamento de vela, em vez de a cada tick. Isso mantém a implementação consistente
  com a API de alto nível enquanto segue a mesma lógica de limite.
- Todos os caminhos de código dependem do padrão `SubscribeCandles` + `Bind` de alto nível do StockSharp em vez de copiar
  manualmente os buffers do indicador.

## Dicas de uso
- Alinhar `TradeVolume` com o passo de lote do instrumento antes de iniciar a estratégia. O construtor também atribui o mesmo valor
  a `Strategy.Volume`, então os métodos auxiliares usam automaticamente o tamanho escolhido.
- `MinimumAoIndent` pode ser aumentado em mercados ruidosos para evitar mudanças frequentes perto de zero. Defini-lo como `0`
  reproduz o comportamento mais agressivo do EA.
- Ao habilitar o trailing stop, manter `TrailingStepPips` acima de zero; caso contrário, o construtor lança uma exceção, reproduzindo
  a validação de parâmetros do EA original.
- Anexar a estratégia a um gráfico para visualizar tanto as velas quanto o Awesome Oscillator sobreposto. Isso ajuda a validar a
  detecção de vale/pico após a conversão.

## Indicador
- **Awesome Oscillator**: diferença entre uma média móvel simples rápida e uma lenta do preço mediano. A configuração padrão 5/34
  corresponde ao indicador do MetaTrader.
