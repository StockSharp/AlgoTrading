# Estratégia de Histograma de Volume XDeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista original do MetaTrader **Exp_XDeMarker_Histogram_Vol** sobre a API de alto nível do StockSharp. Ela transforma o oscilador DeMarker em um histograma ponderado por volume, suaviza tanto o oscilador quanto o volume com médias móveis configuráveis, e reage a mudanças de regime quando o histograma cruza bandas predefinidas.

A lógica é deliberadamente simétrica. Posições compradas são abertas quando o histograma entra em uma das zonas altistas, enquanto as vendidas são abertas quando ele se move para zonas baixistas. Sinais opostos fecham a posição ativa e, se habilitado, invertem imediatamente a direção.

## Conceito

1. **DeMarker ponderado por volume**
   - O DeMarker é calculado com o período selecionado.
   - O oscilador é escalado para o intervalo `[-50; +50]` e multiplicado pelo volume de vela escolhido.
   - Uma média móvel suaviza o oscilador ponderado. A mesma média móvel é aplicada ao volume em si. Apenas quatro tipos de média móvel são fornecidos (simples, exponencial, suavizada, ponderada) porque estes estão disponíveis nativamente no StockSharp.
2. **Níveis dinâmicos**
   - Quatro multiplicadores definidos pelo usuário (`HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2`) definem os limiares altistas e baixistas.
   - Os limiares são escalados pelo volume suavizado de modo que maior participação amplia o intervalo aceitável.
3. **Máquina de estados**
   - Cada vela terminada é classificada em um de cinco estados: `0` (altista extremo), `1` (altista), `2` (neutro), `3` (baixista), `4` (baixista extremo).
   - Os sinais são gerados quando o estado da última vela fechada (deslocada por `SignalBar`) difere do estado anterior de uma forma que indica uma transição para território altista ou baixista.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Período principal. Padrão de velas de 2 horas para espelhar o consultor especialista original. |
| `DeMarkerPeriod` | Período do oscilador DeMarker. |
| `HighLevel1` / `HighLevel2` | Multiplicadores positivos que definem o primeiro e segundo limiar altista. |
| `LowLevel1` / `LowLevel2` | Multiplicadores negativos que definem o primeiro e segundo limiar baixista. |
| `Smoothing` | Tipo de média móvel para o histograma e o volume. Escolhas: Simple, Exponential, Smoothed, Weighted. |
| `SmoothingLength` | Comprimento das médias de suavização. |
| `SignalBar` | Número de barras fechadas usadas para comparação de sinais. `1` significa "usar a vela fechada mais recentemente". |
| `VolumeType` | Fonte de volume. Ambas as opções recorrem ao volume de vela porque o StockSharp não expõe contagens de ticks em todos os feeds. |
| `EnableLongEntries` / `EnableShortEntries` | Permitir abrir novas posições na direção respectiva. |
| `EnableLongExits` / `EnableShortExits` | Permitir fechar posições existentes quando o setup oposto aparecer. |

## Sinais e Gestão de Posições

- **Entrar comprado**: a última barra de sinal transiciona para o estado `1` ou `0` enquanto a barra anterior estava em um estado de número maior (>1). As posições vendidas são opcionalmente fechadas antes de entrar.
- **Entrar vendido**: a última barra de sinal transiciona para o estado `3` ou `4` enquanto a barra anterior estava em um estado de número menor (<3 ou <4 respectivamente). As posições compradas são opcionalmente fechadas antes de entrar.
- **Saída**: sempre que um sinal oposto é acionado e as saídas estão habilitadas para a direção atual. `ClosePosition()` é usado para nivelar antes de reverter.
- **Dimensionamento de posição**: a estratégia se baseia na propriedade padrão `Strategy.Volume`. Os blocos de gestão monetária da versão MetaTrader (dois IDs "mágicos" separados) são intencionalmente simplificados.

## Notas de Implementação

- Apenas velas terminadas são processadas. A estratégia assina o período configurado via `SubscribeCandles().WhenNew(ProcessCandle)`.
- A implementação do DeMarker mantém somas contínuas dos valores DeMax/DeMin para corresponder aos cálculos do MetaTrader e aguarda até que barras suficientes sejam acumuladas antes de emitir sinais.
- Se os dados de volume estiverem faltando, o histograma degrada graciosamente para zero porque tanto o oscilador ponderado quanto os limiares serão zero.
- Os modos de suavização não suportados do indicador original (JJMA, JurX, ParMA, T3, VIDYA, AMA) não são reproduzidos. Escolha a alternativa mais próxima via parâmetro `Smoothing`.
- O buffer `SignalBar` mantém apenas o histórico mínimo necessário (atual, anterior e um slot extra) para imitar o comportamento original de `CopyBuffer` e evitar sinais desatualizados.

## Dicas de Uso

- Iniciar a estratégia no Designer ou Runner após configurar o período e volume desejados.
- Otimizar `DeMarkerPeriod`, `SmoothingLength` e os multiplicadores de limiar juntos — pequenas mudanças nos limiares alteram materialmente a cadência de entradas.
- Como o histograma é ponderado por volume, a qualidade do feed importa. Use provedores de dados que relatem volume de vela confiável para capturar o efeito desejado.
- Considere adicionar módulos externos de gestão monetária ou risco se precisar de regras de stop-loss ou take-profit; eles não estavam presentes na conversão de alto nível.
