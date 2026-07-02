# Estratégia PriceMode do Método Cruzado MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **MA Cross Method PriceMode** é uma porta StockSharp direta do MetaTrader 4 especialista "MA_cross_Method_PriceMode". Combina duas médias móveis configuráveis ​​e reage sempre que a média rápida cruza a média lenta. Ambas as linhas expõem as entradas originais MetaTrader: período, método de suavização (SMA, EMA, SMMA, LWMA), preço aplicado (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado) e deslocamento horizontal. A estratégia funciona com qualquer instrumento que forneça velas regulares baseadas no tempo.

## Indicadores
- **Média Móvel Rápida** – comprimento, método e fonte de preço configuráveis. O parâmetro shift MetaTrader é reproduzido armazenando em buffer os valores do indicador concluídos e lendo as barras de valor `FirstShift` de volta.
- **Média Móvel Lenta** – comprimento, método e fonte de preço configuráveis com a mesma emulação de turno via buffer.

## Lógica de negociação
1. A estratégia assina o tipo de vela selecionado e processa apenas velas acabadas para evitar a repintura intra-barra.
2. Para cada barra fechada alimenta ambas as médias móveis com seus respectivos preços aplicados.
3. Quando ambas as médias produzem valores finais, a estratégia avalia duas condições:
   - **Cruz de alta** – a MM rápida estava abaixo ou igual à MM lenta na barra anterior e se move acima dela na barra atual.
   - **Cruz de baixa** – a MM rápida estava acima ou igual à MM lenta na barra anterior e se move abaixo dela na barra atual.
4. Numa linha de alta, a estratégia compra `OrderVolume` contratos. Se uma posição curta estiver aberta, o tamanho da ordem aumenta automaticamente para cobrir a posição curta e estabelecer a nova exposição longa.
5. Numa linha de baixa, a estratégia vende `OrderVolume` contratos. Se uma posição longa estiver aberta, o tamanho da ordem aumenta para fechá-la antes de estabelecer a posição curta.
6. `StartProtection()` é invocado para que StockSharp módulos de proteção possam ser adicionados se desejado (por exemplo, assistentes de stop-loss ou de ponto de equilíbrio).

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `FirstPeriod` | Período da média móvel rápida. | `3` |
| `SecondPeriod` | Período da média móvel lenta. | `13` |
| `FirstMethod` | Método de suavização usado para média móvel rápida (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `SecondMethod` | Método de suavização usado para a média móvel lenta. | `LinearWeighted` |
| `FirstPriceMode` | Preço aplicado para a média móvel rápida (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPriceMode` | Preço aplicado para a média móvel lenta. | `Median` |
| `FirstShift` | Deslocamento horizontal (em barras) aplicado à média móvel rápida. | `0` |
| `SecondShift` | Deslocamento horizontal (em barras) aplicado à média móvel lenta. | `0` |
| `OrderVolume` | Volume base do pedido usado para novas posições. | `0.1` |
| `CandleType` | Tipo/prazo de vela processado pela estratégia. | Velas de 5 minutos |

## Diferenças em comparação com a versão MQL
- A iteração de ordem MetaTrader (`OrdersTotal`, `OrderSelect`, `OrderClose`) é substituída pelo uso direto da propriedade StockSharp `Strategy.Position` e ordens de mercado dimensionadas para reverter a exposição quando necessário.
- O sinalizador MetaTrader "nova barra" não é necessário: `ProcessCandle` é executado exatamente uma vez por vela concluída, garantindo o mesmo comportamento uma vez por barra sem pesquisa em nível de tick.
- O tratamento de deslocamento MA é implementado com buffers compactos que contêm os últimos `shift + 2` valores para cada média. Isso reflete o deslocamento do indicador sem depender de referências anteriores proibidas do indicador (`GetValue`).
- A estratégia é independente do corretor; auxiliares de gerenciamento de risco podem ser anexados por meio de `StartProtection()` em vez dos argumentos fixos de parada/limite MetaTrader.

## Notas de uso
- Escolha a duração da vela que corresponda ao período original (por exemplo, M5 ou H1). Prazos personalizados podem ser fornecidos editando `CandleType` nos parâmetros da estratégia.
- Definir `FirstShift` ou `SecondShift` para um valor positivo atrasa o cruzamento efetivo naquela quantidade de barras concluídas, assim como a entrada de deslocamento horizontal em MetaTrader.
- O modo de preço `Weighted` reproduz a fórmula `(High + Low + 2 * Close) / 4` de MetaTrader. Os modos mediano e típico seguem as definições padrão `(High + Low) / 2` e `(High + Low + Close) / 3`.
- Como cada ordem é uma ordem de mercado, certifique-se de que a configuração da conta tolere o volume solicitado e a derrapagem.
