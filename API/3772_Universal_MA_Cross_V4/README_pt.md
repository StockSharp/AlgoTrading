# Estratégia Universal MA Cross V4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Universal MA Cross V4** é uma versão StockSharp de alto nível do consultor especialista MetaTrader 4 "Universal MACross EA v4". O algoritmo segue a interação entre uma média móvel rápida configurável e uma média móvel lenta. Ele suporta vários tipos de média móvel, fontes de preços selecionáveis, uma janela de negociação de hora em hora e gerenciamento de posição flexível, incluindo comportamento stop-and-reverse, metas de proteção e trailing stops. A estratégia é projetada para execução baseada em barras usando o StockSharp API de alto nível com assinaturas de velas.

## Lógica de negociação
### Processamento de indicadores
* Duas médias móveis são avaliadas em cada vela finalizada. Cada média móvel pode usar seu próprio comprimento, método de suavização (Simples, Exponencial, Suavizado ou Linear Ponderado) e fonte de preço (fechamento, abertura, máximo, mínimo, mediano, típico ou ponderado).
* O filtro **MinCrossDistancePoints** exige que as médias rápidas e lentas diverjam em pelo menos o número especificado de etapas de preço na barra de cruzamento. Quando **ConfirmedOnEntry** está habilitado, a divergência é validada na vela concluída anterior, reproduzindo o modo "confirmado" do EA original.
* A configuração **ReverseCondition** troca sinais de alta e baixa sem alterar a configuração do indicador.

### Regras de entrada
1. Uma entrada longa ocorre quando a média rápida ultrapassa a média lenta em pelo menos **MinCrossDistancePoints**. Uma entrada curta requer a cruz oposta.
2. Quando **StopAndReverse** for verdadeiro, um sinal oposto fecha a posição ativa antes que novas entradas sejam consideradas.
3. **OneEntryPerBar** evita múltiplas entradas dentro da mesma vela rastreando o carimbo de data/hora do pedido mais recente.
4. O tamanho do pedido é controlado por **TradeVolume**. StockSharp aplica automaticamente este volume às ordens de mercado geradas.

### Gestão de posição
* As distâncias de stop-loss e take-profit são definidas em pontos por meio de **StopLossPoints** e **TakeProfitPoints**. Eles são convertidos em preços absolutos usando a etapa de preço do instrumento. Quando **PureSar** está ativo, toda a lógica de proteção é desativada, assim como a opção "Pure SAR" na versão MQL.
* O gerenciamento de trailing stop reflete a implementação MQL: quando o preço se move além de **TrailingStopPoints** em relação ao nível de entrada, o stop é puxado para trás do mercado pela mesma distância. As paradas finais são ignoradas quando **PureSar** está habilitado.
* Os níveis de proteção são monitorados em cada vela fechada. Se o intervalo da vela violar o stop ou alvo ativo, a estratégia fecha a posição por ordem de mercado para manter o comportamento determinístico nos dados históricos.

### Filtro de sessão
* O sinalizador **UseHourTrade** restringe a negociação à janela inclusiva entre **StartHour** e **EndHour** (0–23). Os limites da sessão terminam por volta da meia-noite quando a hora final é menor que a hora inicial. O gerenciamento de posições, incluindo trailing stops, permanece ativo fora da sessão, mas nenhuma nova entrada é permitida.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Comprimentos das médias móveis rápidas e lentas. |
| `FastMaType`, `SlowMaType` | Métodos de média móvel: Simples, Exponencial, Suavizada ou Linear Ponderada. |
| `FastPriceType`, `SlowPriceType` | Fontes de preços alimentadas em cada média móvel. |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias de proteção nas etapas de preços. Defina como `0` para desativar. |
| `TrailingStopPoints` | Distância do trailing stop em etapas de preço. Defina como `0` para desativar o rastreamento. |
| `MinCrossDistancePoints` | Separação mínima entre as médias necessária para validar um cruzamento. |
| `ReverseCondition` | Troque as regras de alta e de baixa sem alterar os indicadores. |
| `ConfirmedOnEntry` | Valide os sinais na barra fechada anterior. Desative para confirmação imediata. |
| `OneEntryPerBar` | Permita no máximo uma nova posição por vela. |
| `StopAndReverse` | Feche e inverta a posição atual quando o sinal oposto aparecer. |
| `PureSar` | Desative a lógica de stop-loss, take-profit e trailing stop. |
| `UseHourTrade`, `StartHour`, `EndHour` | Filtro de sessão que restringe entradas a um intervalo de horas específico. |
| `TradeVolume` | Volume de pedidos usado por `BuyMarket` e `SellMarket`. |
| `CandleType` | Série de velas inscritas para cálculos de indicadores. |

## Notas de conversão
* As distâncias baseadas em preços são expressas em MetaTrader pontos. O auxiliar `GetPriceOffset` converte esses valores em preços StockSharp usando a etapa de preço do título ou precisão decimal. Isso mantém o comportamento da estratégia alinhado com o EA original, independentemente do instrumento.
* Os trailing stops são gerenciados internamente porque StockSharp estratégias de alto nível operam em velas finalizadas. Essa abordagem determinística garante que os backtests usando velas reproduzam a lógica de rastreamento MT4 pretendida.
* Nenhuma porta Python está incluída, correspondendo à solicitação de conversão. Somente a implementação do C# e a documentação multilíngue são fornecidas neste pacote.
