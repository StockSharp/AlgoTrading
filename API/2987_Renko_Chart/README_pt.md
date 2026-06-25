# Estratégia Renko Chart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
RenkoChartStrategy é uma conversão direta do consultor especialista original **RenkoChart.mq5**. Em vez de colocar ordens, a estratégia se concentra em recriar o fluxo de trabalho do símbolo Renko personalizado dentro do StockSharp. Ela assina dados de tick, produz um fluxo de candles Renko com um tamanho de tijolo configurável e o expõe através da plataforma para que possa ser visualizado ou encaminhado para outros componentes. Cada tijolo completado é registrado com o último tick que o acionou, permitindo ao operador validar a série gerada contra a implementação MQL.

## Mapeamento do Consultor Especialista MQL
- **StartDateTime** → `StartTime`: o carimbo de tempo inicial usado ao semear o histórico Renko.
- **BaseSymbol** → `Strategy.Security`: o StockSharp já atribui o instrumento base, portanto o parâmetro foi substituído confiando no instrumento selecionado. A estratégia ainda prefixa o nome do fluxo gerado com `RenkoPrefix` para imitar a convenção de nomenclatura "Renko-\<symbol\>".
- **Mode (Bid/Last)** → `UseBidTicks`: alterna se atualizações de oferta ou ticks de negociação impulsionam o feed de monitoramento ao vivo.
- **Range** → `BrickSizeSteps`: número de passos de preço que formam um tijolo Renko. A estratégia multiplica o valor pelo `PriceStep` do instrumento para obter o tamanho absoluto da caixa.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `StartTime` | `DateTimeOffset` | 2018‑08‑01 09:00:00 UTC | Tijolos com um tempo de abertura antes deste momento são ignorados, correspondendo ao comportamento de aquecimento original. |
| `BrickSizeSteps` | `int` | 5 | Tamanho do tijolo Renko expresso em passos de preço. Convertido para preço absoluto quando a série Renko é criada. |
| `UseBidTicks` | `bool` | `false` | Quando `false` a estratégia escuta ticks de negociação, quando `true` escuta atualizações de oferta para emular o modo MQL `Bid`. |
| `RenkoPrefix` | `string` | `"Renko-"` | Prefixo adicionado às mensagens de log para que o nome do fluxo corresponda à convenção de nomenclatura de símbolos personalizados. |

> **Nota:** a propriedade calculada `BrickSize` expõe o tamanho absoluto da caixa e pode ser útil ao conectar a estratégia com outros componentes que esperam um delta de preço em vez de contagens de passos.

## Fluxo de dados
1. `GetWorkingSecurities` configura uma assinatura de candles Renko usando `RenkoBuildFrom.Points` e o tamanho de caixa calculado.
2. `OnStarted` lança a assinatura Renko, assina ticks de negociação ou de oferta (dependendo de `UseBidTicks`) e desenha o fluxo Renko no gráfico se houver um disponível.
3. `ProcessTrade` / `ProcessLevel1` armazenam o preço e o carimbo de tempo do tick mais recente para fins de registro.
4. `ProcessCandle` ignora tijolos não terminados, filtra dados anteriores a `StartTime` e registra cada tijolo completado com os níveis de fechamento anterior e novo junto com as informações do último tick.

## Dicas de uso
- Anexe a estratégia a qualquer instrumento que forneça negociações ou atualizações de nível 1. O fluxo Renko aparecerá na área de gráfico padrão com o prefixo configurado.
- Como a implementação não envia ordens, ela pode ser executada em paralelo com outras estratégias de negociação para fornecer uma visão Renko sincronizada do mercado.
- As entradas de log contêm tanto a direção do tijolo quanto o tick acionador. Isso é útil ao comparar a saída com dados históricos exportados do MetaTrader.

## Diferenças em relação à versão MQL
- O StockSharp já gerencia símbolos, portanto a criação explícita de símbolos personalizados foi substituída por saída de log e gráfico.
- Todos os cálculos usam aritmética decimal em vez de arrays, confiando no construtor de candles Renko integrado.
- A estratégia adota o modelo de assinatura e o auxiliar de proteção do StockSharp, tornando-a pronta para ser estendida com lógica de negociação se necessário.
