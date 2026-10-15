# Estratégia Binario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Binario é um sistema de rompimento com entrada por stop que circunda o preço com dois envelopes de média móvel calculados nas máximas e mínimas das velas. Quando o preço opera entre os envelopes, a estratégia coloca ordens stop simétricas para capturar a próxima expansão direcional. As ordens herdam offsets fixos de stop-loss e take-profit que espelham o consultor especialista do MetaTrader 5.

O port para StockSharp mantém a ideia central enquanto aproveita recursos de API de alto nível como assinaturas de velas, vinculação de indicadores e gerenciamento automatizado de ordens. Dados de Nível-1 são consumidos para estimar o spread bid/ask atual, necessário para reproduzir os offsets de entrada originais.

## Lógica de operação
1. Construir duas médias móveis (superior nas máximas, inferior nas mínimas) usando métodos e período configuráveis.
2. Quando o último fechamento está entre as médias:
   - Colocar uma ordem buy-stop acima da média superior mais o buffer de diferença configurado e o spread atual.
   - Colocar uma ordem sell-stop abaixo da média inferior menos o mesmo buffer.
3. Cada ordem pendente armazena seus próprios níveis de stop-loss e take-profit derivados das médias móveis, `PointValue` e parâmetros baseados em pips.
4. Quando uma ordem é executada, a ordem pendente oposta é cancelada e novas ordens de proteção (stop-loss e take-profit) são registradas para a posição aberta.
5. A lógica de stop de seguimento ajusta o stop quando o preço avança pelo menos `TrailingStopPips + TrailingStepPips` a partir do preço de entrada, correspondendo ao comportamento incremental da implementação MQL.
6. Quando a posição muda de comprada para vendida (ou vice-versa), as ordens de proteção existentes são canceladas para evitar conflitos.

## Parâmetros
- `CandleType` – período usado para os cálculos.
- `MaPeriod` – comprimento de ambas as médias móveis.
- `MaShift` – deslocamento de barra aplicado a cada média móvel (0 reproduz o comportamento padrão do EA).
- `HighMaMethod` / `LowMaMethod` – métodos de suavização (`SMA`, `EMA`, `SMMA`, `WMA`, `LWMA`).
- `PointValue` – valor de preço absoluto que representa um pip para o símbolo negociado (0.0001 para a maioria dos pares FX principais, 0.01 para pares JPY, etc.).
- `DifferencePips` – buffer entre as médias e as ordens pendentes, expresso em pips.
- `TakeProfitPips` – distância da meta de lucro em pips.
- `TrailingStopPips` – distância do stop de seguimento em pips (definir como zero para desabilitar o seguimento).
- `TrailingStepPips` – lucro adicional mínimo em pips necessário antes de ajustar o stop novamente.
- `Volume` (herdado de `Strategy`) – tamanho de ordem base; ordens de reversão adicionam automaticamente o tamanho absoluto da posição para inverter completamente a exposição.

Todos os parâmetros baseados em pips são convertidos em preços absolutos via `PointValue`, espelhando a conversão `Point * digits_adjust` realizada na versão MT5.

## Gerenciamento de ordens
- Ordens stop pendentes permanecem ativas apenas enquanto a estratégia está plana em seu lado respectivo (sem posição comprada para um novo buy-stop, sem posição vendida para um novo sell-stop).
- Após um entrada ser acionada, a estratégia envia ordens de stop-loss e take-profit correspondentes e remove o stop-entry oposto não utilizado.
- Reversões de posição cancelam ordens de proteção existentes antes de registrar novas, prevenindo stops órfãos.

## Comportamento de seguimento
- Posições compradas: uma vez que o preço ganha pelo menos `TrailingStopPips + TrailingStepPips` pips, o stop é deslocado para `close - TrailingStopPips` enquanto o movimento exceder o stop anterior em pelo menos `TrailingStepPips`.
- Posições vendidas: quando o preço cai pelo mesmo limiar, o stop é baixado para `close + TrailingStopPips`, também respeitando o filtro de passo.
- O seguimento usa o fechamento da vela mais recente como substituto do valor `PriceCurrent()` do MT5.

## Requisitos de dados
- Velas para o `CandleType` selecionado.
- Cotações de Nível-1 para recuperar os melhores preços bid/ask e calcular o spread. Quando o spread não estiver disponível, a estratégia recorre ao passo de preço mínimo do instrumento ou `PointValue`.

## Diferenças em relação à versão MetaTrader 5
- O dimensionamento de posição é controlado por meio da propriedade `Volume` do StockSharp em vez da combinação Lots/Risk original.
- Ordens de proteção são recriadas quando o seguimento modifica preços, porque as ordens stop do StockSharp não podem ser alteradas no lugar.
- Os preços de execução relatados por MyTrades são aproximados pelos preços de ordens armazenados; ajuste `PointValue` e os parâmetros de pip para corresponder às especificações do broker.
- A estratégia é executada em velas terminadas, equivalente a habilitar "especialista a cada tick" com avaliação de abertura de barra no script MT5.

## Notas de uso
1. Defina `PointValue` de acordo com a relação tick-a-pip do instrumento.
2. Configure métodos e período de média móvel para corresponder ao seu modelo MT5.
3. Escolha distâncias de pip adequadas para os componentes de diferença, take-profit e seguimento.
4. Certifique-se de que os dados de Nível-1 estejam disponíveis para que o componente de spread possa ser aplicado com precisão.
