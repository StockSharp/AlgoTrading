# Estratégia Adaptive Renko Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Adaptive Renko Duplex** é um port do StockSharp do consultor especialista original `Exp_AdaptiveRenko_Duplex.mq5`. A versão convertida mantém a ideia de executar **dois fluxos independentes de Adaptive Renko** – um dedicado a configurações altistas e outro a baixistas – enquanto expõe a lógica através da API de alto nível. Cada fluxo constrói trilhos de suporte e resistência no estilo Renko cuja altura do tijolo se adapta dinamicamente à volatilidade recente. A estratégia reage a reversões de tendência detectadas dentro desses trilhos e pode manter configurações assimétricas para os lados comprado e vendido.

Ao contrário dos sistemas clássicos de trading Renko, que operam com tijolos sintéticos, a abordagem duplex escuta candles padrão e recalcula continuamente os buffers de Renko adaptativo. Os sinais são gerados apenas em candles completamente terminados para evitar repintagem e para corresponder ao modelo orientado por eventos do StockSharp.

## Dados de mercado e indicadores
- **Assinaturas de candles** – dois parâmetros `DataType` independentes selecionam as séries de candles que alimentam os fluxos de Renko comprado e vendido. Podem apontar para o mesmo período ou para diferentes.
- **Reconstrução do Adaptive Renko** – cada fluxo incorpora a lógica original do indicador. Um tamanho mínimo de tijolo (expresso em pontos) é comparado com `K × volatilidade` e o maior define a nova altura do tijolo. O indicador rastreia envelopes superior/inferior mais níveis de tendência coloridos (suporte em tendências de alta, resistência em tendências de baixa).
- **Fontes de volatilidade** – escolher entre um indicador `AverageTrueRange` ou `StandardDeviation`. Ambos operam na série de candles usada pelo respectivo fluxo e aceitam comprimentos de retrocesso personalizados.

## Lógica de trading
1. **Detecção do lado comprado**
   - O fluxo comprado constrói tijolos adaptativos usando os parâmetros configurados.
   - Quando a linha de tendência de alta (`RenkoTrend.Up`) aparece na barra atrasada definida por `LongSignalBarOffset`, a estratégia emite uma ordem de compra de mercado. O tamanho da ordem é `Volume + |Position|`, permitindo reversões imediatas de vendido para comprado.
   - Se uma linha de tendência de baixa for detectada após o atraso configurado e `LongExitsEnabled` for verdadeiro, toda a exposição comprada é fechada.
2. **Detecção do lado vendido**
   - O fluxo vendido espelha a lógica: um sinal `RenkoTrend.Down` produz uma venda de mercado, enquanto `RenkoTrend.Up` na barra atrasada sai de vendidos quando `ShortExitsEnabled` está habilitado.
3. **Atraso de sinal** – ambos os lados respeitam seus parâmetros `SignalBarOffset`, reproduzindo o deslocamento de uma barra usado pelo expert do MetaTrader. Definir o deslocamento como zero reage no candle finalizado mais recente.
4. **Dimensionamento de posição** – a versão do StockSharp depende da propriedade `Volume` da estratégia. Sempre configurá-la antes de iniciar a estratégia.

