# Estratégia de Pure Price Action Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Pure Price Action** é um port StockSharp do consultor especialista MetaTrader "Pure Price Action" (MQL id 24291).
Combina confirmação de rompimento dos fractais de Bill Williams com um filtro de momentum calculado em um período superior e um filtro de tendência MACD de longo prazo.
O algoritmo tenta capturar operações de continuação de tendência imediatamente após o mercado retestar o nível fractal mais recente.

## Lógica de trading
1. **Velas de sinal** – Decisões de trading são tomadas no período selecionado pelo usuário (15 minutos por padrão).
2. **Confirmação de toque fractal** – Uma operação só é permitida se a vela concluída mais recente fechar dentro de um passo de preço do nível fractal confirmado mais recente (fractal superior para vendidos, fractal inferior para comprados).
3. **Padrão de corpo direcional** – O tamanho absoluto do corpo da vela mais recente deve ser menor que o corpo da vela anterior, enquanto o corpo anterior deve ser maior que a vela anterior a ele. Isso imita o filtro de retração de momentum do EA original.
4. **Médias móviles** – Duas médias móveis lineares ponderadas (LWMA 6 e LWMA 85 por padrão) fornecem a tendência base. Operações compradas exigem que a LWMA rápida esteja acima da lenta; vendidas exigem o oposto.
5. **Filtro de momentum** – Um indicador de momentum de 14 períodos avaliado em um período superior (H1 por padrão) deve desviar do nível de equilíbrio (100) pelo menos pelo limiar configurado durante qualquer uma das três últimas leituras de momentum.
6. **Filtro MACD** – Um MACD(12, 26, 9) calculado em um período superior (mensal por padrão) deve mostrar a linha principal acima da linha de sinal para comprados e abaixo para vendidos.
7. **Dimensionamento de posição** – A estratégia usa a propriedade `Volume` da classe base `Strategy`. Se `Volume` não estiver definido, o padrão é um contrato/lote. O parâmetro `MaxPosition` limita o tamanho absoluto da posição.

## Gerenciamento de posição
- **Proteção inicial** – Distâncias opcionais fixas de stop-loss e take-profit são especificadas em passos de preço e aplicadas simetricamente a ambos os lados.
- **Trailing stop** – Quando habilitado, a estratégia rastreia o preço mais alto/mais baixo atingido após a entrada pela distância configurada.
- **Bloqueio de break-even** – Após o preço percorrer a distância de acionamento, o nível protetor é movido para entrada ± offset para garantir lucros.
- **Saídas manuais** – A lógica avalia níveis de stop-loss, take-profit, trailing e break-even em cada vela concluída e fecha toda a posição quando qualquer condição é atendida.

## Parâmetros
- `CandleType` – Período de sinal principal (padrão: período de 15 minutos).
- `MomentumCandleType` – Período para o indicador de momentum (padrão: período de 1 hora).
- `MacdCandleType` – Período para o filtro MACD (padrão: período de 30 dias, emulando velas mensais).
- `FastPeriod` / `SlowPeriod` – Períodos da LWMA rápida e lenta.
- `MomentumPeriod` – Comprimento do indicador de momentum.
- `MomentumThreshold` – Desvio absoluto mínimo do Momentum em relação a 100.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – Configuração MACD.
- `StopLossPoints`, `TakeProfitPoints` – Distâncias de proteção de risco em passos de preço.
- `TrailingStopPoints` – Distância de trailing em passos de preço.
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – Distâncias de acionamento e lucro garantido do break-even.
- `MaxPosition` – Tamanho máximo absoluto de posição tratado pela estratégia.
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – Controles para os blocos de gerenciamento de risco.

## Notas
- Todos os comentários no código são escritos em inglês, conforme exigido pelas diretrizes do projeto.
- A estratégia depende exclusivamente de velas concluídas; sinais intra-barra não são processados.
- Subscrições multi-período são usadas para emular o comportamento do consultor especialista original (velas de sinal M15, momentum H1, MACD mensal por padrão).
- Nenhum teste automático é fornecido nesta pasta. A suíte de testes do repositório global deve permanecer intocada, conforme solicitado.
