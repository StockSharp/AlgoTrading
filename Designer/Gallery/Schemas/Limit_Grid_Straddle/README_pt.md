# Diagrama da estratégia de straddle com grade limitada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama inicia cada ciclo da grade com duas ordens limitadas simétricas ao redor do último candle de cinco minutos concluído. A execução de uma ordem inicial agenda um nível adicional na mesma direção, a proteção com lucro absoluto encerra a posição resultante e uma solicitação adiada de cancelamento em massa remove os limites restantes.

![schema](schema.svg)

## Visão geral da estratégia

- Quando a posição está zerada, uma compra limitada é colocada 100 unidades de preço abaixo do fechamento e uma venda limitada 100 unidades acima.
- A execução de qualquer ordem inicial mantém ativa a ordem oposta e agenda um nível da grade na mesma direção para o próximo candle concluído.
- O nível adicional de compra fica 350 unidades abaixo da execução inicial; o nível adicional de venda fica 350 unidades acima.
- Cada execução inicial ou da grade é enviada ao Position protection com distância absoluta de lucro de 300 e sem stop loss.
- Uma saída de proteção agenda o cancelamento em massa para o próximo candle concluído. A confirmação desse cancelamento libera o próximo ciclo da grade.

## Regras de entrada e saída

- **Lado comprador**: No início de um ciclo com posição zerada, registrar uma compra limitada em `Close - Start Offset`. Após sua execução, registrar outra compra em `Average Fill Price - Grid Distance - Step Distance` no próximo candle concluído.
- **Lado vendedor**: No início de um ciclo com posição zerada, registrar uma venda limitada em `Close + Start Offset`. Após sua execução, registrar outra venda em `Average Fill Price + Grid Distance + Step Distance` no próximo candle concluído.
- **Ordens pendentes**: Uma execução inicial não cancela o limite oposto. Os limites iniciais e da grade restantes permanecem ativos até a etapa de cancelamento em massa.
- **Saída**: Position protection envia uma saída a mercado quando o fechamento do candle alcança um alvo a 300 unidades de preço de uma execução protegida. Nenhum limite de stop loss fica habilitado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Período dos candles concluídos que controlam o ciclo. |
| Start Offset | 100 | Distância entre o fechamento e cada limite inicial, em unidades de preço. |
| Grid Distance | 300 | Distância-base de uma execução inicial ao nível adicional do mesmo lado. |
| Step Distance | 50 | Incremento somado a Grid Distance para o nível adicional. |
| Take Profit | 300 | Distância absoluta de uma execução protegida ao seu alvo de lucro. |
| Stop Loss | 0 | Distância absoluta do stop; zero desativa esse limite. |
| Trailing Stop Loss | false | Mantém desativado o deslocamento do stop loss. |
| Use Market Orders | true | Envia as saídas de proteção como ordens a mercado. |
| Volume | 1 | Volume de cada ordem limitada inicial e da grade. |

## Detalhes do diagrama

- A posição é amostrada a cada candle concluído e comparada com zero. Um bloco Flag permite apenas um par inicial simétrico por ciclo da grade.
- Quatro blocos Order registering enviam os limites iniciais e da grade de compra e venda. Não há blocos de cancelamento direcionado entre as duas ordens iniciais.
- Uma execução inicial é guardada em um bloco Variable. Um Delay de dois eventos consome o candle que gerou a execução e libera a negociação armazenada no candle concluído seguinte, mantendo o novo registro fora do retorno de execução.
- A negociação guardada é convertida por `Order.AveragePrice`; depois, blocos Formula aplicam `Grid Distance + Step Distance`, que totaliza 350 com os padrões.
- Position protection trata separadamente cada execução recebida. Este exemplo limitado não calcula um alvo comum ponderado por volume para várias execuções da grade.
- Uma execução de proteção arma outro Delay de dois eventos. Sua saída solicita Order mass cancellation, e somente um resultado bem-sucedido redefine o Flag do ciclo.
- O gráfico mostra candles de cinco minutos, os quatro fluxos de ordens e todas as execuções ou saídas da estratégia.

## Uso

Importe o arquivo `.json` no Designer, execute o diagrama com dados históricos no testador e ajuste as distâncias e o volume à escala de preço e à volatilidade do instrumento antes de operar ao vivo.
