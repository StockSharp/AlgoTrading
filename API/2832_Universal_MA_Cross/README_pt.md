# Estratégia Universal MA Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Universal MA Cross** é uma conversão direta do consultor especialista MQL5 original "UniversalMACrossEA" para o framework de estratégias de alto nível do StockSharp. O algoritmo compara uma média móvel rápida e uma lenta que podem ser configuradas com diferentes métodos de cálculo e fontes de preço. Filtros opcionais controlam como os sinais são confirmados, se as negociações são revertidas imediatamente, como o gerenciamento de risco é realizado e quando a estratégia tem permissão para negociar.

## Lógica de trading
### Processamento de indicadores
* Duas médias móveis são calculadas na série de candles selecionada. Cada média pode usar seu próprio período, método de suavização (SMA, EMA, SMMA ou LWMA) e tipo de preço (fechamento, abertura, máxima, mínima, mediana, típico ou ponderado).
* O parâmetro **MinCrossDistance** requer que as médias rápida e lenta divirjam pelo menos o número especificado de unidades de preço na barra de cruzamento.
* Quando **ConfirmedOnEntry** está habilitado, o cruzamento é validado na barra completada anterior (equivalente a usar índices de barra 2 e 1 no EA original). Se estiver desabilitado, a barra finalizada atual é comparada com a barra anterior, replicando o comportamento do "modo tick" da versão MQL.
* Configurar **ReverseCondition** troca os sinais de alta e baixa para que as regras possam ser invertidas sem alterar nenhuma configuração de indicador.

### Regras de entrada
1. Para uma entrada comprada, a média rápida deve cruzar acima da média lenta pelo menos **MinCrossDistance**. Para uma entrada vendida, a média rápida deve cruzar abaixo da média lenta essa distância.
2. Quando **StopAndReverse** está habilitado e um sinal oposto chega, a posição ativa é fechada antes de novas ordens serem consideradas.
3. Se **OneEntryPerBar** for verdadeiro, a estratégia lembra o tempo de barra da última entrada e recusa abrir outra negociação durante o mesmo candle.
4. O volume de cada ordem é configurado pelo parâmetro **Volume**.

### Gerenciamento de posições
* Os níveis de stop-loss e take-profit são medidos em unidades de preço. Eles são ignorados quando **PureSar** é verdadeiro, correspondendo ao modo "Pure SAR" do especialista original.
* A lógica de trailing stop é ativada após o preço se mover **TrailingStop + TrailingStep** a partir do preço de entrada. Cada movimento adicional de pelo menos **TrailingStep** pontos aperta o stop pela distância **TrailingStop** especificada. O trailing não funciona no modo "Pure SAR".
* Os níveis de proteção são monitorados a cada candle finalizado. Se o range do candle violar o nível de stop-loss ou take-profit, a posição é fechada por ordem a mercado.

### Filtro de sessão
* Quando **UseHourTrade** está habilitado, a estratégia negocia apenas quando a hora de abertura do candle está entre **StartHour** e **EndHour** (inclusive). O gerenciamento do trailing stop continua funcionando fora desse intervalo, mas nenhuma nova entrada ou ação de stop-and-reverse é executada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `FastMaPeriod`, `SlowMaPeriod` | Períodos das médias móveis rápida e lenta. |
| `FastMaType`, `SlowMaType` | Métodos de média móvel: Simple, Exponential, Smoothed (RMA) ou Linear Weighted. |
| `FastPriceType`, `SlowPriceType` | Fontes de preço alimentadas nas médias. |
| `StopLoss`, `TakeProfit` | Distâncias de proteção em unidades de preço absolutas. Definir como 0 para desabilitar. |
| `TrailingStop`, `TrailingStep` | Offset do trailing stop e movimento extra mínimo necessário antes de deslocar o stop. |
| `MinCrossDistance` | Distância mínima entre as médias na barra de cruzamento. |
| `ReverseCondition` | Trocar regras de alta e baixa. |
| `ConfirmedOnEntry` | Usar apenas barras completadas para validação. |
| `OneEntryPerBar` | Permitir no máximo uma entrada por candle. |
| `StopAndReverse` | Fechar a posição atual e reverter em sinais opostos. |
| `PureSar` | Desabilitar a lógica de stop-loss, take-profit e trailing. |
| `UseHourTrade`, `StartHour`, `EndHour` | Filtro de tempo para sessões de trading (horas 0–23). |
| `Volume` | Volume de ordem para cada posição. |
| `CandleType` | Tipo de dados de candles para cálculos. |

## Notas de conversão
* As ordens de proteção são tratadas internamente verificando as máximas e mínimas dos candles, porque as estratégias do StockSharp operam em candles finalizados em vez de eventos de tick brutos. Isso espelha o comportamento do especialista original enquanto permanece dentro da API de alto nível.
* Os ajustes do trailing stop seguem a implementação MQL, exigindo um movimento de **TrailingStop + TrailingStep** antes que o stop seja deslocado.
* Nenhuma versão em Python é fornecida nesta conversão, conforme solicitado.
