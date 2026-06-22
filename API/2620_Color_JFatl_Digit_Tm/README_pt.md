# Estratégia Color JFATL Digit TM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Color JFATL Digit TM** é um port do consultor especialista original do MetaTrader 5 que combina uma FATL (Fast Adaptive Trend Line) filtrada por Jurik com transições de estado baseadas em cor e um filtro de sessão de negociação opcional. A estratégia monitora a inclinação da linha FATL suavizada: cada barra é classificada como de alta (cor = 2), de baixa (cor = 0) ou neutra (cor = 1). Mudanças nesses estados de cor acionam entradas, saídas e gestão de posições respeitando as horas de sessão configuráveis, as distâncias de stop-loss e take-profit.

## Lógica
1. **Replicação do indicador personalizado**
   - O valor FATL é calculado convolucionalando o preço aplicado selecionado com a tabela de pesos original de 39 coeficientes.
   - O resultado é suavizado usando `JurikMovingAverage` do StockSharp. Se a biblioteca expuser uma propriedade `Phase`, ela é configurada via reflexão para espelhar os parâmetros do MT5.
   - O valor suavizado é arredondado para a precisão do instrumento multiplicando o passo de preço por `10^DigitRounding`, reproduzindo o parâmetro `Digit` do MQL5.
   - A diferença entre o valor arredondado atual e o anterior define a cor da barra (`2 = subindo`, `0 = caindo`, `1 = sem alteração / herdado`).

2. **Avaliação de sinais**
   - Um buffer circular mantém os códigos de cor mais recentes. O parâmetro `SignalBar` seleciona quantas barras completas pular (padrão = 1, ou seja, barra fechada anterior).
   - Uma **entrada comprada** é acionada quando a cor precedente era de alta (`2`) e a mais recente é qualquer coisa que não seja de alta (`< 2`).
   - Uma **entrada vendida** é acionada quando a cor precedente era de baixa (`0`) e a mais recente é qualquer coisa que não seja de baixa (`> 0`).
   - Uma **saída comprada** ocorre quando a cor precedente se torna de baixa (`0`).
   - Uma **saída vendida** ocorre quando a cor precedente se torna de alta (`2`).
   - As entradas são ignoradas quando já existe uma posição, replicando o comportamento de posição única do expert MT5.

3. **Controle de sessão e proteção**
   - A filtragem de sessão opcional (`EnableTimeFilter`) espelha a lógica de hora/minuto do MT5, incluindo sessões noturnas quando a hora de início é maior que a hora de fim.
   - Quando o trading está fora da janela permitida, todas as posições abertas são liquidadas imediatamente, correspondendo ao expert original.
   - As distâncias de stop-loss e take-profit expressas em pontos são convertidas em unidades de preço usando o passo de preço do instrumento e passadas para `StartProtection`.

## Parâmetros
- `OrderVolume` – volume por ordem (usado para entradas de compra e venda).
- `EnableTimeFilter`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute` – configurações da janela de sessão.
- `StopLossPoints`, `TakeProfitPoints` – distâncias de proteção em pontos (0 desabilita a respectiva proteção).
- `BuyOpenEnabled`, `SellOpenEnabled`, `BuyCloseEnabled`, `SellCloseEnabled` – habilitar ou desabilitar entradas e saídas compradas/vendidas individualmente.
- `SignalCandleType` – período usado para o indicador personalizado e os sinais de negociação (padrão candles de 4 horas).
- `JmaLength`, `JmaPhase` – configurações de suavização Jurik (fase aplicada quando o indicador subjacente a expõe).
- `AppliedPriceMode` – enumeração de preço aplicado idêntica à versão MT5 (fechamento, abertura, mediana, típico, variantes TrendFollow, Demark, etc.).
- `DigitRounding` – multiplicador de arredondamento que imita o parâmetro `Digit` do indicador MQL.
- `SignalBar` – quantas barras fechadas olhar para trás ao avaliar transições de cor (padrão 1).

## Notas
- A estratégia usa `SubscribeCandles` e helpers de ordem de alto nível (`BuyMarket`, `SellMarket`) conforme recomendado pelas diretrizes de conversão do StockSharp.
- A fase Jurik é aplicada via reflexão; se a implementação em tempo de execução não expuser uma propriedade `Phase`, o comportamento padrão é usado automaticamente.
- O arredondamento requer um `Security.PriceStep` válido. Quando não disponível, os valores do indicador permanecem sem arredondamento.

## Uso
1. Conecte a estratégia a um instrumento e conexão capaz de fornecer o `SignalCandleType` configurado.
2. Configure o preço aplicado, os parâmetros de Jurik, os horários de sessão e os parâmetros de gestão de capital conforme desejado.
3. Inicie a estratégia; ela gerenciará uma única posição, respeitando as proteções de stop-loss/take-profit e os sinais baseados em cor descritos acima.
