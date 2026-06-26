# Estratégia de Contagem de Sinais com Array
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz a lógica do expert advisor MetaTrader 4 `Signal-COunt-with array.mq4`.
Ela monitora os extremos do canal Donchian para um conjunto configurável de offsets de preço e conta com que frequência
a saída do indicador muda, fica vazia ou retorna a um valor de sinal. A implementação mantém
o foco diagnóstico do script original: nenhuma operação é executada. Em vez disso, a estratégia imprime
estatísticas detalhadas sempre que um novo máximo/mínimo é registrado ou quando o log por candle está habilitado.

## Conceito

- Substituir a pesquisa `iCustom` original de `super_signals_v2_alert` por um canal Donchian que
  fornece a máxima mais alta e a mínima mais baixa durante `ChannelPeriod` candles.
- Avaliar uma grade de offsets (`GapStart`, `GapStep`, `GapCount`) que emulam as múltiplas configurações de indicadores
  testadas pelo script MQL.
- Para cada offset rastrear seis contadores que espelham os arrays originais, incluindo transições para e
  fora do valor sentinela (`2147483647` para leituras superiores vazias e `-2147483646` para leituras inferiores vazias).
- Gerar uma tabela de texto com os contadores acumulados para que o usuário possa inspecionar com que frequência cada buffer
  produz um novo sinal, retorna ao vazio ou sai do estado zero padrão.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 5 minutos | Série de candles usada para os cálculos de Donchian. |
| `ChannelPeriod` | 24 | Número de candles usados para determinar as máximas e mínimas de Donchian. |
| `GapStart` | 0 | Primeiro offset (em múltiplos do passo de preço) aplicado aos valores de sinal virtuais. |
| `GapStep` | 1 | Tamanho do passo (em passos de preço) entre offsets consecutivos. |
| `GapCount` | 8 | Número de offsets a avaliar (corresponde ao loop 0..7 original). |
| `LogOnEachCandle` | false | Quando habilitado, força o log após cada candle terminado. |

## Contadores

Cada offset mantém duas linhas: o índice `0` representa o buffer superior do Donchian (sinal altista) e
o índice `1` representa o buffer inferior (sinal baixista). As seguintes estatísticas são coletadas:

- **Changed** – incrementa sempre que o valor bruto do indicador difere da observação anterior.
- **Empty** – conta quantas vezes o buffer retornou o sentinela positivo (`2147483647`).
- **NegEmpty** – conta ocorrências do sentinela negativo (`-2147483646`), principalmente para o buffer inferior.
- **Zero** – rastreia transições do estado zero padrão para qualquer valor diferente de zero.
- **NewFromEmpty** – incrementa quando um sinal baseado em preço real substitui o valor sentinela.
- **BackToEmpty** – incrementa quando o buffer retorna ao seu sentinela após manter um valor não sentinela.

Esses contadores correspondem um a um com os arrays mantidos no Expert Advisor original
(`GetInd_iCustom_changed`, `GetInd_iCustom_maxInt`, `GetInd_iCustom_minInt`, etc.).

## Registro

A estratégia imprime diagnósticos através de `AddInfoLog` em duas situações:

1. Sempre que a banda superior do Donchian sobe ou a banda inferior desce, indicando um novo extremo.
2. Cada candle terminado quando `LogOnEachCandle` está definido como `true`.

Cada entrada de log começa com o tempo do candle e depois lista os contadores para cada offset, facilitando
a comparação do comportamento entre diferentes configurações de indicadores virtuais.

## Notas de uso

- Anexar a estratégia a qualquer instrumento; ela depende apenas de candles históricos e não envia ordens.
- Ajustar `ChannelPeriod` para corresponder à volatilidade do instrumento que está sendo estudado. Um período mais longo
  imita uma detecção de swing mais ampla similar ao indicador MT4.
- Aumentar `GapCount` se precisar observar mais offsets. Os arrays são redimensionados automaticamente no início.
- Combinar os diagnósticos com desenhos de gráfico (candles mais canal Donchian) para alinhar visualmente as
  estatísticas impressas com a estrutura do mercado.
