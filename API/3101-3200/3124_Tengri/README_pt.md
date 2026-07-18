# Estratégia de Tengri (Port do StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma recriação de alto nível no StockSharp do consultor especializado MetaTrader *Tengri*. O consultor original negocia EURUSD e USDCHF com uma abordagem de grade e escala impulsionada por RSI, filtros de volatilidade "Silence" personalizados e um medidor de tendência EMA. A versão em C# mantém o núcleo comportamental enquanto o adapta às convenções do StockSharp e à contabilização de posição líquida.

## Ideias Principais

- **Viés direcional** – compara o bid atual com o preço de abertura de um candle de período superior (padrão 30 minutos). Uma diferença positiva inclina a estratégia para comprado, uma diferença negativa para vendido.
- **Filtro de momentum** – um RSI de 14 períodos calculado em candles horários deve permanecer abaixo de 70 para entradas compradas e acima de 30 para entradas vendidas, correspondendo à lógica do MetaTrader.
- **Filtros de mercado quieto** – o indicador "Silence" personalizado original é emulado com valores ATR suavizados por EMAs em dois períodos diferentes. Ambos os filtros devem permanecer abaixo de limiares configuráveis para permitir entradas ou escalonamentos.
- **Confirmação de tendência** – uma EMA em um período médio garante que adições compradas ocorram apenas acima da EMA e adições vendidas apenas abaixo.
- **Sizing de grade e martingale** – o primeiro trade usa um lote fixo ou proporcional ao patrimônio. Trades adicionais multiplicam o volume anterior por fatores configuráveis (1.70 antes de `StepX`, 2.08 depois por padrão).
- **Espaçamento de pips** – a distância entre ordens de grade segue dois passos base (10 pips e 20 pips por padrão) e pode crescer exponencialmente através de `PipStepExponent`.

## Fluxo de Negociação

1. **Avaliação de entrada** (por `EntryCandleType`, padrão M1):
   - Determinar a direção a partir do candle `DealCandleType`.
   - Verificar RSI e o primeiro filtro de silêncio.
   - Garantir que não há trades ativos na mesma direção (posições na direção oposta são zeradas primeiro pois os portfólios do StockSharp são líquidos).
   - Enviar uma ordem de mercado com o tamanho de lote calculado. O primeiro trade armazena um alvo de take-profit baseado em pips.
2. **Avaliação de escalonamento** (por `ScaleCandleType`, padrão M1):
   - Confirmar a tendência EMA e o segundo filtro de silêncio.
   - Verificar se o último preço de execução está suficientemente distante do mercado atual usando as regras de pip-step.
   - Adicionar outra ordem de mercado com sizing martingale enquanto a direção permanecer válida e o número de trades estiver abaixo de `MaxTrades`.
3. **Gestão de posição**:
   - O alvo de lucro global opcional fecha a posição quando existem pilhas compradas e vendidas e o PnL não realizado combinado supera `Equity / LimitDivisor`.
   - O take-profit do primeiro trade atua como saída simples: quando o bid/ask atinge o alvo armazenado, toda a posição líquida é zerada.
   - Nenhum stop-loss automático é usado, espelhando o código MetaTrader.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `DealCandleType` | Período cujo preço de abertura define o viés direcional. |
| `EntryCandleType` | Período para avaliar sinais de entrada. |
| `ScaleCandleType` | Período para verificar adições de grade. |
| `MaCandleType` | Período para o filtro de tendência EMA. |
| `Silence1CandleType` / `Silence2CandleType` | Períodos para filtros de volatilidade baseados em ATR. |
| `RsiPeriod` | Comprimento do RSI (padrão 14). |
| `SilencePeriod1/2`, `SilenceInterpolation1/2`, `SilenceLevel1/2` | Suavização ATR e limiares que controlam os dois filtros de silêncio. |
| `MaPeriod` | Período da EMA. |
| `PipStep`, `PipStep2`, `PipStepExponent` | Distâncias entre trades de escalonamento. |
| `LotExponent1`, `LotExponent2`, `StepX` | Fatores martingale para posições adicionais. |
| `LotSize`, `FixLot`, `LotStep` | Configurações de gestão monetária para a primeira posição. |
| `SlTpPips` | Distância em pips para definir um take-profit para o primeiro trade (0 desativa). |
| `MaxTrades` | Número máximo de entradas por direção. |
| `UseLimit`, `LimitDivisor` | Configuração de bloqueio de lucro global. |
| `CloseFriday`, `CloseFridayHour` | Bloqueio opcional de entrada no final de sexta-feira. |

## Diferenças da Versão MetaTrader

- **Substituição do indicador Silence** – o indicador "Silence" proprietário é aproximado com valores ATR suavizados por EMAs. Os limiares mantêm a mesma escala numérica, mas podem ser ajustados se o proxy ATR se comportar de maneira diferente.
- **Contabilização de posição líquida** – os portfólios do StockSharp são líquidos, portanto a estratégia zera a direção oposta antes de abrir uma nova pilha em vez de proteger ambos os lados simultaneamente.
- **Tratamento de take-profit** – o MetaTrader anexa TP apenas à primeira ordem. O port fecha toda a posição líquida quando esse alvo é acionado. Ordens adicionais intencionalmente não têm TP, correspondendo ao modelo de risco original.
- **Escolha de símbolo** – a estratégia usa o `Security` atribuído à instância da estratégia. Configure instâncias separadas para EURUSD, USDCHF ou qualquer outro instrumento.

## Notas de Uso

- Configure o passo de volume e os volumes mín/máx no instrumento alvo para que o arredondamento estilo `LotCheck` se alinhe com os requisitos do corretor.
- A estratégia assume que as cotações do corretor fornecem atualizações do melhor bid/ask. Sem dados de Nível 1, as verificações de direção e TP não podem funcionar.
- Como não há stop-loss, considere executar a estratégia com controles de risco externos (stop de patrimônio, supervisão manual, etc.).

## Visualização

Para analisar o comportamento, conecte widgets de gráfico às séries de candles assinadas (períodos de direção, entrada e escalonamento) e sobreponha os indicadores EMA e ATR. Isso espelha as ferramentas de diagnóstico usadas com o consultor original.
