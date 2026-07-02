# Laptrend_1 Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Laptrend_1 reproduz a lógica do consultor especialista MetaTrader **Laptrend_1.mq4**. A estratégia combina um filtro de canal LabTrend de vários períodos de tempo, confirmação de impulso Fisher Transform e uma verificação de força de tendência ADX em velas de 15 minutos. Os pedidos são abertos somente quando as direções do LabTrend de período de tempo superior (H1) e de sinal (M15) concordam, a transformação de Fisher confirma o movimento e o ADX mostra uma tendência de fortalecimento. As posições são fechadas quando o impulso se inverte, a direção do LabTrend muda ou o mercado transita para um regime plano onde ADX e os componentes DI convergem.

## Lógica de negociação
- **Dados primários** – velas de 15 minutos geram entradas/saídas, enquanto velas de 1 hora alimentam o filtro LabTrend de longo prazo.
- **Canal LabTrend** – O código recria o indicador `LabTrend1_v2.1` construindo canais no estilo Donchian nas últimas `ChannelLength` barras e estreitando-os com o `RiskFactor`. Um fechamento acima da banda superior marca uma tendência de alta; um fechamento abaixo da banda inferior marca uma tendência de baixa. As tendências M15 e H1 devem estar alinhadas para abrir negociações.
- **Fisher Transform** – Uma Fisher Transform personalizada (`Fisher_Yur4ik`) rastreia o impulso no período M15. Cruzar o zero inverte a tendência de alta/baixa, enquanto atravessar ±0,25 produz sinais de saída.
- **ADX filtro** – O Índice Direcional Médio de 15 minutos deve subir e o componente DI dominante deve concordar com a negociação proposta. Quando ADX, +DI e –DI ficam dentro de `Delta` pontos um do outro, a estratégia trata o mercado como plano, redefine os sinalizadores de impulso e liquida as posições abertas.
- **Gerenciamento de posições** – Novas posições fecham qualquer exposição oposta e negociam um volume configurável. As saídas são acionadas por reversões do LabTrend, saídas da Fisher ou uma condição de mercado estável.

## Gestão de risco
- **Stop Loss / Take Profit** – Configurável em pontos do instrumento (MetaTrader “pips”). Eles são avaliados em relação aos máximos/mínimos das velas para imitar as ordens de proteção do EA original.
- **Trailing Stop** – Quando o preço se move a favor da negociação, um trailing stop rastreia o fechamento a uma distância igual a `TrailingStopPoints`. Cruzar o nível final desencadeia uma saída imediata do mercado.
- **Volume** – Todos os pedidos usam o parâmetro fixo `Volume` (lotes).

## Parâmetros
- `Volume` – Tamanho do pedido em lotes. Padrão 1.
- `AdxPeriod` – ADX período de suavização. Padrão 14.
- `FisherLength` – Janela para a transformação de Fisher. Padrão 10.
- `ChannelLength` – Barras utilizadas para o canal LabTrend. Padrão 9.
- `RiskFactor` – Fator de estreitamento do canal LabTrend (intervalo do indicador original 1..10). Padrão 3.
- `Delta` – Diferença máxima entre os valores ADX e DI antes do mercado ser rotulado como plano. Padrão 7.
- `StopLossPoints` – Distância de perda de parada em pontos. Padrão 100.
- `TakeProfitPoints` – Distância de lucro em pontos. Padrão 40.
- `TrailingStopPoints` – Distância da parada final em pontos. Padrão 100.
- `SignalCandleType` – Série de velas para cálculos de sinal (padrão M15).
- `TrendCandleType` – Série de velas para o filtro LabTrend de período de tempo superior (padrão H1).

## Notas
- A implementação original MQL funcionou em cada tick recebido; esta porta processa velas M15 concluídas, o que mantém a lógica determinística e ainda respeita os cálculos do indicador.
- Stop Loss, Take Profit e Trailing Exits são executados com ordens de mercado quando a máxima/mínima da vela ultrapassa os limites configurados. Isso reflete o comportamento de MetaTrader ordens de proteção sem manter ordens de parada/limite explícitas.
- Certifique-se de que a fonte de dados forneça as séries de velas de 15 minutos e de 1 hora definidas nos parâmetros antes de iniciar a estratégia.
