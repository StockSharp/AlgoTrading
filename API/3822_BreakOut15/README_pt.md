# Estratégia BreakOut15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
BreakOut15 é uma estratégia de breakout de 15 minutos convertida do MetaTrader 4 consultor especialista "BreakOut15.mq4". A estratégia combina um filtro cruzado de média móvel com execução de breakout e proteção de rastreamento em vários estágios. Os pedidos são enviados por meio do StockSharp API de alto nível e dependem apenas de velas finalizadas.

## Lógica principal
1. Calcule duas médias móveis configuráveis (rápida e lenta) usando o método, período, mudança e preço aplicado selecionados.
2. Quando a média rápida ultrapassar a média lenta, agende um preço de rompimento longo em `Close + BreakoutLevel * PriceStep`. Um cruzamento de baixa agenda um pequeno rompimento em `Close - BreakoutLevel * PriceStep`.
3. Os preços de rompimento pendentes serão cancelados se a condição de cruzamento desaparecer, o horário de negociação terminar ou um rompimento na direção oposta se tornar ativo.
4. As entradas no mercado são executadas assim que a vela ultrapassa o nível pendente e as verificações de patrimônio e risco são aprovadas.
5. As posições abertas são gerenciadas por stop-loss, take-profit e um dos três modos de trailing-stop. Crossbacks de média móvel forçam uma saída imediata.
6. Filtros de tempo opcionais evitam novas negociações fora da janela configurada e podem liquidar posições no final das sextas-feiras.

## Gestão de capital
* **UseMoneyManagement / TradeSizePercent** – permite dimensionamento baseado em risco. O tamanho da posição é igual à parte inteira de `floor(equity * percent / 10000) / 10`, com mínimo de 1 lote.
* **FixedVolume** – tamanho de reserva quando o gerenciamento de dinheiro está desativado ou o patrimônio não está disponível.
* **MaxVolume** – limita qualquer volume computado.
* **MinimumEquity** – bloqueia novas negociações quando o patrimônio cai abaixo do limite.

## Gestão de risco
* **StopLossPips / TakeProfitPips** – compensações de proteção clássicas medidas em pips (convertidas por meio da etapa de preço do instrumento).
* **UseTrailingStop** – ativa o tratamento de parada dinâmica quando uma posição existe.
* **TrailingStopType**
  * `Immediate`: trilha pela distância de stop-loss original imediatamente.
  * `Delayed`: espere por `TrailingStopPips` de lucro antes de seguir nessa distância.
  * `MultiLevel`: bloqueie os ganhos em três marcos programáveis (`Level1/2/3TriggerPips`) e depois siga até `Level3TrailingPips`.

## Cronograma de Negociação
* **UseTimeLimit, StartHour, StopHour** – permite negociação apenas dentro do intervalo de horas especificado.
* **UseFridayClose, FridayCloseHour** – opcionalmente, achatar todas as posições na sexta-feira.

## Indicadores e Dados
* **Médias móveis rápidas/lentas** – escolha entre os métodos Simples, Exponencial, Suavizado, Linear Ponderado ou Mínimos Quadrados.
* **Modos de preços aplicados** – reproduz fontes de preços MT4 (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado).
* **CandleType** – o padrão é velas de período de 15 minutos, mas pode ser alterado se necessário.

## Notas adicionais
* A estratégia sincroniza automaticamente os preços de entrada, stop e alvo com o preço médio atual da posição, de modo que os ajustes finais reflitam os preenchimentos reais.
* Todos os cálculos dependem do instrumento `PriceStep`; garantir que corresponda ao mercado negociado.
* Os testes devem validar o acionamento de rompimentos, transições de trailing-stop e regras de arredondamento de gerenciamento de dinheiro em cenários de alta e baixa.
