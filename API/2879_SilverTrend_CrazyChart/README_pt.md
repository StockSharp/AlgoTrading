# Estratégia SilverTrend CrazyChart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista MetaTrader "Exp_SilverTrend_CrazyChart" usando a API de alto nível do StockSharp. Ela negocia em ambos os lados do mercado comparando dois buffers do indicador personalizado SilverTrend CrazyChart. Quando a banda atrasada cruza a banda atual, abre uma posição na direção da banda dominante e fecha qualquer exposição oposta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A barra de sinal finalizada anterior mostra a banda atual acima da banda atrasada, e na barra avaliada a banda atual cai abaixo ou toca a banda atrasada. As entradas compradas podem ser desabilitadas com `AllowBuyEntry`.
  - **Vendido**: A barra de sinal finalizada anterior mostra a banda atual abaixo da banda atrasada, e na barra avaliada a banda atual sobe acima ou toca a banda atrasada. As entradas vendidas podem ser desabilitadas com `AllowSellEntry`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Posições compradas fecham quando a banda atrasada ultrapassa a banda atual (`AllowBuyExit`) ou quando os limites de stop-loss/take-profit são acionados.
  - Posições vendidas fecham quando a banda atual ultrapassa a banda atrasada (`AllowSellExit`) ou quando os limites de stop-loss/take-profit são acionados.
- **Stops**: Usa deslocamentos de preço absolutos especificados por `StopLossPoints` e `TakeProfitPoints`. Se qualquer valor for zero, esse limite é ignorado.
- **Filtros**:
  - `SignalBar` seleciona quantas velas completadas para trás a lógica de cruzamento é avaliada.
  - `CandleType` controla o período usado para todos os cálculos.

## Parâmetros

- `CandleType` – Série de velas usada para o indicador (padrão: velas de 1 hora).
- `Length` – Período de oscilação (`SSP`) passado ao indicador SilverTrend CrazyChart.
- `KMin` – Coeficiente de canal inferior que controla a distância da banda atrasada.
- `KMax` – Coeficiente de canal superior que controla a distância da banda atual.
- `SignalBar` – Número de velas completadas para trás usadas para avaliar o cruzamento (equivalente ao `SignalBar` original).
- `AllowBuyEntry` / `AllowSellEntry` – Ativar/desativar entradas compradas/vendidas.
- `AllowBuyExit` / `AllowSellExit` – Ativar/desativar o fechamento de posições compradas/vendidas existentes.
- `StopLossPoints` – Distância de preço absoluta da entrada para stop-loss comprado e take-profit vendido.
- `TakeProfitPoints` – Distância de preço absoluta da entrada para take-profit comprado e stop-loss vendido.
- `Volume` – Volume de estratégia herdado que define o tamanho base da ordem.

## Lógica do indicador

O `SilverTrendCrazyChartIndicator` incluído reproduz os buffers MQL originais:

- `Length`, `KMin` e `KMax` calculam um canal de oscilação a partir da máxima mais alta e mínima mais baixa sobre a janela de retrospectiva.
- A banda "atual" corresponde ao buffer 0 no MetaTrader e reage imediatamente à última barra.
- A banda "atrasada" é o buffer 1, que desloca a banda atual em `Length + 1` barras para corresponder à lógica de desenho original.

Uma compra é acionada quando a banda atrasada, atuando como filtro de tendência, cruza acima da banda atual, enquanto uma venda aparece quando a relação se inverte. O parâmetro `SignalBar` garante que apenas velas completadas participem da decisão, igualando o comportamento do especialista original.
