# Estratégia Diária She Kanskigor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
She Kanskigor Daily Strategy é um sistema de breakout diário que reflete o consultor especialista MetaTrader original `SHE_kanskigor.mq4`. A estratégia avalia a direção da vela diária anterior e abre uma única posição de mercado dentro de uma janela de tempo estreita no início do novo dia de negociação. Ele monitora automaticamente a posição para fechá-la por uma distância configurável de take-profit ou stop-loss, expressa em etapas de preço do título.

## Lógica de negociação
1. Assine velas intradiárias (padrão: 1 minuto) e velas diárias para o título selecionado.
2. Atualize a abertura e fechamento diário armazenado sempre que uma vela diária finalizada chegar.
3. Em cada vela intradiária finalizada:
   - Redefina o sinalizador “negociado hoje” quando uma nova data do calendário começar.
   - Gerencie a posição ativa verificando se o preço de fechamento atinge os limites de stop loss ou take-profit.
   - Verifique se o horário atual está dentro da janela de negociação configurada (início padrão: 00h05, duração da janela: 5 minutos).
   - Se nenhuma posição foi aberta ainda hoje e uma vela diária anterior válida estiver disponível:
     - Opere comprado quando a abertura diária anterior for superior ao fechamento (vela de baixa).
     - Opere vendido quando a abertura diária anterior for inferior ao fechamento (vela de alta).
   - Ignore a negociação quando o dia anterior fechou inalterado.
4. A estratégia executa saídas protetoras usando ordens de mercado assim que o preço de fechamento atinge os limites configurados.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| **Volume** | Volume de pedidos usado para entradas. | `0.1` |
| **Receba lucro** | Meta de lucro expressa em etapas de preço. Um valor de `0` desativa o destino. | `35` |
| **Stop Loss** | Limite de perda expresso em etapas de preço. Um valor de `0` desativa a parada. | `55` |
| **Hora de início** | Hora do dia (fuso horário de câmbio) em que a janela de entrada é iniciada. | `00:05` |
| **Janela (min)** | Duração, em minutos, da janela de entrada. | `5` |
| **Vela intradiária** | Tipo de dados Candle usado para processamento intradiário (padrão: velas de 1 minuto). | `TimeFrameCandleMessage(1m)` |

## Notas
- A estratégia permite apenas uma entrada por dia de negociação.
- Os dados diários das velas devem estar disponíveis; caso contrário, a estratégia espera até que chegue uma vela completa.
- As saídas protetoras operam sobre o preço de fechamento das velas intradiárias finalizadas.
- O código usa StockSharp API de alto nível (`SubscribeCandles` com `Bind`) e segue os padrões de codificação do projeto (guias, comentários em inglês e metadados de parâmetros).