## Gerenciamento de risco
- **Stop-loss / take-profit** – as distâncias são especificadas em **pontos** e multiplicadas pelo `PriceStep` do instrumento para produzir preços absolutos. Os stops são verificados quando um candle assinado fecha. Como o StockSharp não cria automaticamente ordens de proteção do lado do servidor, as saídas são tratadas por ordens de mercado.
- **Rastreamento de estado** – a estratégia armazena o preço no qual a última entrada comprada ou vendida foi executada (baseado no fechamento do candle) para poder avaliar a distância ao stop ou alvo.
- **Substituições manuais** – módulos padrão de `Stop` ou `Protective` podem ser anexados chamando `StartProtection()` externamente se for necessário gerenciamento de risco em nível de conta.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `LongCandleType` | Candles de 4 horas | Série de candles usada para calcular sinais comprados. |
| `ShortCandleType` | Candles de 4 horas | Série de candles usada para calcular sinais vendidos. |
| `LongVolatilityMode` | ATR | Fonte de volatilidade (`AverageTrueRange` ou `StandardDeviation`) para tijolos comprados. |
| `ShortVolatilityMode` | ATR | Fonte de volatilidade para tijolos vendidos. |
| `LongVolatilityPeriod` | 10 | Período de retrocesso para o indicador de volatilidade comprado. |
| `ShortVolatilityPeriod` | 10 | Período de retrocesso para o indicador de volatilidade vendido. |
| `LongSensitivity` | 1.0 | Multiplicador aplicado ao valor de volatilidade antes de construir tijolos comprados. |
| `ShortSensitivity` | 1.0 | Multiplicador aplicado ao valor de volatilidade antes de construir tijolos vendidos. |
| `LongPriceMode` | Close | Entrada de preço (`HighLow` ou `Close`) usada para atualizar os trilhos de Renko comprado. |
| `ShortPriceMode` | Close | Entrada de preço usada para atualizar os trilhos de Renko vendido. |
| `LongMinimumBrickPoints` | 2 | Altura mínima de tijolo para o fluxo comprado, medida em pontos. |
| `ShortMinimumBrickPoints` | 2 | Altura mínima de tijolo para o fluxo vendido. |
| `LongSignalBarOffset` | 1 | Atraso (em barras) antes de confirmar um sinal comprado. |
| `ShortSignalBarOffset` | 1 | Atraso (em barras) antes de confirmar um sinal vendido. |
| `LongEntriesEnabled` | true | Alternar para permitir ou bloquear entradas compradas. |
| `LongExitsEnabled` | true | Alternar para permitir ou bloquear saídas compradas orientadas por Renko. |
| `ShortEntriesEnabled` | true | Alternar para permitir ou bloquear entradas vendidas. |
| `ShortExitsEnabled` | true | Alternar para permitir ou bloquear saídas vendidas orientadas por Renko. |
| `LongStopLossPoints` | 1000 | Distância de stop-loss para posições compradas (pontos × `PriceStep`). |
| `LongTakeProfitPoints` | 2000 | Distância de take-profit para posições compradas. |
| `ShortStopLossPoints` | 1000 | Distância de stop-loss para posições vendidas. |
| `ShortTakeProfitPoints` | 2000 | Distância de take-profit para posições vendidas. |

> **Conversão de pontos** – a versão MQL usou a definição de "ponto" do broker. No StockSharp cada distância é multiplicada por `Security.PriceStep` (ou `Security.MinStep` como fallback) para converter pontos em incrementos de preço absolutos. Ajuste os padrões para o tamanho de tick do seu instrumento.

## Diretrizes de uso
1. **Configurar o ambiente** – atribuir `Security`, `Portfolio` e `Volume` antes de iniciar a estratégia. Certificar-se de que a fonte de dados pode entregar todos os períodos de candles configurados.
2. **Personalizar ambos os fluxos** – pode manter a configuração simétrica padrão ou atribuir diferentes períodos/modos de volatilidade aos lados comprado e vendido para comportamento assimétrico.
3. **Monitorar registros** – a estratégia emite mensagens `LogInfo` em cada entrada e saída, indicando o nível de Renko que desencadeou a ação. Usar esses registros para validar que os sinais correspondem às expectativas.
4. **Combinar com módulos externos** – filtros adicionais (controle de sessão, proteção de capital, etc.) podem ser anexados através das APIs de alto nível do StockSharp porque a estratégia expõe os sinais na classe `Strategy` principal.
5. **Considerações de backtesting** – ao testar com dados históricos, preferir construtores de candles que possam reconstruir os períodos necessários para que o Renko adaptativo permaneça consistente.

## Diferenças em relação ao consultor especialista original
- Recursos específicos do MetaTrader (números mágicos, modos de gerenciamento de dinheiro, tratamento de desvios, notificações push) são intencionalmente omitidos. O dimensionamento de posição depende exclusivamente da propriedade `Volume` do StockSharp.
- O EA original colocava ordens de stop-loss e take-profit do lado do servidor. A versão convertida verifica as distâncias configuradas em cada candle finalizado e fecha via ordens de mercado.
- Os sinais são avaliados estritamente em candles completos para evitar recálculos de barra parcial. Isso espelha a verificação `IsNewBar` usada na implementação MQL.
- A reconstrução do Renko adaptativo segue o algoritmo publicado mas é implementada em C# sem criar objetos indicadores adicionais, o que mantém o caminho de atualização eficiente enquanto respeita as convenções da API de alto nível do StockSharp.

## Melhorias recomendadas
- Combinar o fluxo duplex com filtros de regime de nível superior (horários de sessão, filtros de volatilidade) para evitar operar em condições ilíquidas.
- Anexar módulos de trailing-stop ou proteções baseadas em capital via `StartProtection()` para salvaguardas em nível de conta.
- Registrar ou plotar os trilhos de suporte/resistência gerados para validar visualmente a estratégia durante a revisão discricional.
