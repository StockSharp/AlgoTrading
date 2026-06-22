# Estratégia Proper Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia Proper Bot** é um sistema de trading em grade convertido do assessor especialista original do MetaTrader 4 "Proper Bot". A estratégia abre uma cesta de ordens com viés direcional, expande essa cesta usando um mapa configurável de distância/volume e gerencia todo o ciclo com uma combinação de filtros baseados em tempo, volume e preço. O port em C# depende das subscrições de velas e indicadores de alto nível do StockSharp para manter a implementação próxima ao fluxo de trabalho de trading gerenciado.

## Princípios de operação
1. **Detecção de sinal**
   - Quando o filtro EMA está habilitado, a estratégia rastreia médias móveis exponenciais rápida, média e lenta na série de velas selecionada. Os cruzamentos entre as médias rápida e lenta geram a direção, enquanto a média intermediária bloqueia negociações que ainda não confirmaram a tendência.
   - Quando o filtro está desabilitado, o algoritmo simplesmente reutiliza a direção do corpo da vela finalizada anterior.
2. **Filtros pré-negociação**
   - Uma média móvel simples do volume de velas impõe um requisito mínimo de volume médio.
   - O trading só é permitido entre os horários configuráveis de início e fim de sessão.
   - Níveis de preço superior e inferior impedem compras muito altas ou vendas muito baixas. Movimentos extremos além dessas bandas também podem forçar uma entrada na direção correspondente.
3. **Expansão da grade**
   - A ordem de mercado inicial usa o parâmetro `FirstVolume`. As adições subsequentes seguem o parâmetro `GridMap`, que contém uma lista de pares `distância/volume`. Quando o preço se move contra a posição atual pela distância configurada, uma nova ordem do volume mapeado é adicionada.
   - As distâncias são interpretadas em passos de preço usando o `PriceStep` do instrumento. Se o valor não estiver disponível, a estratégia recorre a `0.0001`.
4. **Gestão de risco**
   - Toda a cesta compartilha um take profit agregado e distância de stop-loss medidos a partir do preço de entrada médio ponderado.
   - Uma saída trailing monitora a soma do lucro flutuante de todas as ordens abertas. Uma vez que o lucro excede o limiar de ativação, qualquer recuo maior que `TrailStepPoints` fecha todo o ciclo.
   - Se alguma condição de saída for acionada, a estratégia fecha a posição completa com uma ordem de mercado e redefine o estado da grade.

## Parâmetros
| Parâmetro | Descrição | Valor padrão |
|-----------|-----------|--------------|
| `FastMaPeriod` | Comprimento da EMA rápida usada no filtro de entrada. | 10 |
| `MidMaPeriod` | Comprimento opcional da EMA intermediária que deve ficar entre as linhas rápida e lenta para confirmar um sinal. Definir como 0 para desabilitar. | 25 |
| `SlowMaPeriod` | Comprimento da EMA lenta usada no filtro de entrada. | 50 |
| `DisableMaFilter` | Quando habilitado, a estratégia ignora as regras EMA e segue a direção da vela anterior. | true |
| `VolumePeriod` | Número de velas usadas para calcular a média do volume. Um valor de 0 desabilita o filtro. | 1 |
| `VolumeMinimum` | Volume médio mínimo necessário para permitir novas entradas. | 69 |
| `HighLevel` | Limiar de preço que bloqueia entradas compradas acima dele e pode forçar vendidas. | 1.50001 |
| `LowLevel` | Limiar de preço que bloqueia entradas vendidas abaixo dele e pode forçar compradas. | 1.40001 |
| `FirstVolume` | Volume usado para a primeira ordem de cada ciclo de grade. | 0.08 |
| `GridMap` | Lista de pares `distância/volume` (pontos separados por espaços) definindo o quanto o preço deve se mover antes de adicionar a próxima ordem e qual volume usar. | `120/0.1 ... 120/0.19` |
| `TakeProfitPoints` | Distância de lucro (em passos de preço) aplicada ao preço de entrada médio ponderado para toda a cesta. | 10000 |
| `StopLossPoints` | Distância de perda (em passos de preço) aplicada ao preço de entrada médio ponderado para toda a cesta. | 30000 |
| `TrailStartPoints` | Lucro flutuante mínimo necessário antes que a lógica trailing possa ser armada. | 52 |
| `TrailDistancePoints` | Distância de lucro que deve ser alcançada (menos o passo de trailing) antes que a lógica trailing seja ativada. | 52 |
| `TrailStepPoints` | Máximo retrocesso de lucro tolerado quando a lógica trailing está ativa. | 2 |
| `StartHour` / `StartMinute` | Início da sessão de trading (inclusive). | 06:00 |
| `FinishHour` / `FinishMinute` | Fim da sessão de trading (inclusive, suporta janelas noturnas). | 21:00 |
| `CandleType` | Tipo de dados de velas processado pela estratégia. | Período de 1 minuto |

## Notas de uso
- Os valores de `GridMap` são analisados usando decimais de cultura invariante. Certifique-se de que as distâncias estejam expressas em pontos do instrumento antes da barra e os volumes depois.
- Todas as distâncias de risco são convertidas usando o `PriceStep` do instrumento. Se o instrumento expõe um tamanho de tick diferente, configure `PriceStep` adequadamente antes de iniciar a estratégia.
- A implementação trailing agrega o lucro flutuante de todas as ordens abertas (correspondendo ao EA original), mas realiza a verificação em velas concluídas. Saídas rápidas intrabarra podem ser habilitadas executando a estratégia em períodos menores.
- As entradas forçadas produzidas ao ultrapassar `HighLevel` ou `LowLevel` usam o preço de fechamento da vela como aproximação dos valores bid/ask.
- O port do StockSharp fecha toda a cesta quando uma condição de take profit, stop-loss ou trailing é atendida. Isso difere da implementação MT4 onde cada ticket carrega seu próprio stop/alvo, mas simplifica o gerenciamento de ordens de alto nível.

## Diferenças em relação à versão MT4
- O EA MT4 enviava níveis de proteção individuais com cada ordem. A implementação do StockSharp calcula saídas contra a posição combinada para permanecer dentro da API de alto nível.
- Os preços bid/ask são aproximados com o preço de fechamento da vela porque as subscrições de velas do StockSharp não entregam spreads por tick por padrão.
- O bloco trailing usa o maior entre `TrailDistancePoints - TrailStepPoints` e `TrailStartPoints` como limiar de ativação para que o comportamento permaneça estável mesmo quando os parâmetros se sobrepõem.
- Os horários de trading dependem do `DateTimeOffset` da vela recebida. Certifique-se de que o feed de dados forneça marcas de tempo no fuso horário desejado.

## Arquivos
- `CS/ProperBotStrategy.cs` – implementação da estratégia.
- `README.md` – descrição em inglês.
- `README_zh.md` – tradução para o chinês.
- `README_ru.md` – tradução para o russo.
