# Estratégia BSS de Separação Triple EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia BSS de Separação Triple EMA** é uma portagem do StockSharp do expert advisor do MetaTrader 5 "BSS 1_0" (MQL ID 20591). A abordagem monitora três médias móveis com janelas de lookback crescentes e aguarda que elas se expandam por pelo menos uma distância configurável. Quando as médias rápida, média e lenta estão devidamente separadas, a estratégia entra na direção da tendência respeitando um período de espera entre preenchimentos e um limite no tamanho total da posição.

Esta implementação mantém o comportamento central do robô original enquanto expõe a configuração através de objetos `StrategyParam` do StockSharp. Todos os comentários e documentação são escritos em inglês conforme solicitado.

## Lógica de Trading

1. Assinar um único fluxo de velas definido pelo parâmetro `CandleType` e calcular três médias móveis (rápida, média, lenta). Cada média pode usar um método de suavização diferente (simples, exponencial, suavizado ou linearmente ponderado).
2. Para uma **configuração comprada** as seguintes condições devem ser atendidas em uma vela terminada:
   - `MA Lenta - MA Média >= MinimumDistance`.
   - `MA Média - MA Rápida >= MinimumDistance`.
3. Para uma **configuração vendida** a separação inversa é necessária:
   - `MA Rápida - MA Média >= MinimumDistance`.
   - `MA Média - MA Lenta >= MinimumDistance`.
4. Antes de abrir uma operação a estratégia garante:
   - Todos os indicadores estão totalmente formados e a estratégia tem permissão para negociar (`IsFormedAndOnlineAndAllowTrading`).
   - A pausa desde a última entrada (`MinimumPauseSeconds`) decorreu.
   - Adicionar um novo lote não violará o limite de exposição `MaxPositions`.
5. Em um sinal de entrada, a estratégia primeiro fecha qualquer posição aberta na direção oposta. Somente após a próxima vela ela considera abrir uma posição na nova direção, refletindo o comportamento do EA MQL original.
6. Quando uma nova posição é aberta ou escalada, o tempo de preenchimento é armazenado para impor o período de espera entre entradas.

Nenhum stop-loss ou take-profit automático é usado. O gerenciamento de risco é alcançado através do filtro de distância, a pausa entre operações e o número máximo de lotes permitidos por direção.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | 0.1 | Volume usado para cada ordem de entrada. A posição líquida está limitada a `OrderVolume * MaxPositions`. |
| `MaxPositions` | 2 | Número máximo de lotes (por direção) que podem ser mantidos simultaneamente. |
| `MinimumDistance` | 0.0005 | Lacuna de preço mínima necessária entre médias móveis vizinhas. Escolha um valor apropriado para o instrumento (para um par FX de 5 dígitos, 0.0005 equivale a 5 pips). |
| `MinimumPauseSeconds` | 600 | Período de espera em segundos entre novas entradas. Fechar operações não reinicia o temporizador; apenas entradas o fazem. |
| `FirstMaPeriod` | 5 | Período da média móvel mais rápida. Deve ser estritamente menor que `SecondMaPeriod`. |
| `FirstMaMethod` | Exponential | Método de suavização usado para a média móvel rápida (Simple, Exponential, Smoothed, LinearWeighted). |
| `SecondMaPeriod` | 25 | Período da média móvel média. Deve ser estritamente menor que `ThirdMaPeriod`. |
| `SecondMaMethod` | Exponential | Método de suavização usado para a média móvel média. |
| `ThirdMaPeriod` | 125 | Período da média móvel lenta. |
| `ThirdMaMethod` | Exponential | Método de suavização usado para a média móvel lenta. |
| `CandleType` | Período de 1 minuto | Fonte de dados de velas usada para cálculos de indicadores e avaliação de sinais. |

## Notas de Implementação

- A API de alto nível do StockSharp é usada: `SubscribeCandles` transmite dados, e `.Bind` alimenta as médias móveis e o manipulador de sinais simultaneamente.
- As médias móveis são instanciadas no início da estratégia de acordo com os métodos selecionados. A configuração padrão corresponde ao EA original (três MAs exponenciais sobre preços de fechamento).
- `StartProtection()` é invocado para habilitar as ferramentas de monitoramento de posição integradas fornecidas pelo StockSharp.
- A estratégia substitui `OnPositionChanged` para registrar o tempo das entradas. Esse timestamp é comparado com `MinimumPauseSeconds` para manter o comportamento de período de espera da versão MetaTrader.
- Posições opostas são niveladas antes que novas sejam consideradas, garantindo que a exposição líquida nunca mude de sinal sem primeiro passar por zero, assim como a implementação original onde todas as posições vendidas eram fechadas antes de abrir posições compradas.

## Diretrizes de Uso

1. Selecione um instrumento e garanta que seu tamanho de tick seja refletido no valor `MinimumDistance`. Por exemplo:
   - EURUSD (preços de 5 dígitos): `0.0005` equivale a 5 pips.
   - USDJPY (preços de 3 dígitos): `0.05` equivale a 5 pips.
2. Ajuste os períodos e métodos da média móvel para se adequar ao regime de mercado que você está atacando.
3. Aumente `MinimumPauseSeconds` em períodos mais lentos para evitar overtrading, ou diminua em períodos mais baixos se a estrutura do mercado permitir entradas frequentes.
4. Teste diferentes valores de `MaxPositions` em combinação com o tamanho do contrato do seu broker para alinhar a exposição ao seu plano de risco.

## Limitações Comparado com a Versão MQL

- O expert MetaTrader permitia selecionar fontes de preço alternativas (abertura, máxima, mínima, etc.). A portagem do StockSharp atualmente opera apenas em preços de fechamento, o que corresponde à configuração padrão do robô original.
- A portagem usa um modelo de posição líquida (positivo para comprados, negativo para vendidos). Quando `MaxPositions` é atingido, nenhum lote adicional é adicionado até que a exposição seja reduzida, reproduzindo o efeito do contador de posição por elemento original.

Com essas considerações você pode reproduzir o comportamento da estratégia BSS original dentro do ecossistema StockSharp e estendê-la com controles de risco adicionais ou análises conforme necessário.
